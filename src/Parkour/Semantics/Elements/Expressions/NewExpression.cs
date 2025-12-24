namespace Parkour.Semantics;

using Symbols;

public class NewExpression : Expression
{
    protected internal override string DebugText =>
        $"{nameof(NewExpression)}: {(ResultType != null ? ResultType.FullName : Type != null ? Type.DebugText : "<inferred>")}";

    public Expression? Type { get; }
    public ImmutableList<Expression> Arguments { get; }
    public ConstructorSymbol? ConstructorSymbol { get; }

    private NewExpression(
        Expression? type,
        ImmutableList<Expression>? arguments,
        ISourceLocation? location,
        ConstructorSymbol? constructorSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(type)
            | CombineState(arguments)
            | NotNullOrDiagnosticState(constructorSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
        this.Arguments = arguments ?? ImmutableList<Expression>.Empty;
        this.ConstructorSymbol = constructorSymbol;
    }

    public NewExpression(
        Expression? type,
        ImmutableList<Expression>? arguments,
        ISourceLocation? location)
        : this(type, arguments, location, null, null, null)
    {
    }

    public override NewExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NewExpression(
            this.Type,
            this.Arguments,
            location,
            this.ConstructorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NewExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NewExpression(
            this.Type,
            this.Arguments,
            this.Location,
            this.ConstructorSymbol,
            this.ResultType,
            diagnostics
            );

    public override NewExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NewExpression(
            this.Type,
            this.Arguments,
            this.Location,
            this.ConstructorSymbol,
            resultType,
            this.Diagnostics
            );

    public NewExpression WithType(Expression? type) =>
        type == this.Type ? this :
        new NewExpression(
            type,
            this.Arguments,
            this.Location,
            this.ConstructorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new NewExpression(
            this.Type,
            arguments,
            this.Location,
            this.ConstructorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewExpression WithConstructorSymbol(ConstructorSymbol? constructorSymbol) =>
        constructorSymbol == this.ConstructorSymbol ? this :
        new NewExpression(
            this.Type,
            this.Arguments,
            this.Location,
            constructorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        1 + Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Type,
            _ => this.Arguments[index - 1]
        };

    public override NewExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.Type);
        var arguments = rewriter.Rewrite(this.Arguments);
        return this
            .WithType(type!)
            .WithArguments(arguments);
    }
}
