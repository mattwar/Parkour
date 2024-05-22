namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Applies all standard lowerers and all element specific lowerers
/// </summary>
public class StandardLowerer : SemanticLowerer
{
    protected SemanticBinder Binder { get; }

    public StandardLowerer(
        SemanticBinder? binder = null)
    {
        this.Binder = binder ?? new StandardBinder();
    }

    public override SemanticLowering Lower(
        SemanticBinding binding)
    {
        var lowerers = new List<PartialLowerer>();
        this.GetElementBasedLowerers(binding.Elements, lowerers);
        lowerers.AddRange(this.GetStandardLowerers());

        var newElements = binding.Elements;

        foreach (var lowerer in lowerers)
        {
            newElements = lowerer.Lower(newElements, binding.ImportedSymbols);
        }

        if (newElements != binding.Elements)
        {
            var newBinding = this.Binder.Bind(newElements, binding.ImportedSymbols);
            return new SemanticLowering(newBinding.Elements, binding.ImportedSymbols, newBinding.CombinedSymbols);
        }
        else
        {
            return new SemanticLowering(binding.Elements, binding.ImportedSymbols, binding.CombinedSymbols);
        }
    }

    /// <summary>
    /// Gets all the standard lowerers that are always applied.
    /// </summary>
    protected virtual ImmutableList<PartialLowerer> GetStandardLowerers()
    {
        return [
            FieldInitializerLowerer.Instance,
            TopLevelExpressionLowerer.Instance,
            ];
    }

    /// <summary>
    /// Gets all the lowerers needed as determined by the 
    /// elements involved.
    /// </summary>
    private void GetElementBasedLowerers(
        ImmutableList<SemanticElement> elements,
        List<PartialLowerer> lowerers)
    {
        var map = new Dictionary<Type, PartialLowerer>();
        
        foreach (var elem in elements)
        {
            Gather(elem);
        }

        void Gather(SemanticElement element)
        {
            var elementType = element.GetType();
            if (!map.ContainsKey(elementType))
            {
                if (element.Lowerer != null)
                {
                    map.Add(elementType, element.Lowerer);
                }
            }

            for (int i = 0, n = element.ChildCount; i < n; i++)
            {
                var child = element.GetChild(i);
                if (child != null)
                    Gather(child);
            }
        }
    }
}