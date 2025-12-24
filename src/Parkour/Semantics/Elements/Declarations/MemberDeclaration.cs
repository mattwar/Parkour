namespace Parkour.Semantics;

using Parkour;
using Symbols;

public abstract class MemberDeclaration : Declaration
{
    public Access Access { get; }
    public BitSet<Modifier> Modifiers { get; }
    public ImmutableList<AttributeExpression> Attributes { get; }

    private protected MemberDeclaration(
        ContainsState state,
        string name,
        Access access,
        BitSet<Modifier> modifiers,
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

    public abstract MemberDeclaration WithAccess(Access access);
    public abstract MemberDeclaration WithModifiers(BitSet<Modifier> modifiers);
    public abstract MemberDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes);

    public override int ChildCount => 
        this.Attributes.Count;

    public override SemanticElement? GetChild(int index) =>
        index >= 0 && index < this.Attributes.Count 
            ? this.Attributes[index] 
            : null;
}
