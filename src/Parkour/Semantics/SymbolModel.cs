using System.Reflection;
using System.Runtime.CompilerServices;
using static Parkour.Semantics.Symbol;

namespace Parkour.Semantics;

public abstract class SymbolModel
{
    public abstract Symbol.Namespace GlobalNamespace { get; }

    public static readonly Symbol.Type Unknown = new Symbol.Type("Unknown", typeof(object));
    public static readonly Symbol.Type Null = new Symbol.Type("Null", typeof(object));
    public static readonly Symbol.Type Any = new Symbol.Type("Any", typeof(object));
    public static readonly Symbol.Type Void = new Symbol.Type("Void", typeof(void));

    public Symbol.Type? _typeType;
    public Symbol.Type Type => _typeType ??= GetType(typeof(System.Type));

    private Symbol.Type? _boolType;
    public Symbol.Type Boolean => _boolType ??= GetType(typeof(bool));

    private Symbol.Type? _byteType;
    public Symbol.Type Byte => _byteType ??= GetType(typeof(byte));

    private Symbol.Type? _int16Type;
    public Symbol.Type Int16 => _int16Type ??= GetType(typeof(short));

    private Symbol.Type? _int32Type;
    public Symbol.Type Int32 => _int32Type ??= GetType(typeof(int));

    private Symbol.Type? _int64Type;
    public Symbol.Type Int64 => _int64Type ??= GetType(typeof(long));

    private Symbol.Type? _float32Type;
    public Symbol.Type Single => _float32Type ??= GetType(typeof(float));

    private Symbol.Type? _float64Type;
    public Symbol.Type Double => _float64Type ??= GetType(typeof(double));

    private Symbol.Type? _decimalType;
    public Symbol.Type Decimal => _decimalType ??= GetType(typeof(decimal));

    private Symbol.Type? _charType;
    public Symbol.Type Char => _charType ??= GetType(typeof(char));

    private Symbol.Type? _stringType;
    public Symbol.Type String => _stringType ??= GetType(typeof(string));

    private Symbol.Type? _objectType;
    public Symbol.Type Object => _objectType ??= GetType(typeof(object));

    /// <summary>
    /// Gets the type based on equivalent runtime type.
    /// </summary>
    public virtual Symbol.Type GetType(System.Type type) =>
        (Symbol.Type)GetSymbol(type.FullName!)!;

    /// <summary>
    /// Gets the symbol corresponding the symbol's full name. (ie System.Int32)
    /// </summary>
    public virtual Symbol? GetSymbol(string fullname)
    {
        var parts = fullname.Split('.', '+');

        Symbol? symbol = null;
        Symbol? container = GlobalNamespace;

        foreach (var part in parts)
        {
            if (container == null)
                break;

            symbol = container.Members.FirstOrDefault(m => m.Name == part);
            container = symbol;
        }

        return symbol;
    }

    private readonly ConditionalWeakTable<ImmutableList<Symbol>, Group> _listToGroupMap =
        new ConditionalWeakTable<ImmutableList<Symbol>, Group>();

    private readonly ConditionalWeakTable<ImmutableList<Symbol.Type>, Union> _listToUnionMap =
        new ConditionalWeakTable<ImmutableList<Symbol.Type>, Union>();


    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public virtual Symbol.Array GetArray(Symbol.Type elementType) =>
        new Symbol.Array(elementType);

    /// <summary>
    /// Gets a list of the specified element type.
    /// </summary>
    public virtual Symbol.List GetList(Symbol.Type elementType) =>
        new Symbol.List(elementType);

    /// <summary>
    /// Gets the union or individual type from a list of types.
    /// </summary>
    public virtual Symbol.Type GetUnion(IEnumerable<Symbol.Type> types)
    {
        if (!types.Any())
            return SymbolModel.Void;

        if (types is IReadOnlyList<Symbol.Type> roTypes
            && roTypes.Count == 1)
        {
            return roTypes[0];
        }

        var immutableTypes = types as ImmutableList<Symbol.Type>;
        if (immutableTypes != null
            && _listToUnionMap.TryGetValue(immutableTypes, out var union))
        {
            return union;
        }

        types = FlattenUnions(types).ToList();

        var hasUnknown = types.Any(t => t == SymbolModel.Unknown);
        var hasAny = types.Any(t => t == SymbolModel.Any);
        var hasVoid = types.Any(t => t == SymbolModel.Void);
        var hasNull = types.Any(t => t == SymbolModel.Null);

        var canonicalTypes = types
            .Where(t =>
                t == SymbolModel.Unknown
                || t == SymbolModel.Any && !hasUnknown
                || t == SymbolModel.Null && !hasUnknown
                || t == SymbolModel.Void && !hasUnknown
                || (!hasUnknown || !hasAny))
            .DistinctBy(t => t, TypeEqualityComparer.Instance)
            .OrderBy(t => t.Name)
            .ToImmutableList();

        if (canonicalTypes.Count == 1)
            return canonicalTypes[0];

        union = _listToUnionMap.GetValue(canonicalTypes, _newTypes => new Union(_newTypes));

        // also associate union with original list, if it was immutable
        if (immutableTypes != null)
        {
            _listToUnionMap.GetValue(immutableTypes, _ => union);
        }

        return union;

        static IEnumerable<Symbol.Type> FlattenUnions(IEnumerable<Symbol.Type> types)
        {
            foreach (var type in types)
            {
                if (type is Union union)
                {
                    foreach (var unionType in union.Types)
                        yield return unionType;
                }

                yield return type;
            }
        }
    }

