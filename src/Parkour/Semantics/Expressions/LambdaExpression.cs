namespace Parkour.Semantics;
using Symbols;

public sealed class LambdaExpression : Expression
{
    public string Name { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public FunctionSymbol? FunctionSymbol { get; }
    public TypeSymbol? ReturnType { get; }
    public LabelSymbol? ReturnLabel { get; }

    public LambdaExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        TypeSymbol? returnType,
        FunctionSymbol? functionSymbol,
        LabelSymbol? returnLabel,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters)
            | State(body)
            | NotNullState(returnType)
            | NotNullState(functionSymbol)
            | NotNullState(returnLabel),
            location,
            functionSymbol,
            diagnostics)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.FunctionSymbol = functionSymbol;
        this.ReturnType = returnType;
        this.ReturnLabel = returnLabel;
    }

    public override int ChildCount =>
        this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;
}

