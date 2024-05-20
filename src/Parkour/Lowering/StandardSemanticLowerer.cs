using Parkour.Binding;
using Parkour.Symbols;
using Parkour.Semantics;

namespace Parkour.Lowering;

public class StandardSemanticLowerer : SemanticLowerer
{
    protected SemanticBinder Binder { get; }

    public StandardSemanticLowerer(
        SemanticBinder? binder = null)
    {
        this.Binder = binder ?? new StandardSemanticBinder();
    }

    public override DeclarationLowering LowerDeclarations(
        ImmutableList<Declaration> declarations,
        SymbolTable externalSymbols)
    {
        var context = new LoweringContext(externalSymbols);
        var lowered = this.Lower(context, ImmutableStack<Declaration>.Empty, declarations);
        var additional = context.GetAdditionalDeclarations();
        if (lowered != declarations || additional.Count > 0)
        {
            var newdecls = lowered.AddRange(additional);
            var bound = this.Binder.BindDeclarations(newdecls, externalSymbols);
            return new DeclLowering(
                bound.Declarations, 
                () => bound.Diagnostics
                );
        }
        else
        {
            return new DeclLowering(
                declarations, 
                () => ImmutableList<Diagnostic>.Empty
                );
        }
    }

    public override ExpressionLowering LowerExpression(
        Expression expression,
        SymbolTable externalSymbols)
    {
        var context = new LoweringContext(externalSymbols);
        var stack = new Stack<Declaration>();
        var lowered = this.Lower(context, ImmutableStack<Declaration>.Empty, expression);

        if (lowered != expression)
        {
            var list = new List<Diagnostic>();
            expression.GetContainedDiagnostics(list);
            return new ExpressionLowering(
                expression,
                list.ToImmutableList()
                );
        }
        else
        {
            return new ExpressionLowering(
                expression, 
                ImmutableList<Diagnostic>.Empty
                );
        }
    }

    private class DeclLowering : DeclarationLowering
    {
        public override ImmutableList<Declaration> Declarations { get; }

        private Func<ImmutableList<Diagnostic>> _fnDiagnostics { get; }
        private ImmutableList<Diagnostic>? _diagnostics;

        public override ImmutableList<Diagnostic> Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    var tmp = _fnDiagnostics();
                    Interlocked.CompareExchange(ref _diagnostics, tmp, null);
                }

                return _diagnostics;
            }
        }

        public DeclLowering(
            ImmutableList<Declaration> declarations,
            Func<ImmutableList<Diagnostic>> fnDiagnostics)
        {
            this.Declarations = declarations;
            _fnDiagnostics = fnDiagnostics;
        }
    }

    protected virtual ImmutableList<Declaration> Lower(
        LoweringContext context,
        ImmutableStack<Declaration> ancestors,
        ImmutableList<Declaration> declarations)
    {
        return declarations.Rewrite(d =>
            (Declaration)Lower(context, ancestors, d)
            );
    }

    protected virtual SemanticElement Lower(
        LoweringContext context,
        ImmutableStack<Declaration> ancestors,
        SemanticElement element)
    {
        var lowered = FieldInitializerLowerer.LowerAll(element);
        return lowered;
    }

    protected class LoweringContext
    {
        public SymbolTable Symbols { get; }

        public LoweringContext(
            SymbolTable symbols)
        {
            this.Symbols = symbols;
        }

        private readonly List<Declaration> _additionalDeclarations =
            new List<Declaration>();

        private readonly Dictionary<Declaration, List<Declaration>> _declToAddMembersMap =
            new Dictionary<Declaration, List<Declaration>>();

        /// <summary>
        /// Adds a new declaration
        /// </summary>
        public void AddDeclaration(Declaration declaration)
        {
            _additionalDeclarations.Add(declaration);
        }

        public ImmutableList<Declaration> GetAdditionalDeclarations() =>
            _additionalDeclarations.ToImmutableList();

        /// <summary>
        /// Adds a new member declaration to the parent declaration.
        /// </summary>
        public void AddMembers(Declaration declaration, IEnumerable<Declaration> members)
        {
            if (!_declToAddMembersMap.TryGetValue(declaration, out var additionalMembers))
            {
                additionalMembers = new List<Declaration>();
                _declToAddMembersMap.Add(declaration, additionalMembers);
            }

            additionalMembers.AddRange(members);
        }

        /// <summary>
        /// Gets the additional member declarations for the parent declaration.
        /// </summary>
        public ImmutableList<Declaration> GetAdditionalMembers(Declaration declaration)
        {
            _declToAddMembersMap.TryGetValue(declaration, out var additionalMembers);
            return additionalMembers?.ToImmutableList() ?? ImmutableList<Declaration>.Empty;
        }
    }
}