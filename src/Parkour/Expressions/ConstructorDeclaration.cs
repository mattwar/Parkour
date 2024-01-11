
namespace Parkour.Expressions;

using Symbols;
using Syntax;

public class ConstructorDeclaration : MemberDeclaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public ConstructorSymbol? Symbol { get; }

    public ConstructorDeclaration(
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        ConstructorSymbol? symbol)
        : base(
            CombineState(parameters) | body.State,
            (modifiers & SymbolModifier.Static) == 0 ? ".ctor" : ".cctor",
            access,
            modifiers,
            diagnostics,
            syntax)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.Symbol = symbol;
    }
}
