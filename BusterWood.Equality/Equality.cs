using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BusterWood.EqualityGenerator
{
    public static class Equality
    {
        static readonly ConcurrentDictionary<Key, object> _comparers = new ConcurrentDictionary<Key, object>();

        public static IEqualityComparer<T> Comparer<T>(params string[] properties) => (IEqualityComparer<T>)_comparers.GetOrAdd(new Key(typeof(T), properties), CreateInstance);

        private static object CreateInstance(Key key)
        {
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Equality_" + key), AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("mod1");
            var eqType = typeof(IEqualityComparer<>).GetTypeInfo().MakeGenericType(key.Type);
            var typeBuilder = modBuilder.DefineType($"BusterWood.{key.Type}Equality", TypeAttributes.Class, typeof(object), new[] { eqType });

            // define interface implementation
            var iface = typeof(IEqualityComparer<>).GetTypeInfo().MakeGenericType(new[] { key.Type });
            typeBuilder.AddInterfaceImplementation(iface);

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            if (key.Type.GetTypeInfo().IsClass)
            {
                ClassEqualityBuilder.DefineEquals(key, typeBuilder);
                ClassEqualityBuilder.DefineGetHashCode(key, typeBuilder);
            }
            else
            {
                StructEqualityBulider.DefineEquals(key, typeBuilder);
                StructEqualityBulider.DefineGetHashCode(key, typeBuilder);
            }

            var tinfo = typeBuilder.CreateTypeInfo();
            return Activator.CreateInstance(tinfo.AsType());
        }

    }

    static class ClassEqualityBuilder
    {
        public static MethodBuilder DefineGetHashCode(Key key, TypeBuilder typeBuilder)
        {
            var method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.HasThis, typeof(int), new[] { key.Type });
            var il = method.GetILGenerator();
            var getHashCodeOverLoads = typeof(object).GetTypeInfo().GetDeclaredMethods(nameof(object.GetHashCode));
            var getHashCode = getHashCodeOverLoads.First(m => m.GetParameters().Length == 0);

            var hc = il.DeclareLocal<int>();
            il.Load0().Store(hc);  // hc = 0

            //if (obj == null) return 0;
            var @return = il.DefineLabel();
            il.Arg1().Null().IfEqualGoto(@return);

            foreach (var propName in key.Properties)
            {
                var prop = key.Type.GetTypeInfo().GetDeclaredProperty(propName);
                TypeInfo propType = prop.PropertyType.GetTypeInfo();
                var equatable = typeof(IEquatable<>).GetTypeInfo().MakeGenericType(new[] { prop.PropertyType });
                if (propType.IsClass)
                {
                    // if (prop != null) hc += prop.GetHashCode();
                    var next = il.DefineLabel();
                    var temp = il.DeclareLocal(prop.PropertyType);
                    il.Arg1().CallGetProperty(prop).Store(temp);
                    il.Load(temp).Null().IfEqualGoto(next);
                    il.Load(temp).CallVirt(getHashCode).Load(hc).Add().Store(hc);
                    il.MarkLabel(next);
                }
                else // is a struct
                {
                    // var temp = obj.Prop;
                    // hc += temp.GetHashCode();
                    var ghc = propType.GetDeclaredMethods(nameof(object.GetHashCode)).First(m => m.GetParameters().Length == 0); // GetHashCode must be overridden on a struct
                    var temp = il.DeclareLocal(prop.PropertyType);
                    il.Arg1().CallGetProperty(prop).Store(temp);
                    il.LoadAddress(temp).Call(ghc).Load(hc).Add().Store(hc);
                }
            }
            il.MarkLabel(@return);
            il.Load(hc).Return(); // todo: generate hash code
            return method;
        }

        public static MethodBuilder DefineEquals(Key key, TypeBuilder typeBuilder)
        {
            var method = typeBuilder.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.HasThis, typeof(bool), new[] { key.Type, key.Type });
            var il = method.GetILGenerator();
            var returnFalse = il.DefineLabel();
            var returnTrue = il.DefineLabel();

            // reference equality
            il.Arg1().Arg2().Call<object>(nameof(object.ReferenceEquals)).IfTrueGoto(returnTrue);

            // generate null checks
            il.Arg1().Null().Call<object>(nameof(object.ReferenceEquals)).IfTrueGoto(returnFalse);
            il.Arg2().Null().Call<object>(nameof(object.ReferenceEquals)).IfTrueGoto(returnFalse);

            IEnumerable<MethodInfo> equalsOverloads = typeof(object).GetTypeInfo().GetDeclaredMethods(nameof(object.Equals));
            var equals = equalsOverloads.First(m => m.GetParameters().Length == 1);
            var equalsXY = equalsOverloads.First(m => m.GetParameters().Length == 2);
            foreach (var propName in key.Properties)
            {
                var prop = key.Type.GetTypeInfo().GetDeclaredProperty(propName);
                TypeInfo propType = prop.PropertyType.GetTypeInfo();
                var equatable = typeof(IEquatable<>).GetTypeInfo().MakeGenericType(new[] { prop.PropertyType });
                if (propType.IsClass)
                {
                    // reference type call object.Equals(x, y)
                    il.Arg1().CallGetProperty(prop); // cast to object?
                    il.Arg2().CallGetProperty(prop);
                    il.Call(equalsXY).IfFalseGoto(returnFalse);
                }
                else if (propType.IsPrimitive)
                {
                    il.Arg1().CallGetProperty(prop); // cast to object?
                    il.Arg2().CallGetProperty(prop);
                    il.IfNotEqualGoto(returnFalse);
                }
                //else if (propType.ImplementedInterfaces.Contains(equatable))
                //{
                //    IEnumerable<MethodInfo> eqOverloads = propType.GetDeclaredMethods(nameof(object.Equals));
                //    var equitableXY = equalsOverloads.First(m => m.GetParameters().Length == 2 && m.GetParameters().All(p => p.ParameterType == propType));
                //    il.Arg1().CallGetProperty(prop); // cast to object?
                //    il.Arg2().CallGetProperty(prop);
                //    il.CallVirt(equitableXY).IfFalseGoto(returnFalse);
                //}
                else
                {
                    // ValueType type
                    il.Arg1().CallGetProperty(prop);
                    il.Arg2().CallGetProperty(prop);
                    il.CallVirt(equals).IfFalseGoto(returnFalse);
                }
            }

            // drops through to here is all properties are equal
            il.MarkLabel(returnTrue);
            il.Load1().Return();

            // something is not equal
            il.MarkLabel(returnFalse);
            il.Load0().Return();
            return method;
        }

    }

    static class StructEqualityBulider
    {
        public static MethodBuilder DefineEquals(Key key, TypeBuilder typeBuilder)
        {
            var method = typeBuilder.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.HasThis, typeof(bool), new[] { key.Type, key.Type });
            var il = method.GetILGenerator();
            var returnFalse = il.DefineLabel();
            var returnTrue = il.DefineLabel();

            IEnumerable<MethodInfo> equalsOverloads = typeof(object).GetTypeInfo().GetDeclaredMethods(nameof(object.Equals));
            var equals = equalsOverloads.First(m => m.GetParameters().Length == 1);
            var equalsXY = equalsOverloads.First(m => m.GetParameters().Length == 2);
            foreach (var propName in key.Properties)
            {
                var prop = key.Type.GetTypeInfo().GetDeclaredProperty(propName);
                TypeInfo propType = prop.PropertyType.GetTypeInfo();
                var equatable = typeof(IEquatable<>).GetTypeInfo().MakeGenericType(new[] { prop.PropertyType });
                if (propType.IsClass)
                {
                    // reference type call object.Equals(x, y)
                    il.Arg1Address().CallGetProperty(prop); // cast to object?
                    il.Arg2Address().CallGetProperty(prop);
                    il.Call(equalsXY).IfFalseGoto(returnFalse);
                }
                else if (propType.IsPrimitive)
                {
                    il.Arg1Address().CallGetProperty(prop); // cast to object?
                    il.Arg2Address().CallGetProperty(prop);
                    il.IfNotEqualGoto(returnFalse);
                }
                //else if (propType.ImplementedInterfaces.Contains(equatable))
                //{
                //    IEnumerable<MethodInfo> eqOverloads = propType.GetDeclaredMethods(nameof(object.Equals));
                //    var equitableXY = equalsOverloads.First(m => m.GetParameters().Length == 2 && m.GetParameters().All(p => p.ParameterType == propType));
                //    il.Arg1().CallGetProperty(prop); // cast to object?
                //    il.Arg2().CallGetProperty(prop);
                //    il.CallVirt(equitableXY).IfFalseGoto(returnFalse);
                //}
                else
                {
                    // ValueType type
                    il.Arg1Address().CallGetProperty(prop);
                    il.Arg2Address().CallGetProperty(prop);
                    il.Call(equals).IfFalseGoto(returnFalse);
                }
            }

            // drops through to here is all properties are equal
            il.MarkLabel(returnTrue);
            il.Load1().Return();

            // something is not equal
            il.MarkLabel(returnFalse);
            il.Load0().Return();
            return method;
        }

        public static MethodBuilder DefineGetHashCode(Key key, TypeBuilder typeBuilder)
        {
            var method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.HasThis, typeof(int), new[] { key.Type });
            var il = method.GetILGenerator();
            var getHashCodeOverLoads = typeof(object).GetTypeInfo().GetDeclaredMethods(nameof(object.GetHashCode));
            var getHashCode = getHashCodeOverLoads.First(m => m.GetParameters().Length == 0);

            var hc = il.DeclareLocal<int>();
            il.Load0().Store(hc); // hc = 0

            foreach (var propName in key.Properties)
            {
                var prop = key.Type.GetTypeInfo().GetDeclaredProperty(propName);
                TypeInfo propType = prop.PropertyType.GetTypeInfo();
                var equatable = typeof(IEquatable<>).GetTypeInfo().MakeGenericType(new[] { prop.PropertyType });
                if (propType.IsClass)
                {
                    // if (prop != null) hc += prop.GetHashCode();
                    var next = il.DefineLabel();
                    var temp = il.DeclareLocal(prop.PropertyType);
                    il.Arg1Address().CallGetProperty(prop).Store(temp);
                    il.Load(temp).Null().IfEqualGoto(next);
                    il.Load(temp).CallVirt(getHashCode).Load(hc).Add().Store(hc);
                    il.MarkLabel(next);
                }
                else // is a struct
                {
                    // var temp = obj.Prop;
                    // hc += temp.GetHashCode();
                    var ghc = propType.GetDeclaredMethods(nameof(object.GetHashCode)).First(m => m.GetParameters().Length == 0); // GetHashCode must be overridden on a struct
                    var temp = il.DeclareLocal(prop.PropertyType);
                    il.Arg1Address().CallGetProperty(prop).Store(temp);
                    il.LoadAddress(temp).Call(ghc).Load(hc).Add().Store(hc);
                }
            }
            il.Load(hc).Return(); // todo: generate hash code
            return method;
        }
    }

    struct Key
    {
        public Type Type { get; }
        public string[] Properties { get; }

        public Key(Type type, string[] props)
        {
            Type = type;
            Properties = props;
        }

        public override string ToString()
        {
            return Type.Name + "_" + string.Join("_", Properties);
        }
    }
}
