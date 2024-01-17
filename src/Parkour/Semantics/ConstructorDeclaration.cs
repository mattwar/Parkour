
namespace Parkour.Semantics;

using Symbols;
using Syntax;

public class ConstructorDeclaration : MemberDeclaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public ConstructorSymbol? ConstructorSymbol { get; }

    public ConstructorDeclaration(
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        ConstructorSymbol? constructorSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters) 
            | body.State
            | NotNullState(constructorSymbol),
            (modifiers & SymbolModifier.Static) == 0 ? ".ctor" : ".cctor",
            access,
            modifiers,
            location,
            diagnostics)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.ConstructorSymbol = constructorSymbol;
    }
}
