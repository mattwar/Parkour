
namespace Parkour.Semantics;

using Symbols;
using Syntax;

public class ConstructorDeclaration : MemberDeclaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public ConstructorSymbol? ConstructorSymbol { get; }
    public LabelSymbol? ReturnLabel { get; }

    public ConstructorDeclaration(
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        ConstructorSymbol? constructorSymbol,
        LabelSymbol? returnLabel,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters) 
            | State(body)
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
        this.ReturnLabel = returnLabel;
    }

    public override Symbol? DeclaredSymbol => this.ConstructorSymbol;

    public override int ChildCount =>
        this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;
}
