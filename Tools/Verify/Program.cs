using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace AFCT.SfxLab.Verify
{
    /// <summary>
    /// Reflects over every [Test] method in this assembly, runs it, prints pass/fail
    /// and exits non-zero when anything fails.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            Assembly assembly = typeof(Program).Assembly;
            Type[] types = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            int passed = 0;
            var failures = new List<string>();
            var tests = new List<(Type Type, MethodInfo Method)>();

            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttribute<TestAttribute>() != null)
                    {
                        tests.Add((type, method));
                    }
                }
            }

            if (tests.Count == 0)
            {
                Console.Error.WriteLine("No [Test] methods found.");
                return 2;
            }

            foreach ((Type type, MethodInfo method) in tests)
            {
                string name = type.Name + "." + method.Name;
                try
                {
                    object instance = Activator.CreateInstance(type);
                    InvokeSetUps(type, instance);
                    method.Invoke(instance, null);
                    passed += 1;
                    Console.WriteLine("PASS  " + name);
                }
                catch (TargetInvocationException exception)
                {
                    Exception inner = exception.InnerException ?? exception;
                    failures.Add(name + ": " + inner.GetType().Name + ": " + inner.Message);
                    Console.WriteLine("FAIL  " + name);
                    Console.WriteLine("      " + inner.GetType().Name + ": " + inner.Message);
                }
                catch (Exception exception)
                {
                    failures.Add(name + ": " + exception.GetType().Name + ": " + exception.Message);
                    Console.WriteLine("FAIL  " + name);
                    Console.WriteLine("      " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tests run: " + (passed + failures.Count) + ", passed: " + passed + ", failed: " + failures.Count);

            if (failures.Count == 0)
            {
                Console.WriteLine("RESULT: PASS");
                return 0;
            }

            Console.WriteLine("RESULT: FAIL");
            foreach (string failure in failures)
            {
                Console.WriteLine("  - " + failure);
            }

            return 1;
        }

        private static void InvokeSetUps(Type type, object instance)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<SetUpAttribute>() != null)
                {
                    method.Invoke(instance, null);
                }
            }
        }
    }
}
