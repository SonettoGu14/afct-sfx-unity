using System;

// Minimal NUnit-compatible shim so the EditMode tests can run in a plain dotnet
// console app without downloading any packages. Only the surface the tests use.
namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestFixtureAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetUpAttribute : Attribute
    {
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        {
            AreEqual(expected, actual, null);
        }

        public static void AreEqual(object expected, object actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new AssertionException(
                    Describe(message) + "expected <" + Render(expected) + "> but was <" + Render(actual) + ">");
            }
        }

        public static void AreEqual(long expected, long actual)
        {
            AreEqual(expected, actual, null);
        }

        public static void AreEqual(long expected, long actual, string message)
        {
            if (expected != actual)
            {
                throw new AssertionException(
                    Describe(message) + "expected <" + expected + "> but was <" + actual + ">");
            }
        }

        public static void AreEqual(double expected, double actual, double delta)
        {
            AreEqual(expected, actual, delta, null);
        }

        public static void AreEqual(double expected, double actual, double delta, string message)
        {
            if (double.IsNaN(expected) || double.IsNaN(actual))
            {
                if (!(double.IsNaN(expected) && double.IsNaN(actual)))
                {
                    throw new AssertionException(
                        Describe(message) + "expected <" + expected + "> but was <" + actual + ">");
                }
            }
            else if (Math.Abs(expected - actual) > delta)
            {
                throw new AssertionException(
                    Describe(message) + "expected <" + expected.ToString("R") + "> but was <"
                    + actual.ToString("R") + "> (tolerance " + delta.ToString("R") + ", off by "
                    + Math.Abs(expected - actual).ToString("R") + ")");
            }
        }

        public static void IsTrue(bool condition)
        {
            IsTrue(condition, null);
        }

        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new AssertionException(Describe(message) + "expected true but was false");
            }
        }

        public static void Fail(string message)
        {
            throw new AssertionException(message);
        }

        private static string Describe(string message)
        {
            return string.IsNullOrEmpty(message) ? string.Empty : message + ": ";
        }

        private static string Render(object value)
        {
            if (value == null)
            {
                return "null";
            }

            return value is double number ? number.ToString("R") : value.ToString();
        }
    }
}
