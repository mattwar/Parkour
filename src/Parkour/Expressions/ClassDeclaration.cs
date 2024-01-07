namespace Parkour.Expressions;
using Symbols;
using Analysis;

public sealed class ClassDeclaration : Declaration
{
    public ImmutableList<Declaration> Declarations { get; }
    public TypeSymbol Symbol { get; }

    public ClassDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<Declaration>? declarations,
        FunctionSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics = null)
    : base(
          declarations != null ? CombineState(declarations) : ContainsState.None,
          name,
          access,
          modifiers,
          diagnostics)
    {
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
        this.Symbol = symbol ?? SymbolModel.Unknown;
    }
}

