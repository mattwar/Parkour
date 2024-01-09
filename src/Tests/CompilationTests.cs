using Parkour;
using Parkour.Expressions;
using Parkour.Analysis;
using Parkour.Symbols;
using static Parkour.Expressions.ExpressionFactory;

namespace Tests;

[TestClass]
public class CompilationTests
{
    [TestMethod]
    public void TestDeclarationCompilation()
    {
        var compilation = DeclarationCompilation.Create(
            ImmutableList.Create<Declaration>(
                Namespace("Fred", Namespace("Wilma")),
                Namespace("Barny", Namespace("Betty")))
            );

        var members = compilation.GlobalNamespace.Members;
        var fred = compilation.GlobalNamespace.GetFirstMember("Fred");
        var wilma = compilation.GlobalNamespace.GetFirstSymbolFromPath("Fred.Wilma");
    }
}
