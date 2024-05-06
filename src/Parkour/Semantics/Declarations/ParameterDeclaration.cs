namespace Parkour.Semantics;
using Symbols;
using Syntax;
using System.Xml.Linq;

public sealed class ParameterDeclaration : Declaration
{
    public override ParameterSymbol? Symbol { get; }

    public Expression? ParameterType { get; }

    public ParameterDeclaration(
        string name, 
        Expression? parameterType,
        ISourceLocation? location,
        ParameterSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(parameterType)
            | NotNullState(symbol), 
            name, 
            location,
            diagnostics)
    {
        this.ParameterType = parameterType;
        this.Symbol = symbol;
    }

    public override ParameterDeclaration WithName(string name) =>
        new ParameterDeclaration(
            name,
            this.ParameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override ParameterDeclaration WithLocation(ISourceLocation? location) =>
        new ParameterDeclaration(
            this.Name,
            this.ParameterType,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public ParameterDeclaration WithSymbol(ParameterSymbol? symbol) =>
        new ParameterDeclaration(
            this.Name,
            this.ParameterType,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override ParameterDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new ParameterDeclaration(
            this.Name,
            this.ParameterType,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public ParameterDeclaration WithParameterType(Expression parameterType) =>
        new ParameterDeclaration(
            this.Name,
            parameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );


    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        this.ParameterType;
}