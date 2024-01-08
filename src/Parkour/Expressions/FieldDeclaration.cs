namespace Parkour.Expressions;
using Symbols;

public sealed class FieldDeclaration : Declaration
{
    public Expression FieldType { get; }
    public Expression? Initializer { get; }

    public FieldDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression fieldType,
        Expression? initializer,
        ImmutableList<Diagnostic>? diagnostics = null)
    : base(
          initializer != null ? initializer.State : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics)
    {
        this.FieldType = fieldType;
        this.Initializer = initializer;
    }
}

