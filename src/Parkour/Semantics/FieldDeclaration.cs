namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class FieldDeclaration : MemberDeclaration
{
    public Expression FieldType { get; }
    public Expression? Initializer { get; }
    public FieldSymbol? FieldSymbol { get; }

    public FieldDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression fieldType,
        Expression? initializer,
        ISourceLocation? location,
        FieldSymbol? fieldSymbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          OptionalState(initializer)
          | NotNullState(fieldSymbol),
          name,
          access,
          modifiers,
          location,
          diagnostics)
    {
        this.FieldType = fieldType;
        this.Initializer = initializer;
        this.FieldSymbol = fieldSymbol;
    }
}

