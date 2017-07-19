using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BusterWood.EqualityGenerator
{
    public struct Vt1
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public void Fred()
        {
            IEqualityComparer<Vt1> eq = Equality.Create<Vt1>(nameof(Id), nameof(Name));
        }

        public bool Equals(Vt1 left, Vt1 right)
        {
            return left.Id != right.Id;
        }

        public int GetHashCode(Vt1 left)
        {
            int hc = 0;
            hc += left.Id.GetHashCode();
            return hc;
        }
    }

    public class Class1
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public void Fred()
        {
            IEqualityComparer<Class1> eq = Equality.Create<Class1>(nameof(Id), nameof(Name));
        }

        public bool Equals(Class1 left, Class1 right)
        {
            return left.Id != right.Id;
        }

        public int GetHashCode(Class1 left)
        {
            int hc = 0;
            hc += left.Id.GetHashCode();
            return hc;
        }
    }
}
