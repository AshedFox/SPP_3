using System.Collections.Generic;
using Xunit;

namespace AssemblyBrowserLib.Tests
{
    public class AssemblyParserTests
    {
        public static IEnumerable<object[]> TestLibsPaths
        {
            get
            {
                yield return new object[] { "./Moq.dll" };
                yield return new object[] { "./Newtonsoft.Json.dll" };
                yield return new object[] { "./AutoMapper.dll" };
                yield return new object[] { "./Castle.Core.dll" };
                yield return new object[] { "./AssemblyBrowserLib.Tests.dll" };
            }
        }
        
        public static IEnumerable<object[]> TestMembers
        {
            get
            {
                yield return new object[]
                {
                    "namespace AssemblyBrowserLib.Tests", "public class TestClass1", new List<string>
                    {
                        "protected internal Int32 ReturnInt(Boolean param1, Byte param2, Char param3)",
                        "private T ReturnAny<T>()"
                    }
                };
                yield return new object[]
                {
                    "namespace System", "String", new List<string>
                    {
                        "public static Void DoNothing(this String str)",
                        "public static Void DoNothing(this String str, Boolean isNothing)",
                        "public static String DoReturn(this String str)"
                    }
                };
                yield return new object[]
                {
                    "namespace AssemblyBrowserLib.Tests", "internal class TestClass3", new List<string>
                    {
                        "public T Prop1 { public get; public set; }",
                        "public Int32 Prop2 { public get; private set; }",
                        "private Single Prop3 { private get; }",
                        "protected T ReturnT()"
                    }
                };
                yield return new object[]
                {
                    "namespace AssemblyBrowserLib.Tests", "public interface ITestInterface", new List<string>
                    {
                        "public abstract Void HelloWorld()"
                    }
                };
                yield return new object[]
                {
                    "namespace AssemblyBrowserLib.Tests", "public struct TestStruct", new List<string>
                    {
                        "public virtual Void HelloWorld()"
                    }
                };
                yield return new object[]
                {
                    "namespace AssemblyBrowserLib.Tests", "public enum TestEnum", new List<string>
                    {
                        "Value1",
                        "Value2",
                        "Value3"
                    }
                };
            }
        }

        public static readonly string MainTestLibPath = "./AssemblyBrowserLib.Tests.dll";
        
        [Theory]
        [MemberData(nameof(TestLibsPaths))]
        public void Parse_ParsingAssembly_AssemblyParsedWithoutExceptions(string path)
        {
            var assemblyParser = new AssemblyParser();
            assemblyParser.Parse(path);
            Assert.NotNull(assemblyParser);
        }

        [Fact]
        public void Parse_ParsingAssembly_GotCorrectNamespaces()
        {
            List<string> namespacesTitles = new()
            {
                "namespace AssemblyBrowserLib.Tests",
                "namespace System",
            };
            
            var assemblyParser = new AssemblyParser();
            var result = assemblyParser.Parse(MainTestLibPath);
            Assert.True(namespacesTitles.TrueForAll(s => result.ChildNodes.Exists(node => node.Title.Contains(s))));
        }
        
        [Fact]
        public void Parse_ParsingAssembly_GotCorrectTypes()
        {
            List<string> typesTitles = new()
            {
                "public class AssemblyParserTests",
                "public class TestClass1",
                "public static class TestClass2",
                "public interface ITestInterface",
                "public struct TestStruct",
                "public enum TestEnum"
            };

            var assemblyParser = new AssemblyParser();
            var result = assemblyParser.Parse(MainTestLibPath);
            
            var namespaceNode = result.ChildNodes.Find(node => node.Title == "namespace AssemblyBrowserLib.Tests");

            Assert.NotNull(namespaceNode);
            Assert.True(typesTitles.TrueForAll(s => namespaceNode.ChildNodes.Exists(node => node.Title.Contains(s))));
        }
        
        [Theory]
        [MemberData(nameof(TestMembers))]
        public void Parse_ParsingAssembly_GotCorrectMembers(string namespaceTitle, string typeTitle, List<string> membersTitles)
        {
            var assemblyParser = new AssemblyParser();
            var result = assemblyParser.Parse(MainTestLibPath);
            
            var typeNode = result.ChildNodes.Find(namespaceNode => namespaceNode.Title.Equals(namespaceTitle))
                ?.ChildNodes.Find(typeNode => typeNode.Title.Contains(typeTitle));
            
            Assert.NotNull(typeNode);

            foreach (var memberTitle in membersTitles)
            {
                Assert.Contains(typeNode.ChildNodes,
                    memberNode => memberNode != null && memberNode.Title.Contains(memberTitle));
            }
        }
    }
}