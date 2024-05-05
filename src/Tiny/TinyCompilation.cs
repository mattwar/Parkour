using Parkour;
using Parkour.Compilations;
using Parkour.Binding;
using Parkour.Syntax;
using Parkour.Symbols;

namespace Tiny;

public class TinyCompilation : ExpressionCompilation
{
    public TinyCompilation(SyntaxTree tinyTree, SymbolTable externalSymbols)
        : base(tinyTree, _tree => Bind(_tree, externalSymbols))
    {
    }

    public TinyCompilation(string tinyText, SymbolTable externalSymbols)
        : base(Parse(tinyText), _tree => Bind(_tree, externalSymbols))
    {
    }

    private static SyntaxTree Parse(string tinyText)
    {
        return new TinyParser().Parse("", tinyText);
    }

    private static BindingInfo Bind(ISyntaxTree tree, SymbolTable externalSymbols)
    {
        var tinyTree = (SyntaxTree)tree;
        var unbound = new TinyTranslator().Translate(tinyTree.Root);
        var binding = new StandardDeclarationBinder().BindExpression(unbound, externalSymbols);
        return new BindingInfo(binding.Expression);
    }
}