    /// <summary>
    /// Gets the union or individual type from a list of types.
    /// </summary>
    public virtual Symbol.Type GetUnion(params Symbol.Type[] types) =>
        GetUnion((IEnumerable<Symbol.Type>)types);

    /// <summary>
    /// Gets the group or individual symbol for the specified symbols.
    /// </summary>
    public virtual Symbol GetGroup(IEnumerable<Symbol> symbols)
    {
        if (!symbols.Any())
            return SymbolModel.Void;

        if (symbols is IReadOnlyList<Symbol> roSymbols
            && roSymbols.Count == 1)
        {
            return roSymbols[0];
        }

        var immutableSymbols = symbols as ImmutableList<Symbol>;
        if (immutableSymbols != null
            && _listToGroupMap.TryGetValue(immutableSymbols, out var group))
        {
            return group;
        }

        var canonicalSymbols = symbols
            .DistinctBy(s => s, SymbolEqualityComparer.Instance)
            .OrderBy(s => s.Name)
            .ToImmutableList();

        if (canonicalSymbols.Count == 1)
            return canonicalSymbols[0];

        group = _listToGroupMap.GetValue(canonicalSymbols, _symbols => new Group(_symbols));

        // also associate union with original list, if it was immutable
        if (immutableSymbols != null)
        {
            _listToGroupMap.GetValue(immutableSymbols, _ => group);
        }

        return group;
    }

    /// <summary>
    /// Gets the group or individual symbol for the specified symbols.
    /// </summary>
    public virtual Symbol GetGroup(params Symbol[] symbols) =>
        GetGroup((IEnumerable<Symbol>)symbols);
}

public class RuntimeSymbolModel : SymbolModel
{
    public ImmutableList<Assembly> Assemblies { get; }

    private ConditionalWeakTable<object, Symbol> _runtimeToSymbolMap =
        new ConditionalWeakTable<object, Symbol>();

    public override Namespace GlobalNamespace { get; }

    public RuntimeSymbolModel(ImmutableList<Assembly>? assemblies = null)
    {
        this.Assemblies = assemblies ?? _defaultAssemblies;
        var types = this.Assemblies.SelectMany(a => a.GetTypes()).ToList();
        this.GlobalNamespace = GetNamespace("", "", types);
    }

    private static ImmutableList<Assembly> _defaultAssemblies =
        ImmutableList.Create(typeof(int).Assembly);

    /// <summary>
    /// Gets a namespace symbol containing the types and namespaces as members.
    /// </summary>
    private Symbol.Namespace GetNamespace(string containingName, string name, IReadOnlyList<System.Type> types)
    {
        var fullName = containingName.Length > 0 && name.Length > 0 
            ? containingName + "." + name
            : name;
        var nameWithDot = fullName.Length > 0 ? fullName + "." : fullName;
        var typesInNamespace = types.Where(t => t.Namespace == fullName).ToList();
        var nestedTypes = types.Where(t => t.Namespace != null && t.Namespace.Contains(nameWithDot)).ToList();
        var nestedNamespaces = nestedTypes.Select(t => t.Namespace).Where(n => n != null).Distinct();

        return new Symbol.Namespace(name, () =>
        {
            var list = new List<Symbol>();
            list.AddRange(typesInNamespace.Select(t => GetType(t)));
            list.AddRange(nestedNamespaces.Select(nn => GetNamespace(fullName, GetNextNamespaceName(fullName, nn!), nestedTypes)));
            return list.ToImmutableList();
        });

        static string GetNextNamespaceName(string fullName, string containingNamespace)
        {
            var start = containingNamespace.Length + 1;
            var nextDot = fullName.IndexOf('.', start);
            if (nextDot >= 0)
                return fullName.Substring(start, nextDot - start);
            return fullName.Substring(start);
        }
    }

    public override Symbol.Type GetType(System.Type type) =>
        (Symbol.Type)GetOrCreateSymbol(type);

    private Symbol GetOrCreateSymbol(object runtimeSymbol)
    {
        if (runtimeSymbol == (object)typeof(void))
            return SymbolModel.Void;

        if (!_runtimeToSymbolMap.TryGetValue(runtimeSymbol, out var symbol))
        {
            TryCreateSymbol(runtimeSymbol, out symbol);
        }

        return symbol;
    }

