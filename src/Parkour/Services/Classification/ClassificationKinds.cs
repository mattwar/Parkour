namespace Parkour.Services;

/// <summary>
/// This is a set of common classifications that a language service may produce.
/// </summary>

public abstract class ClassificationKind
{
    private string? _cachedToString;

    public override string ToString() => _cachedToString ??= this.GetType().Name.ToLower();

    #region LSP predefined token types
    /// <summary>
    /// A namespace like module, package, or namespace.
    /// </summary>
    public sealed class Namespace : ClassificationKind;

    /// <summary>
    /// A type not otherwise classified.
    /// </summary>
    public sealed class Type : ClassificationKind;

    /// <summary>
    /// A class type
    /// </summary>
    public sealed class Class : ClassificationKind;

    /// <summary>
    /// An enum type
    /// </summary>
    public sealed class Enum : ClassificationKind;

    /// <summary>
    /// An interface type
    /// </summary>
    public sealed class Interface : ClassificationKind;

    /// <summary>
    /// A struct type
    /// </summary>
    public sealed class Struct : ClassificationKind;

    /// <summary>
    /// A generic type parameter
    /// </summary>
    public sealed class TypeParameter : ClassificationKind;

    /// <summary>
    /// A method or function parameter
    /// </summary>
    public sealed class Parameter : ClassificationKind;

    /// <summary>
    /// A local variable or field.
    /// </summary>
    public sealed class Variable : ClassificationKind;

    /// <summary>
    /// A property
    /// </summary>
    public sealed class Property : ClassificationKind;

    /// <summary>
    /// A enum member
    /// </summary>
    public sealed class EnumMember : ClassificationKind;

    /// <summary>
    /// An event member (field-like or property-like)
    /// </summary>
    public sealed class Event : ClassificationKind;

    /// <summary>
    /// A function
    /// </summary>
    public sealed class Function : ClassificationKind;

    /// <summary>
    /// A method member
    /// </summary>
    public sealed class Method : ClassificationKind;

    /// <summary>
    /// A macro
    /// </summary>
    public sealed class Macro : ClassificationKind;

    /// <summary>
    /// A keyword
    /// </summary>
    public sealed class Keyword : ClassificationKind;

    /// <summary>
    /// A declaration modifier
    /// </summary>
    public sealed class Modifier : ClassificationKind;

    /// <summary>
    /// A comment
    /// </summary>
    public sealed class Comment : ClassificationKind;

    /// <summary>
    /// A string literal
    /// </summary>
    public sealed class String : ClassificationKind;

    /// <summary>
    /// A numeric literal
    /// </summary>
    public sealed class Number : ClassificationKind;

    /// <summary>
    /// A regular expression literal
    /// </summary>
    public sealed class RegExp : ClassificationKind;

    /// <summary>
    /// An operator
    /// </summary>
    public sealed class Operator : ClassificationKind;

    /// <summary>
    /// A decorator (annotation/attribute?)
    /// </summary>
    public sealed class Decorator : ClassificationKind;

    #endregion

    #region Additional syntax/semantic kinds
    /// <summary>
    /// A field member
    /// </summary>
    public sealed class Field : ClassificationKind;

    /// <summary>
    /// Plain Text
    /// </summary>
    public sealed class Text : ClassificationKind;

    /// <summary>
    /// Punctation like commas, semicolons, brackets, etc.
    /// </summary>
    public sealed class Punctuation : ClassificationKind;

    /// <summary>
    /// Non-comment trivia like whitespace, newlines, etc.
    /// </summary>
    public sealed class Trivia : ClassificationKind;

    /// <summary>
    /// Intrinsic operators or types defined by the language.
    /// </summary>
    public sealed class Intrinsic : ClassificationKind;

    /// <summary>
    /// Literals like numbers, strings, etc, not otherwise classified.
    /// </summary>
    public sealed class Literal : ClassificationKind;

    /// <summary>
    /// A date/time literal
    /// </summary>
    public sealed class DateTime : ClassificationKind;

    /// <summary>
    /// A boolean literal
    /// </summary>
    public sealed class Boolean : ClassificationKind;

    /// <summary>
    /// An name that is not otherwise classified
    /// </summary>
    public sealed class Name : ClassificationKind;

    /// <summary>
    /// A label within code, such as for goto statements
    /// </summary>
    public sealed class Label : ClassificationKind;
    #endregion
}

