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
            Assert.AreEqual("System", symbols.System.Name);
            Assert.AreEqual("Object", symbols.Object.Name);
            Assert.AreEqual("Boolean", symbols.Boolean.Name);
            Assert.AreEqual("Byte", symbols.Byte.Name);
            Assert.AreEqual("Int32", symbols.Int32.Name);
            Assert.AreEqual("Int64", symbols.Int64.Name);
            Assert.AreEqual("Single", symbols.Single.Name);
            Assert.AreEqual("Double", symbols.Double.Name);
            Assert.AreEqual("Decimal", symbols.Decimal.Name);
            Assert.AreEqual("String", symbols.String.Name);
            Assert.AreEqual("Char", symbols.Char.Name);
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
            TestFindSymbol(rs.GlobalNamespace, "System.Collections.Generic.List`1");
        }

        private void TestFindSymbol(NamespaceSymbol symbol, string pathName)
        {
            var found = symbol.GetFirstSymbolFromPath(pathName);
            Assert.IsNotNull(found);
        }

        [TestMethod]
        public void TestConstruct()
        {
            var runtimeSymbols = ReflectionSymbols.CurrentMscorlib;

            var listT = (TypeSymbol?)runtimeSymbols.GlobalNamespace.GetFirstSymbolFromPath("System.Collections.Generic.List`1");
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
