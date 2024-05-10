using System.Collections.Immutable;
using Parkour;
using Parkour.Compilations;
using Parkour.Binding;
using Parkour.Semantics;
using Parkour.Syntax;
using Parkour.Symbols;

namespace Tiny;

public class TinyCompilation : SemanticCompilation
{
    private readonly SymbolTable _imports;

    public TinyCompilation(ISourceDocument document, SymbolTable imports)
        : base([document])
    {
        _imports = imports;
    }

    protected override ParseInfo Parse()
    {
        var doc = this.Documents[0];
        var tree = new TinyParser().Parse(doc);
        return new ParseInfo([tree]);
    }

    protected override BindingInfo Bind()
    {
        if (this.GetSyntaxTree(this.Documents[0]) is SyntaxTree tree)
        {
            var unbound = new TinyTranslator().Translate(tree.Root);
            var binding = new StandardDeclarationBinder().BindExpression(unbound, _imports);
            return new BindingInfo(_imports, [binding.Expression]);
       }

        return new BindingInfo(_imports, ImmutableList<SemanticElement>.Empty);
    }
}