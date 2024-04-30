using Parkour.Binding;
using Parkour.Symbols;
using Parkour.Semantics;

namespace Parkour.Lowering;

public class StandardLowerer : Lowerer
{
    public StandardLowerer()
    {
    }

    public override DeclarationLowering Lower(DeclarationBinding binding)
    {
        // currently does nothing but repackage the binding results
        return new DeclLowering(binding);
    }

    public override ExpressionLowering Lower(ExpressionBinding binding)
    {
        // currently does nothing but repackage the binding results
        return new ExpressionLowering(binding, binding.BoundExpression);
    }

    private class DeclLowering : DeclarationLowering
    {
        private readonly DeclarationBinding _binding;

        public override DeclarationBinding Binding =>
            _binding;

        public override ImmutableList<Declaration> LoweredDeclarations =>
            _binding.BoundDeclarations;

        public override GlobalNamespaceSymbol LoweredSymbols => 
            _binding.BoundSymbols;

        public override ImmutableList<Diagnostic> Diagnostics =>
            ImmutableList<Diagnostic>.Empty;

        public DeclLowering(DeclarationBinding binding)
        {
            _binding = binding;
        }

        public override MethodDeclaration? GetMethodDeclaration(MethodSymbol methodSymbol)
        {
            return _binding.GetSymbolDeclarations(methodSymbol).OfType<MethodDeclaration>().FirstOrDefault();
        }

        public override ConstructorDeclaration? GetConstructorDeclaration(ConstructorSymbol constructorSymbol)
        {
            return _binding.GetSymbolDeclarations(constructorSymbol).OfType<ConstructorDeclaration>().FirstOrDefault();
        }
    }
}