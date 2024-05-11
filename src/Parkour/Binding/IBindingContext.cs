namespace Parkour.Binding;

public interface IBindingContext
{
    ImmutableList<ISymbol> GetSymbolsInScope(
        ISourceDocument document, 
        int position);
}