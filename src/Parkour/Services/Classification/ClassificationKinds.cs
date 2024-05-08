namespace Parkour.Services;

/// <summary>
/// This is a set of common classifications that a language service may produce.
/// </summary>
public static class ClassificationKinds
{
    public const string Text = nameof(Text);
    public const string Keyword = nameof(Keyword);
    public const string Punctuation = nameof(Punctuation);
    public const string Trivia = nameof(Trivia);
    public const string Comment = nameof(Comment);
    public const string Annotation = nameof(Annotation);
    public const string Literal = nameof(Literal);
    public const string NumericLiteral = nameof(NumericLiteral);
    public const string StringLiteral = nameof(StringLiteral);
    public const string DateTimeLiteral = nameof(DateTimeLiteral);
    public const string Intrinsic = nameof(Intrinsic);
    public const string Name = nameof(Name);
    public const string TypeName = nameof(TypeName);
    public const string TypeMemberName = nameof(TypeMemberName);
    public const string TypeParameterName = nameof(TypeParameterName);
    public const string InterfaceName = nameof(InterfaceName);
    public const string NamespaceName = nameof(NamespaceName);
    public const string MethodName = nameof(MethodName);
    public const string FieldName = nameof(FieldName);
    public const string PropertyName = nameof(PropertyName);
    public const string EventName = nameof(EventName);
}

