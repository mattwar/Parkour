namespace Parkour.Symbols;

public class OperatorSymbol : DelegateSymbol
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
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
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

    public override int ReferenceCount => 2;
    public override Symbol? GetReference(int index) =>
        index switch
        {
            0 => this.CheckedMethod,
            1 => this.UncheckMethod,
            _ => null
        };
}
