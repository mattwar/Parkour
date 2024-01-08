namespace Parkour.Expressions;
using Symbols;
using Analysis;

public sealed class ClassDeclaration : Declaration
{
    public ImmutableList<Expression> BaseTypes { get; }
    public ImmutableList<Declaration> Declarations { get; }

    public ClassDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ImmutableList<Diagnostic>? diagnostics = null)
    : base(
          declarations != null ? CombineState(declarations) : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics)
    {
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
    }
}

