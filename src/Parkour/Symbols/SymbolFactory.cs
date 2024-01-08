namespace Parkour.Symbols;

public static class SymbolFactory
{
    public static VariableSymbol Variable(string name, TypeSymbol type) =>
        new VariableSymbol(name, type);

    public static FieldSymbol Field(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol fieldType) =>
        new FieldSymbol(name, container, access, modifier, fieldType);

    public static PropertySymbol Property(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol propertyType) =>
        new PropertySymbol(name, container, access, modifier, propertyType);

    public static TypeSymbol Class(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol baseType, ImmutableList<Symbol> members) =>
        new TypeSymbol(name, container, access, modifier, ImmutableList.Create(baseType), members);

    public static MethodSymbol Method(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol returnType, ImmutableList<ParameterSymbol> parameters) =>
        new MethodSymbol(name, container, access, modifier, parameters, returnType);

    public static ConstructorSymbol Constructor(Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol returnType, ImmutableList<ParameterSymbol> parameters) =>
        new ConstructorSymbol(container, access, modifier, parameters, returnType);

    public static ParameterSymbol Parameter(string name, Func<TypeSymbol> fnType) =>
        new ParameterSymbol(name, fnType);

    public static ParameterSymbol Parameter(string name, TypeSymbol type) =>
        new ParameterSymbol(name, type);

    public static FunctionSymbol Function(string name, TypeSymbol returnType, ImmutableList<ParameterSymbol> parameters) =>
        new FunctionSymbol(name, parameters, returnType);

    public static FunctionSymbol Function(string name, TypeSymbol returnType, IEnumerable<ParameterSymbol> parameters) =>
        Function(name, returnType, parameters.ToImmutableList());

    public static FunctionSymbol Function(string name, TypeSymbol returnType, params ParameterSymbol[] parameters) =>
        Function(name, returnType, parameters.ToImmutableList());

    public static FunctionSymbol Operator(string name, string kind, TypeSymbol returnType, ImmutableList<ParameterSymbol> parameters) =>
        new OperatorSymbol(name, kind, parameters, returnType);

    public static FunctionSymbol Operator(string name, string kind, TypeSymbol returnType, IEnumerable<ParameterSymbol> parameters) =>
        Operator(name, kind, returnType, parameters.ToImmutableList());

    public static FunctionSymbol Operator(string name, string kind, TypeSymbol returnType, params ParameterSymbol[] parameters) =>
        Operator(name, kind, returnType, parameters.ToImmutableList());

#if false
    public static FunctionSymbol Intrinsic(string name, FunctionSymbol related, TypeSymbol returnType, ImmutableList<ParameterSymbol> parameters) =>
        new IntrinsicSymbol(name, parameters, returnType, related);

    public static FunctionSymbol Intrinsic(string name, FunctionSymbol related, TypeSymbol returnType, IEnumerable<ParameterSymbol> parameters) =>
        Intrinsic(name, related, returnType, parameters.ToImmutableList());

    public static FunctionSymbol Intrinsic(string name, FunctionSymbol related, TypeSymbol returnType, params ParameterSymbol[] parameters) =>
        Intrinsic(name, related, returnType, parameters.ToImmutableList());
#endif
}