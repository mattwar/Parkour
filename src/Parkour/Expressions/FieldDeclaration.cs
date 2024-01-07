namespace Parkour.Expressions;
using Symbols;

public sealed class FieldDeclaration : Declaration
{
    public Expression? Initializer { get; }
    public TypeSymbol? FieldType { get; }

    public FieldDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression? initializer,
        TypeSymbol? fieldType,
        ImmutableList<Diagnostic>? diagnostics = null)
    : base(
          initializer != null ? initializer.State : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics)
    {
        this.Initializer = initializer;
        this.FieldType = fieldType;
    }
}

