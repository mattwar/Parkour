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
            var symbols = RuntimeSymbols.GetOrCreateCache();
            TestSymbolCache(symbols);
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
            var ns = RuntimeSymbols.GetOrCreateGlobalNamespace();
            EnumerateMembers(ns);

            void EnumerateMembers(Symbol symbol)
            {
                symbol.WalkDeclarations(null);
            }
        }

        [TestMethod]
        public void TestFindSymbol()
        {
            var ns = RuntimeSymbols.GetOrCreateGlobalNamespace();

            //TestFindSymbol(ns, "System.Int32");
            TestFindSymbol(ns, "System.Collections.Generic.List`1");
        }

        private void TestFindSymbol(NamespaceSymbol symbol, string pathName)
        {
            var found = symbol.GetFirstSymbolFromPath(pathName);
            Assert.IsNotNull(found);
        }

        [TestMethod]
        public void TestConstruct()
        {
            var symbols = RuntimeSymbols.GetOrCreateCache();
            var listT = (TypeSymbol?)symbols.GlobalNamespace.GetFirstSymbolFromPath("System.Collections.Generic.List`1");
            Assert.IsNotNull(listT);
            var listInt32 = symbols.GetOrConstruct(listT, [symbols.Int32]);
            Assert.IsNotNull(listInt32);
            listInt32.WalkDeclarations(null);
        }

        [TestMethod]
        public void TestSubstitute()
        {
            var symbols = RuntimeSymbols.GetOrCreateCache();
            var listT = (TypeSymbol?)symbols.GlobalNamespace.GetFirstSymbolFromPath("System.Collections.Generic.List`1");
            Assert.IsNotNull(listT);
            var listInt32 = symbols.Substitute(listT, listT.TypeParameters, [symbols.Int32]);
            Assert.IsNotNull(listInt32);
            var members = listInt32.Members;
        }
    }
}
