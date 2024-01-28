namespace Parkour.Semantics;
using Symbols;

public class NewExpression : Expression
{
    public Expression? TypeExpression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public ConstructorSymbol? ConstructorSymbol { get; }

    public NewExpression(
        Expression? typeExpression,
        ImmutableList<Expression>? arguments,
        ISourceLocation? location,
        ConstructorSymbol? constructorSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(typeExpression)
            | CombineState(arguments)
            | NotNullOrDiagnosticState(constructorSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.TypeExpression = typeExpression;
        this.Arguments = arguments ?? ImmutableList<Expression>.Empty;
        this.ConstructorSymbol = constructorSymbol;
    }

    public override int ChildCount => 
        1 + Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.TypeExpression,
            _ => this.Arguments[index - 1]
        };
}

public class NewArraySizeExpression : Expression
{
    public Expression? ElementType { get; }
    public Expression Size { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArraySizeExpression(
        Expression? elementType,
        Expression size,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | State(size)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Size = size;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => ElementType,
            1 => Size,
            _ => null
        };
}

public class NewArrayInitExpression : Expression
{
    public Expression? ElementType { get; }
    public ImmutableList<Expression> Expressions { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArrayInitExpression(
        Expression? elementType,
        ImmutableList<Expression> expressions,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | CombineState(expressions)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Expressions = expressions;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override int ChildCount => 
        1 + Expressions.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            _ => this.Expressions[index - 1]
        };
}
