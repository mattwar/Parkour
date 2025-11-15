namespace Parkour.Semantics;

using Parkour;
using Symbols;
using static Semantics.SemanticFactory;

/// <summary>
/// Converts all top-level expressions into Program.Main()
/// </summary>
public class TopLevelExpressionLowerer : PartialLowerer
{
    public static readonly PartialLowerer Instance =
        new TopLevelExpressionLowerer();

    public override ImmutableList<SemanticElement> Lower(
        ImmutableList<SemanticElement> elements, 
        SymbolTable symbols)
    {
        if (elements.Any(e => e is Expression))
        {
            var exprs = elements.OfType<Expression>().ToImmutableList();
            var decls = elements.OfType<Declaration>().ToImmutableList();

            // put all expressions into a static Program.Main method
            var program =
                Class("Program", 
                    [
                        Method(
                            "Main", 
                            [Parameter("args", Symbol("System.String").Array())],
                            Symbol("System.Object"),
                            Block(exprs),
                            exprs[0].Location)
                            .WithAccess(Access.Public)
                            .WithModifiers(Modifier.Static)
                    ],
                    exprs[0].Location);

            return [..decls, program];
        }
        else
        {
            return elements;
        }
    }
}