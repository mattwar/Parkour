namespace Parkour.Expressions;
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
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        TypeSymbol? symbol)
    : base(
          declarations != null ? CombineState(declarations) : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics,
          syntax)
    {
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
        this.Symbol = symbol;
    }
}

