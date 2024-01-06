using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Parkour.Semantics;
public abstract class Symbol
{
    public string Name { get; }
    public virtual ImmutableList<Symbol> Members => ImmutableList<Symbol>.Empty;

    public Symbol(string name)
    {
        this.Name = name;
    }
    #region Symbols

    public class Member : Symbol
    {
        public Symbol? Container { get; }
        public SymbolAccess Access { get; }
        public SymbolModifier Modifiers { get; }

        public bool IsStatic => (this.Modifiers & SymbolModifier.Static) != 0;

        public Member(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier)
            : base(name)
        {
            this.Container = container;
            this.Access = access;
            this.Modifiers = modifier;
        }
    }

    public class Namespace : Symbol
    {
        private Func<ImmutableList<Symbol>>? _fnMembers;
        private ImmutableList<Symbol>? _members;

        public override ImmutableList<Symbol> Members
        {
            get
            {
                 if (_members == null && _fnMembers != null)
                {
                    _members = _fnMembers();
                    _fnMembers = null;
                }

                return _members ?? ImmutableList<Symbol>.Empty;
            }
        }

        public Namespace(string name, Func<ImmutableList<Symbol>> fnMembers)
            : base(name)
        {
            _fnMembers = fnMembers;
        }

        public Namespace(string name, ImmutableList<Symbol> members)
            : base(name)
        {
            _members = members;
        }
    }

    public class Type : Member
    {
        private readonly Func<Type>? _fnBaseType;
        private Type? _baseType;
        private readonly Func<Type, ImmutableList<Symbol>>? _fnMembers;
        private ImmutableList<Symbol>? _members;

        public Type? BaseType => 
            _baseType ??= _fnBaseType != null ? _fnBaseType() : null;

        public override ImmutableList<Symbol> Members =>
            _members ??= _fnMembers != null ? _fnMembers(this) : ImmutableList<Symbol>.Empty;

        public System.Type? RuntimeType { get; }

        public Type(
            string name, 
            Symbol? container,
            SymbolAccess access,
            SymbolModifier modifier,
            Func<Type>? fnBaseType,
            Func<Type, ImmutableList<Symbol>>? fnMembers,
            System.Type? runtimeType = null)
            : base(name, container, access, modifier)
        {
            _fnBaseType = fnBaseType;
            _baseType = null;
            _fnMembers = fnMembers;
            _members = null;
            this.RuntimeType = runtimeType;
        }

        public Type(string name, System.Type? runtimeType = null)
            : this(name, container: null, SymbolAccess.Public, SymbolModifier.None, fnBaseType: null, fnMembers: null, runtimeType)
        {
        }
    }

    public sealed class Field : Member
    {
        public Type FieldType { get; }
        public FieldInfo? RuntimeField { get; }

        public Field(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Type fieldType, FieldInfo? runtimeField = null)
            : base(name, container, access, modifier)
        {
            this.FieldType = fieldType;
            this.RuntimeField = runtimeField;
        }
    }

    public sealed class Property : Member
    {
        public Type PropertyType { get; }
        public PropertyInfo? RuntimeProperty { get; }

        public Property(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, Type propertyType, PropertyInfo? runtimeProperty = null)
            : base(name, container, access, modifier)
        {
            this.PropertyType = propertyType;
            this.RuntimeProperty = runtimeProperty;
        }
    }

    public class Method : Member
    {
        public ImmutableList<Parameter> Parameters { get; }
        public Type ReturnType { get; }
        public MethodBase? RuntimeMethod { get; }

        public Method(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, ImmutableList<Parameter> parameters, Type? returnType = null, MethodBase? runtimeMethod = null)
            : base(name, container, access, modifier)
        {
            this.Parameters = parameters;
            this.ReturnType = returnType ?? SymbolModel.Unknown;
            this.RuntimeMethod = runtimeMethod;
        }
    }

    public class Constructor : Member
    {
        public ImmutableList<Parameter> Parameters { get; }
        public Type ReturnType { get; }
        public MethodBase? RuntimeMethod { get; }