    public static SymbolAccess GetAccess(MemberInfo info) =>
        info switch
        {
            System.Type type =>
                type.IsPublic ? SymbolAccess.Public
                    : type.IsNestedPublic ? SymbolAccess.Public
                    : type.IsNestedFamily ? SymbolAccess.Protected
                    : type.IsNestedFamANDAssem ? SymbolAccess.ProtectedAndInternal
                    : type.IsNestedFamORAssem ? SymbolAccess.ProtectedOrInternal
                    : type.IsPublic ? SymbolAccess.Public
                    : type.IsNotPublic ? SymbolAccess.Internal
                    : SymbolAccess.Private,
            FieldInfo field =>
                field.IsPublic ? SymbolAccess.Public
                    : field.IsAssembly ? SymbolAccess.Internal
                    : field.IsFamily ? SymbolAccess.Protected
                    : field.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                    : field.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                    : SymbolAccess.Private,
            PropertyInfo property =>
                GetAccess(property.GetGetMethod()!),
            MethodBase method =>
                method.IsPublic ? SymbolAccess.Public
                    : method.IsAssembly ? SymbolAccess.Internal
                    : method.IsFamily ? SymbolAccess.Protected
                    : method.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                    : method.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                    : SymbolAccess.Private,
            _ => SymbolAccess.Private
        };

    public static SymbolModifier GetModifiers(MemberInfo info) =>
        info switch
        {
            System.Type type =>
                (type.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (type.IsSealed ? SymbolModifier.Sealed : SymbolModifier.None),
            FieldInfo field =>
                (field.IsStatic ? SymbolModifier.Static : SymbolModifier.None),
            PropertyInfo property =>
                GetModifiers(property.GetGetMethod()!),
            MethodBase method =>
                (method.IsStatic ? SymbolModifier.Static : SymbolModifier.None)
                | (method.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (method.IsVirtual ? SymbolModifier.Virtual : SymbolModifier.None)
                | (method.IsFinal ? SymbolModifier.Sealed : SymbolModifier.None)
                | (method.IsHideBySig ? SymbolModifier.HideBySig : SymbolModifier.None)
                | (method.IsSpecialName ? SymbolModifier.Special : SymbolModifier.None),
            _ => SymbolModifier.None
        };

    private ImmutableList<Parameter> GetParameters(MethodBase method) =>
        method.GetParameters().Select(p =>
            new Parameter(p.Name ?? "", GetType(p.ParameterType))).ToImmutableList();

    private bool TryCreateSymbol(object runtimeSymbol, out Symbol symbol)
    {
        symbol = null!;

        switch (runtimeSymbol)
        {
            case FieldInfo field:
                symbol = new Field(
                    field.Name,
                    field.DeclaringType != null ? GetType(field.DeclaringType) : null,
                    GetAccess(field),
                    GetModifiers(field),
                    GetType(field.FieldType),
                    field);
                break;

            case PropertyInfo property:
                symbol = new Property(
                    property.Name,
                    property.DeclaringType != null ? GetType(property.DeclaringType) : null,
                    GetAccess(property),
                    GetModifiers(property),
                    GetType(property.PropertyType),
                    property);
                break;

            case MethodInfo method:
                symbol = new Method(
                    method.Name,
                    method.DeclaringType != null ? GetType(method.DeclaringType) : null,
                    GetAccess(method),
                    GetModifiers(method),
                    GetParameters(method),
                    GetType(method.ReturnType),
                    method);
                break;

            case ConstructorInfo constructor:
                symbol = new Constructor(
                    constructor.DeclaringType != null ? GetType(constructor.DeclaringType) : null,
                    GetAccess(constructor),
                    GetModifiers(constructor),
                    GetParameters(constructor),
                    constructor.DeclaringType != null ? GetType(constructor.DeclaringType) : null,
                    constructor);
                break;

            case System.Type type:
                if (type.IsClass || type.IsValueType || type.IsInterface)
                {
                    symbol = new Symbol.Type(
                        type.Name,
                        type.DeclaringType != null ? GetType(type.DeclaringType) : null,
                        GetAccess(type),
                        GetModifiers(type),
                        type.BaseType != null ? () => GetType(type.BaseType) : null,
                        (c) => CreateMembers(type),
                        type);
                }
                else if (type.IsArray && type.GetElementType() is System.Type elementType)
                {
                    symbol = GetArray(GetType(elementType));
                }
                break;
        }

        if (symbol != null)
        {
            var tmp = symbol;
            symbol = _runtimeToSymbolMap.GetValue(runtimeSymbol, _ => tmp);
        }

        return symbol != null;
    }

    private ImmutableList<Symbol> CreateMembers(System.Type runtimeType) =>
        runtimeType.GetMembers()
            .Select(m => TryCreateSymbol(m, out var s) ? s : null)
            .Where(s => s != null)
            .ToImmutableList()!;
}
