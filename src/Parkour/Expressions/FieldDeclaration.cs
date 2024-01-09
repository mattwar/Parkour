namespace Parkour.Expressions;
using Symbols;
using Syntax;

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
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
    : base(
          initializer != null ? initializer.State : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics,
          syntax)
    {
        this.FieldType = fieldType;
        this.Initializer = initializer;
    }
}