        public Constructor(Symbol? container, SymbolAccess access, SymbolModifier modifier, ImmutableList<Parameter> parameters, Type? returnType = null, MethodBase? runtimeMethod = null)
            : base("", container, access, modifier)
        {
            this.Parameters = parameters;
            this.ReturnType = returnType ?? SymbolModel.Unknown;
            this.RuntimeMethod = runtimeMethod;
        }
    }

    public sealed class Array : Type
    {
        public Type ElementType { get; }

        public Array(Type elementType) : base($"Array({elementType.Name})") 
        {
            this.ElementType = elementType;
        }
    }

    public sealed class List : Type
    {
        public Type ElementType { get; }

        public List(Type elementType) : base($"List({elementType.Name})")
        {
            this.ElementType = elementType;
        }
    }

    public sealed class Union : Type
    {
        public ImmutableList<Type> Types { get; }

        internal Union(ImmutableList<Type> types)
            : base($"Union({string.Join(" | ", types.Select(t => t.Name))})")
        {
            this.Types = types;
        }
    }

    public sealed class Group : Type
    {
        public ImmutableList<Symbol> Symbols { get; }

        internal Group(ImmutableList<Symbol> symbols)
            : base($"Group({string.Join(", ", symbols.Select(t => t.Name))})")
        {
            this.Symbols = symbols;
        }
    }

    public sealed class Variable : Symbol
    {
        public Type VariableType { get; }

        public Variable(string name, Type variableType) : base(name)
        {
            this.VariableType = variableType;
        }
    }

    public sealed class Parameter : Symbol
    {
        private readonly Func<Type>? _fnParameterType;
        public ParameterInfo? RuntimeParameter { get; }
        
        private Type? _parameterType;
        public Type ParameterType
        {
            get
            {
                if (_parameterType == null )
                {
                    _parameterType = _fnParameterType != null ? _fnParameterType() : SymbolModel.Unknown;
                }

                return _parameterType;
            }
        }

        public Parameter(string name, Func<Type> fnParameterType, ParameterInfo? runtimeParameter = null) 
            : base(name)
        {
            _fnParameterType = fnParameterType;
        }

        public Parameter(string name, Type parameterType, ParameterInfo? runtimeParameter = null) 
            : base(name)
        {
            _parameterType = parameterType;
            this.RuntimeParameter = runtimeParameter;
        }
    }

    public class Function : Type
    {
        public ImmutableList<Parameter> Parameters { get; }
        public Type ReturnType { get; }
        public MethodBase? RuntimeMethod { get; }

        public Function(string name, ImmutableList<Parameter> parameters, Type? returnType, MethodBase? runtimeMethod = null)
            : base(name)
        {
            this.Parameters = parameters;
            this.ReturnType = returnType ?? SymbolModel.Unknown;
            this.RuntimeMethod = runtimeMethod;
        }

        public Function WithName(string name)
        {
            if (this.Name == null)
                return this;
            return new Function(name, this.Parameters, this.ReturnType, this.RuntimeMethod);
        }
    }

    public class IntrinsicFunction : Function
    {
        public Function RelatedFunction { get; }

        public IntrinsicFunction(string name, ImmutableList<Parameter> parameters, Type? returnType, Function relatedFunction)
            : base(name, parameters, returnType)
        {
            this.RelatedFunction = relatedFunction;
        }
    }

    public class OperatorFunction : Function
    {
        public OperatorFunction(string name, ImmutableList<Parameter> parameters, Type? returnType)
            : base(name, parameters, returnType)
        {
        }
    }

    public sealed class Target : Symbol
    {
        public new Type Type { get; }

        public Target(string name, Type? type)
            : base(name)
        {
            this.Type = type ?? SymbolModel.Void;
        }
    }

#endregion

