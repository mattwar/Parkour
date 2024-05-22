namespace Parkour.Semantics;

using Symbols;
using static Semantics.SemanticFactory;

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
                            .WithAccess(SymbolAccess.Public)
                            .WithModifiers(SymbolModifier.Static)
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