namespace Parkour.Symbols;

public class OperatorSymbol : DelegateSymbol
{
    /// <summary>
    /// The kind of the operator.
    /// </summary>
    public Operator Operator { get; }

    /// <summary>
    /// Method used in checked context (if not actually an intrinsic operator)
    /// </summary>
    public MethodSymbol? CheckedMethod { get; }

    /// <summary>
    /// Method used in unchecked context (if not actually an intrinsic operator)
    /// </summary>
    public MethodSymbol? UncheckMethod { get; }

    public OperatorSymbol(
        Operator op,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType,
        MethodSymbol? checkedMethod = null,
        MethodSymbol? uncheckedMethod = null)
        : base(
            op.GetType().Name,
            null,
            fnParameters,
            fnReturnType)
    {
        this.Operator = op;
        this.CheckedMethod = checkedMethod;
        this.UncheckMethod = uncheckedMethod;
    }

    public override int ReferencedSymbolCount => 2;
    public override Symbol? GetReferencedSymbol(int index) =>
        index switch
        {
            0 => this.CheckedMethod,
            1 => this.UncheckMethod,
            _ => null
        };
}
