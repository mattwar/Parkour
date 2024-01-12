namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ClassDeclaration : MemberDeclaration
{
    public ImmutableList<Expression> BaseTypes { get; }
    public ImmutableList<Declaration> Declarations { get; }
    public TypeSymbol? Symbol { get; }

    public ClassDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        TypeSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          declarations != null ? CombineState(declarations) : ContainsState.None,
          name,
          access,
          modifiers,
          location,
          diagnostics)
    {
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
        this.Symbol = symbol;
    }
}

