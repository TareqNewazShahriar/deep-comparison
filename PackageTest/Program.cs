using DeepComparison;
using System;
using System.Globalization;

namespace PackageTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var a = new Student { MyProperty = new[] { 3 } };
            var b = new Student { MyProperty = new[] { 3 } };

            bool diff = DeepComparison<Student>.Compare(a, b, depth: 1);
            // new CultureInfo("en-US"), new CultureInfo("en-NZ")
            Console.WriteLine(diff);
        }
    }

    class Student
    {
        public int[] MyProperty { get; set; }
    }

}
