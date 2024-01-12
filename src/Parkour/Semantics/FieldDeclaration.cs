namespace Parkour.Semantics;
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
        ISourceLocation? location,
        FieldSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          initializer != null ? initializer.State : ContainsState.None,
          name,
          access,
          modifiers,
          location,
          diagnostics)
    {
        this.FieldType = fieldType;
        this.Initializer = initializer;
        this.Symbol = symbol;
    }
}

