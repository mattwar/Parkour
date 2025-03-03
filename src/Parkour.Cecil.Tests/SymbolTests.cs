using Parkour;
using Parkour.Cecil;
using Parkour.Symbols;
using Mono.Cecil;

namespace Tests
{
    [TestClass]
    public class SymbolTests
    {
        [TestMethod]
        public void TestCommonSymbols()
        {
            TestCommonSymbols(CecilSymbols.CurrentMscorlib);
        }

        private void TestCommonSymbols(SymbolTable symbols)
        {
            Assert.AreEqual("System", symbols.System.FullName);
            Assert.AreEqual("System.Object", symbols.Object.FullName);
            Assert.AreEqual("System.Boolean", symbols.Boolean.FullName);
            Assert.AreEqual("System.Byte", symbols.Byte.FullName);
            Assert.AreEqual("System.Int32", symbols.Int32.FullName);
            Assert.AreEqual("System.Int64", symbols.Int64.FullName);
            Assert.AreEqual("System.Single", symbols.Single.FullName);
            Assert.AreEqual("System.Double", symbols.Double.FullName);
            Assert.AreEqual("System.Decimal", symbols.Decimal.FullName);
            Assert.AreEqual("System.String", symbols.String.FullName);
            Assert.AreEqual("System.Char", symbols.Char.FullName);
        }

        [TestMethod]
        public void TestSymbolsWalk()
        {
            // attempt to iterate though all declared symbols reachable from global namespace
            var rs = CecilSymbols.CurrentMscorlib;
            EnumerateMembers(rs.GlobalNamespace);

            void EnumerateMembers(Symbol symbol)
            {
                symbol.WalkDeclarations(null);
            }
        }

        [TestMethod]
        public void TestFindSymbol()
        {
            var rs = CecilSymbols.CurrentMscorlib;

            //TestFindSymbol(ns, "System.Int32");
            TestFindSymbol(rs, "System.Collections.Generic.List`1");
        }

        private void TestFindSymbol(SymbolTable symbols, string pathName)
        {
            var found = symbols.GetSymbol(pathName);
            Assert.IsNotNull(found);
        }

        [TestMethod]
        public void TestConstruct()
        {
            var symbols = CecilSymbols.CurrentMscorlib;

            var listT = symbols.GetTypeSymbol("System.Collections.Generic.List`1");
            Assert.IsNotNull(listT);

            var listTBT = listT.BaseTypes;

            var listInt32 = symbols.GetConstructed(listT, [symbols.Int32]);
            Assert.IsNotNull(listInt32);

            var listBT = listInt32.BaseTypes;
            var ieT = listBT.FirstOrDefault(t => t.FullName == "System.Collections.Generic.IEnumerable[System.Int32]");
            Assert.IsNotNull(ieT, "could not find interface IEnumerable<T>");
            Assert.IsTrue(ieT.IsConstructed, "base type IEnumerable<T> is not constructed");

            listInt32.WalkDeclarations(null);

            symbols.TryGetTypeReference(listInt32, out var cecilType);
        }


        [TestMethod]
        public void TestNestedReference()
        {
            var testAssembly = AssemblyDefinition.ReadAssembly("Test.Metadata.dll");

            //var testType = testAssembly.MainModule.GetTypes().FirstOrDefault(t => t.Name == "Test");
            //var testMethod = testType!.Methods.FirstOrDefault(m => m.Name == "TestNestedReference");
            //var parameterType = testMethod!.Parameters[0].ParameterType;
            //var declaringType = (GenericInstanceType)parameterType.DeclaringType;

            var symbols = CecilSymbols.GetOrCreate([CecilSymbols.CurrentMscorlibAssembly, testAssembly]);
            var type = symbols.GetTypeSymbol("Test.Metadata.Test");
            var field = type.Members.OfType<FieldSymbol>().FirstOrDefault(f => f.Name == "Field");
            var ftype = field!.Type;

            //var symbols = CecilSymbols.GetOrCreate([CecilSymbols.CurrentMscorlibAssembly, testAssembly]);
        }
    }
}