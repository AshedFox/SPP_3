using System;

namespace AssemblyBrowserLib.Tests
{
    public class TestClass1
    {
        protected internal int ReturnInt(bool param1, byte param2, char param3) => int.MaxValue;

        private T ReturnAny<T>() where T: new() => new();
    }

    public static class TestClass2
    {
        public static void DoNothing(this string str)
        {
            Console.WriteLine("Nothing");
        }

        public static void DoNothing(this string str, bool isNothing)
        {
            Console.WriteLine("Nothing");
        }

        public static string DoReturn(this string str) => str;
    }

    internal class TestClass3<T> where T : class, new()
    {
        public T Prop1 { get; set; }
        public int Prop2 { get; private set; }
        private float Prop3 { get; }

        protected T ReturnT() => new T();
    }

    public interface ITestInterface
    {
        public void HelloWorld();
    }

    public struct TestStruct: ITestInterface
    {
        public void HelloWorld() { }
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}