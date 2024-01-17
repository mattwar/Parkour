namespace Parkour.Binding;
using Symbols;

internal static class BindingDiagnostics
{
    public static Diagnostic UnknownName(string name) =>
        new Diagnostic($"The name '{name}' does not match anything in this context.");

    public static Diagnostic UnknownOperator(string kind) =>
        new Diagnostic($"The name '{kind}' does not match a known operator.");

    public static Diagnostic UnknownFunction(string name) =>
        new Diagnostic($"The name '{name}' does not match a function in this context.");

    public static Diagnostic NoMatchingFunction() =>
        new Diagnostic($"No functions match the arguments.");

    public static Diagnostic NoMatchingTarget(string name) =>
        new Diagnostic($"No matching branch target '{name}' in this context'");

    public static Diagnostic AmbiguousName(string name) =>
        new Diagnostic($"The name '{name}' matches more than one item in this context.");

    public static Diagnostic SymbolNotCallable(string type) =>
        new Diagnostic($"The symbol '{type}' cannot be invoked.");

    public static Diagnostic NoCallableSymbol() =>
        new Diagnostic($"No callable symbol is found.");

    public static Diagnostic CallIsAmbiguous() =>
        new Diagnostic("The call refers to more than one invokable symbol, and a best symbol cannot be determined.");

    public static Diagnostic CallHasIncorrectNumberOfArguments(string name) =>
        new Diagnostic($"The call to '{name}' has an incorrect number of arguments");

    public static Diagnostic CannotConvert(TypeSymbol source, TypeSymbol target) =>
        new Diagnostic($"Cannot convert from type '{source.Name}' to type '{target.Name}'.");

    public static Diagnostic NotAValidAssignmentTarget() =>
        new Diagnostic($"The expression is not a valid assignment target.");

    public static Diagnostic CannotPassValueToLabel(string label) =>
        new Diagnostic($"Cannot pass value to label '{label}'");

    public static Diagnostic DeclarationMustHaveTypeOrInitializer() =>
        new Diagnostic($"The variable declaration must have either a type or initializer.");

    public static Diagnostic DefaultTypeCannotBeInferred() =>
        new Diagnostic($"A type for the default expression cannot be inferred in this context.");

    public static Diagnostic NoCommonTypeFound() =>
        new Diagnostic("No common type can be found for the result of this expression.");

    public static Diagnostic NoCommonReturnTypeFound() =>
        new Diagnostic("No common return type can be found for the function.");

    public static Diagnostic FlowIntoLabelDoesNotMatchType() =>
        new Diagnostic("The flow of logic does not provide the label's expected value or compatible type.");
}
