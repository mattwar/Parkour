using Parkour;
using Parkour.Compilations;
using Parkour.Binding;
using Parkour.Syntax;
using Parkour.Symbols;

namespace Tiny;

public class TinyCompilation : ExpressionCompilation
{
    public TinyCompilation(SyntaxTree tinyTree, NamespaceSymbol externalSymbols)
        : base(tinyTree, externalSymbols, Bind)
    {
    }

    public TinyCompilation(string tinyText, NamespaceSymbol externalSymbols)
        : base(Parse(tinyText), externalSymbols, Bind)
    {
    }

    private static SyntaxTree Parse(string tinyText)
    {
        return new TinyParser().Parse("", tinyText);
    }

    private static ExpressionBinding Bind(ISyntaxTree tree, NamespaceSymbol globalNs)
    {
        var tinyTree = (SyntaxTree)tree;
        var unbound = new TinyTranslator().Translate(tinyTree.Root);
        var bound = new ExpressionBinder(globalNs).Bind(unbound);
        return new ExpressionBinding(unbound, bound);
    }
}