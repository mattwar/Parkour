namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Defines an inline function.
/// </summary>
public sealed class LambdaExpression : Expression
{
    public string Name { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression? ReturnType { get; }
    public DelegateSymbol? FunctionSymbol { get; }
    public LabelSymbol? ReturnLabel { get; }

    private LambdaExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression? returnType,
        Expression body,
        ISourceLocation? location,
        DelegateSymbol? functionSymbol,
        LabelSymbol? returnLabel,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters)
            | State(body)
            | State(returnType)
            | NotNullState(functionSymbol)
            | NotNullState(returnLabel),
            location,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.ReturnType = returnType;
        this.FunctionSymbol = functionSymbol;
        this.ReturnLabel = returnLabel;
    }

    public LambdaExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression? returnType,
        Expression body,
        ISourceLocation? location)
        : this(name, parameters, returnType, body, location, null, null, null, null)
    {
    }

    public override LambdaExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public override LambdaExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            diagnostics
            );

    public override LambdaExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            resultType,
            this.Diagnostics
            );

    public LambdaExpression WithName(string name) =>
        name == this.Name ? this :
        new LambdaExpression(
            name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public LambdaExpression WithParameters(ImmutableList<ParameterDeclaration> parameters) =>
        parameters == this.Parameters ? this :
        new LambdaExpression(
            this.Name,
            parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public LambdaExpression WithBody(Expression body) =>
        body == this.Body ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public LambdaExpression WithReturnType(Expression? returnType) =>
        returnType == this.ReturnType ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            returnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public LambdaExpression WithFunctionSymbol(DelegateSymbol? functionSymbol) =>
        functionSymbol == this.FunctionSymbol ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            functionSymbol,
            this.ReturnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public LambdaExpression WithReturnLabel(LabelSymbol? returnLabel) =>
        returnLabel == this.ReturnLabel ? this :
        new LambdaExpression(
            this.Name,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.FunctionSymbol,
            returnLabel,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;

    public override LambdaExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var parameters = rewriter.Rewrite(this.Parameters);
        var returnType = rewriter.Rewrite(this.ReturnType);
        var body = rewriter.Rewrite(this.Body);
        return this
            .WithParameters(parameters)
            .WithReturnType(returnType)
            .WithBody(body!);
    }
}