namespace Parkour.Expressions;
using Symbols;

internal static class DiagnosticFactory
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

    public static Diagnostic CannotConvert(TypeSymbol source, TypeSymbol target) =>
        new Diagnostic($"Cannot convert from type '{source.Name}' to type '{target.Name}'.");
}
