using Parkour;
using Parkour.Reflection;
using Parkour.Symbols;

namespace Tests
{
    [TestClass]
    public class SymbolTests
    {
        [TestMethod]
        public void TestCommonSymbols_ReflectionSymbols()
        {
            TestCommonSymbols(ReflectionSymbols.CurrentMscorlib);
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
        public void TestRuntimeSymbolsWalk()
        {
            // attempt to iterate though all declared symbols reachable from global namespace
            var rs = ReflectionSymbols.CurrentMscorlib;
            EnumerateMembers(rs.GlobalNamespace);

            void EnumerateMembers(Symbol symbol)
            {
                symbol.WalkDeclarations(null);
            }
        }

        [TestMethod]
        public void TestFindSymbol()
        {
            var rs = ReflectionSymbols.CurrentMscorlib;

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
            var runtimeSymbols = ReflectionSymbols.CurrentMscorlib;

            var listT = runtimeSymbols.GetType("System.Collections.Generic.List`1");
            Assert.IsNotNull(listT);

            var listTBT = listT.BaseTypes;

            var listInt32 = runtimeSymbols.GetConstructed(listT, [runtimeSymbols.Int32]);
            Assert.IsNotNull(listInt32);

            var listBT = listInt32.BaseTypes;
            var ieT = listBT.FirstOrDefault(t => t.FullName == "System.Collections.Generic.IEnumerable[System.Int32]");
            Assert.IsNotNull(ieT, "could not find interface IEnumerable<T>");
            Assert.IsTrue(ieT.IsConstructed, "base type IEnumerable<T> is not constructed");

            listInt32.WalkDeclarations(null);
        }
    }
}