    /// <summary>
    /// Gets the <see cref="Symbol.Type"/> that the variable/parameter/function would return.
    /// </summary>
    public Symbol.Type GetResultType() =>
        this switch
        {
            Symbol.Variable v => v.VariableType,
            Symbol.Parameter p => p.ParameterType,
            Symbol.Function f => f.ReturnType,
            _ => this as Symbol.Type ?? SymbolModel.Unknown
        };
}

public class TypeEqualityComparer : IEqualityComparer<Symbol.Type>
{
    public static TypeEqualityComparer Instance = new TypeEqualityComparer();

    private TypeEqualityComparer() { }

    public bool Equals(Symbol.Type? type1, Symbol.Type? type2)
    {
        // most types are singetons
        if (type1 == type2) return true;

        if (type1 == null && type2 == null) return true;
        if (type1 == null || type2 == null) return false;

        switch (type1)
        {
            case Symbol.Array array1 when type2 is Symbol.Array array2:
                return Equals(array1.ElementType, array2.ElementType);
            case Symbol.List list1 when type2 is Symbol.List list2:
                return Equals(list1.ElementType, list2.ElementType);
            case Symbol.Union union1 when type2 is Symbol.Union union2:
                if (union1.Types.Count != union2.Types.Count)
                    return false;
                for (int i = 0; i < union1.Types.Count; i++)
                {
                    if (!Equals(union1.Types[i], union2.Types[i]))
                        return false;
                }
                return true;
            case Symbol.Group group1 when type2 is Symbol.Group group2:
                if (group1.Symbols.Count != group2.Symbols.Count)
                    return false;
                for (int i = 0; i < group1.Symbols.Count; i++)
                {
                    if (!SymbolEqualityComparer.Instance.Equals(group1.Symbols[i], group2.Symbols[1]))
                        return false;
                }
                return true;
        }

        return false;
    }

    public int GetHashCode([DisallowNull] Symbol.Type type)
    {
        var hc = 0;

        switch (type)
        {
            case Symbol.Array array:
                hc = GetHashCode(array.ElementType);
                break;
            case Symbol.List list:
                hc = GetHashCode(list.ElementType);
                break;
            case Symbol.Union union:
                for (int i = 0; i < union.Types.Count; i++)
                {
                    hc = HashCode.Combine(hc, GetHashCode(union.Types[i]));
                }
                break;
            case Symbol.Group group:
                for (int i = 0; i < group.Symbols.Count; i++)
                {
                    hc = HashCode.Combine(hc, SymbolEqualityComparer.Instance.GetHashCode(group.Symbols[i]));
                }
                break;
            default:
                // rest are singleton types so we use the runtime default hashcode.
                hc = type.GetHashCode();
                break;
        }

        return hc;
    }
}

public class SymbolEqualityComparer : IEqualityComparer<Symbol>
{
    private SymbolEqualityComparer() { }
    public static SymbolEqualityComparer Instance = new SymbolEqualityComparer();

    public bool Equals(Symbol? symbol1, Symbol? symbol2)
    {
        if (symbol1 == symbol2) return true;

        if (symbol1 == null && symbol2 == null) return true;
        if (symbol1 == null || symbol2 == null) return false;

        switch (symbol1)
        {
            case Symbol.Type type1 when symbol2 is Symbol.Type type2:
                return TypeEqualityComparer.Instance.Equals(type1, type2);
            default:
                return false;
        }
    }

    public int GetHashCode([DisallowNull] Symbol symbol)
    {
        switch (symbol)
        {
            case Symbol.Type type:
                return TypeEqualityComparer.Instance.GetHashCode(type);
            default:
                return symbol.GetHashCode();
        }
    }
}

public enum SymbolAccess
{
    Public,
    Private,
    Protected,
    ProtectedAndInternal,
    ProtectedOrInternal,
    Internal
}

[Flags]
public enum SymbolModifier
{
    None = 0,
    Static = 0b0000_0001,
    Abstract = 0b0000_0010,
    Virtual = 0b0000_0100,
    Override = 0b0000_1000,
    Sealed = 0b0001_0000,
    HideBySig = 0b0010_0000,
    Special = 0b0100_0000,
    ReadOnly = 0b1000_0000
}