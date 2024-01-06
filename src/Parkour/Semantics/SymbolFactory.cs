namespace Parkour.Semantics;

public static class SymbolFactory
{
    public static Symbol.Variable Variable(string name, Symbol.Type type) =>
        new Symbol.Variable(name, type);

    public static Symbol.Field Field(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Symbol.Type fieldType) =>
        new Symbol.Field(name, container, access, modifier, fieldType);

    public static Symbol.Property Property(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Symbol.Type propertyType) =>
        new Symbol.Property(name, container, access, modifier, propertyType);

    public static Symbol.Type Class(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Symbol.Type baseType, ImmutableList<Symbol> members) =>
        new Symbol.Type(name, container, access, modifier, () => baseType, (c) => members);

    public static Symbol.Method Method(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Symbol.Type returnType, ImmutableList<Symbol.Parameter> parameters) =>
        new Symbol.Method(name, container, access, modifier, parameters, returnType);

    public static Symbol.Constructor Constructor(Symbol? container, SymbolAccess access, SymbolModifier modifier, Symbol.Type returnType, ImmutableList<Symbol.Parameter> parameters) =>
        new Symbol.Constructor(container, access, modifier, parameters, returnType);

    public static Symbol.Parameter Parameter(string name, Func<Symbol.Type> fnType) =>
        new Symbol.Parameter(name, fnType);

    public static Symbol.Parameter Parameter(string name, Symbol.Type type) =>
        new Symbol.Parameter(name, type);

    public static Symbol.Function Function(string name, Symbol.Type returnType, ImmutableList<Symbol.Parameter> parameters) =>
        new Symbol.Function(name, parameters, returnType);

    public static Symbol.Function Function(string name, Symbol.Type returnType, IEnumerable<Symbol.Parameter> parameters) =>
        Function(name, returnType, parameters.ToImmutableList());

    public static Symbol.Function Function(string name, Symbol.Type returnType, params Symbol.Parameter[] parameters) =>
        Function(name, returnType, parameters.ToImmutableList());

    public static Symbol.Function Operator(string name, Symbol.Type returnType, ImmutableList<Symbol.Parameter> parameters) =>
        new Symbol.OperatorFunction(name, parameters, returnType);

    public static Symbol.Function Operator(string name, Symbol.Type returnType, IEnumerable<Symbol.Parameter> parameters) =>
        Operator(name, returnType, parameters.ToImmutableList());

    public static Symbol.Function Operator(string name, Symbol.Type returnType, params Symbol.Parameter[] parameters) =>
        Operator(name, returnType, parameters.ToImmutableList());

    public static Symbol.Function Intrinsic(string name, Symbol.Function related, Symbol.Type returnType, ImmutableList<Symbol.Parameter> parameters) =>
        new Symbol.IntrinsicFunction(name, parameters, returnType, related);

    public static Symbol.Function Intrinsic(string name, Symbol.Function related, Symbol.Type returnType, IEnumerable<Symbol.Parameter> parameters) =>
        Intrinsic(name, related, returnType, parameters.ToImmutableList());

    public static Symbol.Function Intrinsic(string name, Symbol.Function related, Symbol.Type returnType, params Symbol.Parameter[] parameters) =>
        Intrinsic(name, related, returnType, parameters.ToImmutableList());
}