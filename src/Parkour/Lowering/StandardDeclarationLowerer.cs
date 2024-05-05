using Parkour.Binding;
using Parkour.Symbols;
using Parkour.Semantics;

namespace Parkour.Lowering;

public class StandardDeclarationLowerer : DeclarationLowerer
{
    public StandardDeclarationLowerer()
    {
    }

    public override DeclarationLowering LowerDeclarations(
        ImmutableList<Declaration> declarations,
        SymbolTable externalSymbols)
    {
        // currently does nothing but repackage the binding results
        return new DeclLowering(declarations, ImmutableList<Diagnostic>.Empty);
    }

    public override ExpressionLowering LowerExpression(
        Expression expression,
        SymbolTable externalSymbols)
    {
        // currently does nothing but repackage the binding results
        return new ExpressionLowering(expression, ImmutableList<Diagnostic>.Empty);
    }

    private class DeclLowering : DeclarationLowering
    {
        public override ImmutableList<Declaration> Declarations { get; }
        public override ImmutableList<Diagnostic> Diagnostics { get; }

        public DeclLowering(
            ImmutableList<Declaration> declarations,
            ImmutableList<Diagnostic> diagnostics)
        {
            this.Declarations = declarations;
            this.Diagnostics = diagnostics;
        }
    }
}