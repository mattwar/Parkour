namespace Parkour.Binding;
using Symbols;

internal static class BindingDiagnostics
{
    public static Diagnostic UnknownName(string name) =>
        new Diagnostic($"The name '{name}' does not match anything in this context.");

    public static Diagnostic UnknownSymbol(string name) =>
        new Diagnostic($"The name '{name}' does not match known symbol.");

    public static Diagnostic UnknownOperator(string kind) =>
        new Diagnostic($"The name '{kind}' does not match a known operator.");

    public static Diagnostic UnknownFunction(string name) =>
        new Diagnostic($"The name '{name}' does not match a function in this context.");

    public static Diagnostic ExpressionIsNotType() =>
        new Diagnostic("The expression is not a type.");

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
        new Diagnostic("The call is ambiguous, it has multiple best candidates.");

    public static Diagnostic IncorrectNumberOfArguments() =>
        new Diagnostic($"Incorrect number of arguments.");

    public static Diagnostic NoOperatorDefined() =>
        new Diagnostic($"No operator defined for the operands.");

    public static Diagnostic OperatorIsAmbiguous() =>
        new Diagnostic($"The operator is ambiguous, it has multiple candidates.");

    public static Diagnostic IncorrectNumberOfOperands() =>
        new Diagnostic($"Incorrect number of operands.");

    public static Diagnostic NoConstructorFound() =>
        new Diagnostic($"No suitable constructor found");

    public static Diagnostic ConstructorsAreAmbiguous() =>
        new Diagnostic($"No single best constructor can be determined");

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

    public static Diagnostic NoTypeOrMethodWithMatchingArityToConstruct() =>
        new Diagnostic("No type or method with matching arity to construct.");

    public static Diagnostic MethodDoesNotHaveMatchingArity() =>
        new Diagnostic("The method does not have the same arity as the number of type arguments.");

    public static Diagnostic TypeDoesNotHaveMatchingArity() =>
        new Diagnostic("The type does not have the same arity as the number of type arguments.");

    public static Diagnostic NoReferencedSymbolsHaveMatchingArity() =>
        new Diagnostic("No referenced symbol have mathing arity.");

    public static Diagnostic CannotInferElementType() =>
        new Diagnostic("Cannot infer element type.");

    public static Diagnostic ReferencedSymbolNotType() =>
        new Diagnostic("The referenced Symbol is not a type.");

    public static Diagnostic NoSettableIndexer() =>
        new Diagnostic("The is no settable indexer available.");

    public static Diagnostic NoMatchingIndexer() =>
        new Diagnostic("There is no matching indexer available.");

    public static Diagnostic IndexerIsAmbiguous() =>
        new Diagnostic("The indexing operation is ambiguous, there are multiple best candidates.");
}
