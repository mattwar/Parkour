using Parkour;
using Parkour.Binding;
using Parkour.Symbols;

namespace Tests
{
    [TestClass]
    public class SymbolTests
    {
        [TestMethod]
        public void TestRuntimeCommonSymbols()
        {
            var symbols = RuntimeSymbols.GetOrCreateCommonSymbols();
            TestCommonSymbols(symbols);
        }

        private void TestCommonSymbols(SymbolCache symbols)
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
                symbol.Walk(null);
            }
        }
    }
}