public static class ClassificationKindExtensions
{
    private static readonly ClassificationKind _keyword = new ClassificationKind.Keyword();
    private static readonly ClassificationKind _punctuation = new ClassificationKind.Punctuation();
    private static readonly ClassificationKind _trivia = new ClassificationKind.Trivia();
    private static readonly ClassificationKind _comment = new ClassificationKind.Comment();
    private static readonly ClassificationKind _decoarator = new ClassificationKind.Decorator();
    private static readonly ClassificationKind _literal = new ClassificationKind.Literal();
    private static readonly ClassificationKind _number = new ClassificationKind.Number();
    private static readonly ClassificationKind _string = new ClassificationKind.String();
    private static readonly ClassificationKind _dateTime = new ClassificationKind.DateTime();
    private static readonly ClassificationKind _operator = new ClassificationKind.Operator();
    private static readonly ClassificationKind _boolean = new ClassificationKind.Boolean();
    private static readonly ClassificationKind _intrinsic = new ClassificationKind.Intrinsic();
    private static readonly ClassificationKind _name = new ClassificationKind.Name();
    private static readonly ClassificationKind _namespace = new ClassificationKind.Namespace();
    private static readonly ClassificationKind _type = new ClassificationKind.Type();
    private static readonly ClassificationKind _struct = new ClassificationKind.Struct();
    private static readonly ClassificationKind _class = new ClassificationKind.Class();
    private static readonly ClassificationKind _interface = new ClassificationKind.Interface();
    private static readonly ClassificationKind _enum = new ClassificationKind.Enum();
    private static readonly ClassificationKind _enumMember = new ClassificationKind.EnumMember();
    private static readonly ClassificationKind _typeParameter = new ClassificationKind.TypeParameter();
    private static readonly ClassificationKind _function = new ClassificationKind.Function();
    private static readonly ClassificationKind _method = new ClassificationKind.Method();
    private static readonly ClassificationKind _property = new ClassificationKind.Property();
    private static readonly ClassificationKind _field = new ClassificationKind.Field();
    private static readonly ClassificationKind _macro = new ClassificationKind.Macro();
    private static readonly ClassificationKind _variable = new ClassificationKind.Variable();
    private static readonly ClassificationKind _parameter = new ClassificationKind.Parameter();
    private static readonly ClassificationKind _label = new ClassificationKind.Label();
    private static readonly ClassificationKind _modifier = new ClassificationKind.Modifier();
    private static readonly ClassificationKind _event = new ClassificationKind.Event();
    private static readonly ClassificationKind _regexp = new ClassificationKind.RegExp();


    private static readonly ClassificationKind _text = new ClassificationKind.Text();

    extension(ClassificationKind)
    {
        public static ClassificationKind Text() => _text;
        public static ClassificationKind Keyword() => _keyword;
        public static ClassificationKind Punctuation() => _punctuation;
        public static ClassificationKind Trivia() => _trivia;
        public static ClassificationKind Comment() => _comment;
        public static ClassificationKind Decorator() => _decoarator;
        public static ClassificationKind Literal() => _literal;
        public static ClassificationKind Number() => _number;
        public static ClassificationKind String() => _string;
        public static ClassificationKind DateTime() => _dateTime;
        public static ClassificationKind Operator() => _operator;
        public static ClassificationKind Boolean() => _boolean;
        public static ClassificationKind Intrinsic() => _intrinsic;
        public static ClassificationKind Name() => _name;
        public static ClassificationKind Namespace() => _namespace;
        public static ClassificationKind Type() => _type;
        public static ClassificationKind Struct() => _struct;
        public static ClassificationKind Class() => _class;
        public static ClassificationKind Interface() => _interface;
        public static ClassificationKind Enum() => _enum;
        public static ClassificationKind EnumMember() => _enumMember;
        public static ClassificationKind TypeParameter() => _typeParameter;
        public static ClassificationKind Function() => _function;
        public static ClassificationKind Method() => _method;
        public static ClassificationKind Property() => _property;
        public static ClassificationKind Field() => _field;
        public static ClassificationKind Macro() => _macro;
        public static ClassificationKind Variable() => _variable;
        public static ClassificationKind Parameter() => _parameter;
        public static ClassificationKind Label() => _label;
        public static ClassificationKind Modifier() => _modifier;
        public static ClassificationKind Event() => _event;
        public static ClassificationKind RegExp() => _regexp;
    }
}
