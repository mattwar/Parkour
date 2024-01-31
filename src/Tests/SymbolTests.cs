using Parkour;
using Parkour.Binding;
using Parkour.Symbols;

namespace Tests
{
    [TestClass]
    public class SymbolTests
    {
        [TestMethod]
        public void TestSymbolCache_RuntimeSymbols()
        {
            var runtimeSymbols = RuntimeSymbols.CurrentMscorlib;
            TestSymbolCache(runtimeSymbols.Symbols);
        }

        private void TestSymbolCache(SymbolCache symbols)
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
            var rs = RuntimeSymbols.CurrentMscorlib;
            EnumerateMembers(rs.Namespace);

            void EnumerateMembers(Symbol symbol)
            {
                symbol.WalkDeclarations(null);
            }
        }

        [TestMethod]
        public void TestFindSymbol()
        {
            var rs = RuntimeSymbols.CurrentMscorlib;

            //TestFindSymbol(ns, "System.Int32");
            TestFindSymbol(rs.Namespace, "System.Collections.Generic.List`1");
        }

        private void TestFindSymbol(NamespaceSymbol symbol, string pathName)
        {
            var found = symbol.GetFirstSymbolFromPath(pathName);
            Assert.IsNotNull(found);
        }

        [TestMethod]
        public void TestConstruct()
        {
            var runtimeSymbols = RuntimeSymbols.CurrentMscorlib;
            var listT = (TypeSymbol?)runtimeSymbols.Namespace.GetFirstSymbolFromPath("System.Collections.Generic.List`1");
            Assert.IsNotNull(listT);
            var listInt32 = runtimeSymbols.Symbols.GetConstructed(listT, [runtimeSymbols.Symbols.Int32]);
            Assert.IsNotNull(listInt32);
            listInt32.WalkDeclarations(null);
        }
    }
}
