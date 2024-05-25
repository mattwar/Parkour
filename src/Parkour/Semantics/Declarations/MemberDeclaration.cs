namespace Parkour.Semantics;
using Symbols;

public abstract class MemberDeclaration : Declaration
{
    public SymbolAccess Access { get; }
    public BitSet<SymbolModifier> Modifiers { get; }
    public ImmutableList<AttributeExpression> Attributes { get; }

    private protected MemberDeclaration(
        ContainsState state,
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            state
            | CombineState(attributes),
            name,
            location,
            diagnostics)
    {
        this.Access = access;
        this.Modifiers = modifiers;
        this.Attributes = attributes;
    }

    public abstract MemberDeclaration WithAccess(SymbolAccess access);
    public abstract MemberDeclaration WithModifiers(BitSet<SymbolModifier> modifiers);
    public abstract MemberDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes);

    public override int ChildCount => 
        this.Attributes.Count;

    public override SemanticElement? GetChild(int index) =>
        index >= 0 && index < this.Attributes.Count 
            ? this.Attributes[index] 
            : null;
}
