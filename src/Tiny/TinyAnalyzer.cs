using Parkour;
using Parkour.Analysis;
using Parkour.Diagnostics;
using Parkour.Symbols;
using Parkour.Syntax;

namespace Tiny
{
    public class TinyAnalyzer
    {
        private readonly CommonSymbols _symbols;
        private readonly Dictionary<string, Symbol> _globals;
        private readonly Dictionary<SyntaxElement, SemanticInfo> _symbolMap;

        private TinyAnalyzer(Dictionary<string, Symbol> globals, CommonSymbols model)
        {
            _globals = globals;
            _symbols = model;
            _symbolMap = new Dictionary<SyntaxElement, SemanticInfo>();
        }

        public static SyntaxTreeAnalysis Analyze(SyntaxTree syntax, Dictionary<string, Symbol> globals, CommonSymbols symbols)
        {
            var analyzer = new TinyAnalyzer(globals, symbols);
            return analyzer.Analyze(syntax);
        }

        private TinyTreeAnalysis Analyze(SyntaxTree tree)
        {
            SyntaxElement.WalkElements(tree.Root,
                fnAfter: element =>
                {
                    if (element is SyntaxToken token)
                    {
                        switch (token.Kind)
                        {
                            case TokenKinds.NumberToken:
                                _symbolMap.Add(token, new SemanticInfo(_symbols.Double));
                                break;

                            case TokenKinds.StringToken:
                                _symbolMap.Add(token, new SemanticInfo(_symbols.String));
                                break;

                            case TokenKinds.IdentifierToken:
                                if (_globals.TryGetValue(token.Text, out var symbol))
                                {
                                    _symbolMap.Add(token, new SemanticInfo(symbol as TypeSymbol, symbol));
                                }
                                else
                                {
                                    _symbolMap.Add(token, new SemanticInfo(new Diagnostic($"The name '{token.Text}' does not refer to any known symbol.")));
                                }
                                break;
                        }
                    }
                    else if (element is SyntaxList node)
                    {
                        switch (node.Kind)
                        {
                            case NodeKinds.Add:
                            case NodeKinds.Subtract:
                            case NodeKinds.Divide:
                            case NodeKinds.Multiply:
                            case NodeKinds.Equal:
                                _symbolMap.Add(node, GetOperatorInfo(node));
                                break;
                        }
                    }
                });

            return new TinyTreeAnalysis(tree, _symbolMap);
        }

        private TypeSymbol? GetResultType(SyntaxElement element)
        {
            if (_symbolMap.TryGetValue(element, out var info))
            {
                return info.ResultType;
            }

            return null;
        }

        private SemanticInfo GetOperatorInfo(SyntaxList node)
        {
            if (node.GetChild(0) is SyntaxElement left
                && node.GetChild(1) is SyntaxToken op
                && node.GetChild(2) is SyntaxElement right)
            {
                var leftType = GetResultType(left);
                var rightType = GetResultType(right);

                if (node.Kind == NodeKinds.Add)
                {
                    if (leftType == _symbols.Double && rightType == _symbols.Double)
                    {
                        return new SemanticInfo(_symbols.Double);
                    }
                    else if (leftType == _symbols.String && rightType == _symbols.String
                        || leftType == _symbols.String && rightType == _symbols.String
                        || leftType == _symbols.String && rightType == _symbols.String)
                    {
                        return new SemanticInfo(_symbols.String);
                    }
                }
                else
                {
                    if (leftType == _symbols.Double && rightType == _symbols.Double)
                    {
                        return new SemanticInfo(_symbols.Double);
                    }
                }

                if (leftType != CommonSymbols.Unknown && rightType != CommonSymbols.Unknown)
                {
                    return new SemanticInfo(CommonSymbols.Unknown, diagnostics: new[] { GetOperatorNotDefinedForTypes(op.Text, leftType, rightType) });
                }
            }

            return new SemanticInfo(CommonSymbols.Unknown);
        }

        private static Diagnostic GetOperatorNotDefinedForTypes(string opName, params Symbol?[] types)
        {
            var typeList = string.Join(", ", types.Select(t => t?.Name ?? "Unknown"));
            return new Diagnostic($"The operator '{opName}' is not defined for types: {typeList}");
        }

        private class TinyTreeAnalysis : SyntaxTreeAnalysis
        {
            private readonly Dictionary<SyntaxElement, SemanticInfo> _elementSemantics;

            internal TinyTreeAnalysis(
                SyntaxTree tree,
                IEnumerable<KeyValuePair<SyntaxElement, SemanticInfo>> elementSemantics)
                : base(tree)
            {
                _elementSemantics = elementSemantics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            /// <summary>
            /// Attempts to get/return any semantic information associated with the syntax element.
            /// </summary>
            public SemanticInfo? GetSemanticInfo(SyntaxElement element)
            {
                _elementSemantics.TryGetValue(element, out var semanticInfo);
                return semanticInfo;
            }

            public override Symbol? GetReferencedSymbol(SyntaxElement element) =>
                GetSemanticInfo(element)?.ReferencedSymbol;

            public override void GetReferencedSymbols(SyntaxElement element, List<Symbol> symbols)
            {
                if (GetReferencedSymbol(element) is Symbol symbol)
                {
                    symbols.Add(symbol);
                }
            }

            public override TypeSymbol? GetResultType(SyntaxElement element) =>
                GetSemanticInfo(element)?.ResultType;


            public override void GetDiagnostics(SyntaxElement syntax, List<Diagnostic> diagnostics)
            {
                if (syntax.Diagnostic != null)
                    diagnostics.Add(syntax.Diagnostic.WithLocation(syntax));

                if (GetSemanticInfo(syntax) is SemanticInfo info)
                {
                    diagnostics.AddRange(info.Diagnostics);
                }
            }
        }

        private class SemanticInfo
        {
            public Symbol? ReferencedSymbol { get; }
            public TypeSymbol? ResultType { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }

            internal SemanticInfo(
                TypeSymbol? resultType = null,
                Symbol? referencedSymbol = null,
                IReadOnlyList<Diagnostic>? diagnostics = null)
            {
                ReferencedSymbol = referencedSymbol;
                ReferencedSymbol = resultType;
                Diagnostics = diagnostics ?? Array.Empty<Diagnostic>();
            }

            public SemanticInfo(Diagnostic diagnostic)
                : this(null, null, new[] { diagnostic })
            {
            }
        }
    }
}
