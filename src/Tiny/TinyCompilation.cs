using Parkour;
using Parkour.Compilations;
using Parkour.Binding;
using Parkour.Syntax;
using Parkour.Symbols;

namespace Tiny;

public class TinyCompilation : ExpressionCompilation
{
    public TinyCompilation(SyntaxTree tinyTree, SymbolTable externalSymbols)
        : base(tinyTree, externalSymbols, Bind)
    {
    }

    public TinyCompilation(string tinyText, SymbolTable externalSymbols)
        : base(Parse(tinyText), externalSymbols, Bind)
    {
    }

    private static SyntaxTree Parse(string tinyText)
    {
        return new TinyParser().Parse("", tinyText);
    }

    private static ExpressionBinding Bind(ISyntaxTree tree, SymbolTable externalSymbols)
    {
        var tinyTree = (SyntaxTree)tree;
        var unbound = new TinyTranslator().Translate(tinyTree.Root);
        return new StandardBinder().BindExpression(unbound, externalSymbols);
    }
}