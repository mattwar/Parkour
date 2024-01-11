namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class FieldDeclaration : MemberDeclaration
{
    public Expression FieldType { get; }
    public Expression? Initializer { get; }
    public FieldSymbol? Symbol { get; }

    public FieldDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression fieldType,
        Expression? initializer,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        FieldSymbol? symbol)
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
        this.Symbol = symbol;
    }
}

