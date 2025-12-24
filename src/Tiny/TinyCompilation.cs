using System.Collections.Immutable;
using Parkour;
using Parkour.Parsers;
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

    protected override ParsingInfo Parse(ISourceDocument document)
    {
        var lexer = new TinyLexer();
        var tokens = lexer.Parse(document.Text);
        var tp = new TinyParser();
        var result = tp.Parser.Parse(tokens.AsSpan());
        var context = new LexicalParserAnnotations(tp.Parser, tokens);
        var tree = new SyntaxTree(document, result.Output);
        return new ParsingInfo(tree, context);
    }

    protected override BindingInfo Bind()
    {
        if (this.GetSyntaxTree(this.Documents[0]) is SyntaxTree tree)
        {
            var unbound = new TinyTranslator().Translate(tree.Root);
            var binding = new StandardBinder().Bind([unbound], _imports);
            return new BindingInfo(_imports, binding.BoundElements);
       }

        return new BindingInfo(_imports, ImmutableList<SemanticElement>.Empty);
    }
}