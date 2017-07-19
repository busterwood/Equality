using NUnit.Framework;
using System;
using System.Collections.Generic;
using BusterWood.EqualityGenerator;

namespace UnitTests
{
    [TestFixture]
    public class ClassEqualityTests
    {
        [Test]
        public void can_check_equality_with_integer()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 2, Name = "world" };
            Assert.AreEqual(true, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_int_property_value_different()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 99, Name = "world" };
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_left_is_null()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Test1 left = null;
            Test1 right = new Test1 { Id = 2, Name = "world" };
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_right_is_null()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Test1 left = new Test1 { Id = 2, Name = "world" }; ;
            Test1 right = null;
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void can_check_equality_with_reference_equals()
        {
            var eq = Equality.Comparer<TestRefEquals>(nameof(Test1.Id));
            var left = new TestRefEquals();
            Assert.AreEqual(true, eq.Equals(left, left)); // if the property is read then an exception is thrown
        }

        [Test]
        public void can_check_equality_with_string()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Name));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 99, Name = "hello" };
            Assert.AreEqual(true, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_string_property_value_different()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Name));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 2, Name = "world" };
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void can_check_equality_with_multiple_properties()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id), nameof(Test1.Name));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 2, Name = "hello" };
            Assert.AreEqual(true, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_first_of_multiple_properties_does_not_match()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id), nameof(Test1.Name));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 99, Name = "hello" };
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void not_equal_if_last_of_multiple_properties_does_not_match()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id), nameof(Test1.Name));
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            Test1 right = new Test1 { Id = 2, Name = "world" };
            Assert.AreEqual(false, eq.Equals(left, right));
        }

        [Test]
        public void can_get_hashcode_of_int_property()
        {
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Assert.AreNotEqual(0, eq.GetHashCode(left));
        }

        [Test]
        public void can_get_hashcode_of_string_property()
        {
            Test1 left = new Test1 { Id = 2, Name = "hello" };
            var eq = Equality.Comparer<Test1>(nameof(Test1.Name));
            Assert.AreNotEqual(0, eq.GetHashCode(left));
        }

        [Test]
        public void can_get_hashcode_of_null_string_property()
        {
            Test1 left = new Test1 { Id = 2, Name = null };
            var eq = Equality.Comparer<Test1>(nameof(Test1.Name));
            Assert.AreEqual(0, eq.GetHashCode(left));
        }

        [Test]
        public void hashcode_of_null_is_zero()
        {
            var eq = Equality.Comparer<Test1>(nameof(Test1.Id));
            Assert.AreEqual(0, eq.GetHashCode(null));
        }

        public class Test1
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class TestRefEquals
        {
            public int Id
            {
                get { throw new ArgumentException(); }
            }
        }
    }
}
