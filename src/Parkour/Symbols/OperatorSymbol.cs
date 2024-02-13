namespace Parkour.Symbols;

public class OperatorSymbol : FunctionSymbol
{
    public string Kind => this.Name;

    /// <summary>
    /// Method used in checked context (if not actually an intrinsic operator)
    /// </summary>
    public MethodSymbol? CheckedMethod { get; }

    /// <summary>
    /// Method used in unchecked context (if not actually an intrinsic operator)
    /// </summary>
    public MethodSymbol? UncheckMethod { get; }

    public OperatorSymbol(
        string kind,
        Func<FunctionSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType,
        MethodSymbol? checkedMethod = null,
        MethodSymbol? uncheckedMethod = null)
        : base(
            kind,
            null,
            fnParameters,
            fnReturnType)
    {
        this.CheckedMethod = checkedMethod;
        this.UncheckMethod = uncheckedMethod;
    }
}
