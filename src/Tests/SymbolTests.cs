using Parkour;
using Parkour.Analysis;

namespace Tests
{
    [TestClass]
    public class SymbolTests
    {
        private SymbolModel _symbols;

        public SymbolTests()
        {
            _symbols = new RuntimeSymbolModel();
        }

        [TestMethod]
        public void TestInt32()
        {
            var type = _symbols.Int32;
            var members = type.Members;
        }
    }
}
