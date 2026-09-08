using System.Diagnostics.Tracing;

namespace Parkour.Services;

/// <summary>
/// The kind determines the glyph included with completion item in UI.
/// This specific list originates from LSP definition, but may include others.
/// </summary>

public abstract class CompletionKind
{
    public class Text : CompletionKind;
    public class Method : CompletionKind;
    public class Function : CompletionKind;
    public class Constructor : CompletionKind;
    public class Field : CompletionKind;
    public class Variable : CompletionKind;
    public class Class : CompletionKind;
    public class Interface : CompletionKind;
    public class Module : CompletionKind;
    public class Property : CompletionKind;
    public class Unit : CompletionKind;
    public class Value : CompletionKind;
    public class Enum : CompletionKind;
    public class Keyword : CompletionKind;
    public class Snippet : CompletionKind;
    public class Color : CompletionKind;
    public class File : CompletionKind;
    public class Reference : CompletionKind;
    public class Folder : CompletionKind;
    public class EnumMember : CompletionKind;
    public class Constant : CompletionKind;
    public class Struct : CompletionKind;
    public class Event : CompletionKind;
    public class Operator : CompletionKind;
    public class TypeParameter : CompletionKind;
    public class Macro : CompletionKind;
    public class Namespace : CompletionKind;
    public class Template : CompletionKind;
    public class TypeDefinition : CompletionKind;
    public class Union : CompletionKind;
    public class Delegate : CompletionKind;
    public class TagHelper : CompletionKind;
    public class ExtensionMethod : CompletionKind;
    public class Element : CompletionKind;
    public class LocalResource : CompletionKind;
    public class SystemResource : CompletionKind;
    public class CloseElement : CompletionKind;
}

public static class CompletionKindExtensions
{
    private static readonly CompletionKind _text = new CompletionKind.Text();
    private static readonly CompletionKind _method = new CompletionKind.Method();
    private static readonly CompletionKind _function = new CompletionKind.Function();
    private static readonly CompletionKind _constructor = new CompletionKind.Constructor();
    private static readonly CompletionKind _field = new CompletionKind.Field();
    private static readonly CompletionKind _variable = new CompletionKind.Variable();
    private static readonly CompletionKind _class = new CompletionKind.Class();
    private static readonly CompletionKind _interface = new CompletionKind.Interface();
    private static readonly CompletionKind _module = new CompletionKind.Module();
    private static readonly CompletionKind _property = new CompletionKind.Property();
    private static readonly CompletionKind _unit = new CompletionKind.Unit();
    private static readonly CompletionKind _value = new CompletionKind.Value();
    private static readonly CompletionKind _enum = new CompletionKind.Enum();
    private static readonly CompletionKind _keyword = new CompletionKind.Keyword();
    private static readonly CompletionKind _snippet = new CompletionKind.Snippet();
    private static readonly CompletionKind _color = new CompletionKind.Color();
    private static readonly CompletionKind _file = new CompletionKind.File();
    private static readonly CompletionKind _reference = new CompletionKind.Reference();
    private static readonly CompletionKind _folder = new CompletionKind.Folder();
    private static readonly CompletionKind _enumMember = new CompletionKind.EnumMember();
    private static readonly CompletionKind _constant = new CompletionKind.Constant();
    private static readonly CompletionKind _struct = new CompletionKind.Struct();
    private static readonly CompletionKind _event = new CompletionKind.Event();
    private static readonly CompletionKind _operator = new CompletionKind.Operator();
    private static readonly CompletionKind _typeParameter = new CompletionKind.TypeParameter();
    private static readonly CompletionKind _macro = new CompletionKind.Macro();
    private static readonly CompletionKind _namespace = new CompletionKind.Namespace();
    private static readonly CompletionKind _template = new CompletionKind.Template();
    private static readonly CompletionKind _typeDefinition = new CompletionKind.TypeDefinition();
    private static readonly CompletionKind _union = new CompletionKind.Union();
    private static readonly CompletionKind _delegate = new CompletionKind.Delegate();
    private static readonly CompletionKind _tagHelper = new CompletionKind.TagHelper();
    private static readonly CompletionKind _extensionMethod = new CompletionKind.ExtensionMethod();
    private static readonly CompletionKind _element = new CompletionKind.Element();
    private static readonly CompletionKind _localResource = new CompletionKind.LocalResource();
    private static readonly CompletionKind _systemResource = new CompletionKind.SystemResource();
    private static readonly CompletionKind _closeElement = new CompletionKind.CloseElement();

    extension(CompletionKind)
    {
        public static CompletionKind Text() => _text;
        public static CompletionKind Method() => _method;
        public static CompletionKind Function() => _function;
        public static CompletionKind Constructor() => _constructor;
        public static CompletionKind Field() => _field;
        public static CompletionKind Variable() => _variable;
        public static CompletionKind Class() => _class;
        public static CompletionKind Interface() => _interface;
        public static CompletionKind Module() => _module;
        public static CompletionKind Property() => _property;
        public static CompletionKind Unit() => _unit;
        public static CompletionKind Value() => _value;
        public static CompletionKind Enum() => _enum;
        public static CompletionKind Keyword() => _keyword;
        public static CompletionKind Snippet() => _snippet;
        public static CompletionKind Color() => _color;
        public static CompletionKind File() => _file;
        public static CompletionKind Reference() => _reference;
        public static CompletionKind Folder() => _folder;
        public static CompletionKind EnumMember() => _enumMember;
        public static CompletionKind Constant() => _constant;
        public static CompletionKind Struct() => _struct;
        public static CompletionKind Event() => _event;
        public static CompletionKind Operator() => _operator;
        public static CompletionKind TypeParameter() => _typeParameter;
        public static CompletionKind Macro() => _macro;
        public static CompletionKind Namespace() => _namespace;
        public static CompletionKind Template() => _template;
        public static CompletionKind TypeDefinition() => _typeDefinition;
        public static CompletionKind Union() => _union;
        public static CompletionKind Delegate() => _delegate;
        public static CompletionKind TagHelper() => _tagHelper;
        public static CompletionKind ExtensionMethod() => _extensionMethod;
        public static CompletionKind Element() => _element;
        public static CompletionKind LocalResource() => _localResource;
        public static CompletionKind SystemResource() => _systemResource;
        public static CompletionKind CloseElement() => _closeElement;
    }
}