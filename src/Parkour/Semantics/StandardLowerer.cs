namespace Parkour.Semantics;

using Symbols;

public class StandardLowerer : SemanticLowerer
{
    protected SemanticBinder Binder { get; }

    public StandardLowerer(
        SemanticBinder? binder = null)
    {
        this.Binder = binder ?? new StandardBinder();
    }

    public override SemanticLowering Lower(
        ImmutableList<SemanticElement> elements,
        SymbolTable symbols)
    {
        var lowerers = new List<SemanticLowerer>();
        this.GetElementBasedLowerers(elements, lowerers);
        this.GetStandardLowerers(lowerers);

        var diagnostics = new List<Diagnostic>();
        var newElements = elements;

        foreach (var lowerer in lowerers)
        {
            var lowering = lowerer.Lower(newElements, symbols);
            newElements = lowering.Elements;
            diagnostics.AddRange(lowering.Diagnostics);
        }

        if (newElements != elements)
        {
            var bound = this.Binder.Bind(newElements, symbols);
            return new SemanticLowering(bound.Elements, bound.Diagnostics);
        }
        else
        {
            return new SemanticLowering(elements, ImmutableList<Diagnostic>.Empty);
        }
    }

    /// <summary>
    /// Gets all the standard lowerers that are always applied.
    /// </summary>
    protected virtual void GetStandardLowerers(
        List<SemanticLowerer> lowerers)
    {
        lowerers.Add(FieldInitializerLowerer.Instance);
    }

    /// <summary>
    /// Gets all the lowerers needed as determined by the 
    /// elements involved.
    /// </summary>
    private void GetElementBasedLowerers(
        ImmutableList<SemanticElement> elements,
        List<SemanticLowerer> lowerers)
    {
        var map = new Dictionary<Type, SemanticLowerer>();
        
        foreach (var elem in elements)
        {
            Gather(elem);
        }

        void Gather(SemanticElement element)
        {
            var elementType = element.GetType();
            if (!map.ContainsKey(elementType))
            {
                var lowerer = element.CreateLowerer();
                if (lowerer != null)
                {
                    map.Add(elementType, lowerer);
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