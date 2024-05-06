namespace Parkour.Semantics;

using Symbols;

public class MethodDeclaration : MemberDeclaration
{
    public override MethodSymbol? Symbol { get; }

    public ImmutableList<TypeParameterDeclaration> TypeParameters { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression ReturnType { get; }
    public LabelSymbol? ReturnLabel { get; }

    public MethodDeclaration(
        string name, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<ParameterDeclaration> parameters,
        Expression returnType,
        Expression body, 
        ISourceLocation? location,
        MethodSymbol? symbol,
        LabelSymbol? returnLabel,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(typeParameters)
            | CombineState(parameters) 
            | State(body)
            | State(returnType)
            | NotNullState(symbol),
            name, 
            access, 
            modifiers, 
            location,
            diagnostics)
    {
        this.TypeParameters = typeParameters;
        this.Parameters = parameters;
        this.Body = body;
        this.ReturnType = returnType;
        this.Symbol = symbol;
        this.ReturnLabel = returnLabel;
    }

    public override MethodDeclaration WithName(string name) =>
        new MethodDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override MethodDeclaration WithLocation(ISourceLocation? location) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithSymbol(MethodSymbol? symbol) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithReturnLabel(LabelSymbol? returnLabel) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            returnLabel,
            this.Diagnostics
            );

    public override MethodDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            diagnostics
            );

    public override MethodDeclaration WithAccess(SymbolAccess access) =>
        new MethodDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override MethodDeclaration WithModifiers(SymbolModifier modifiers) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            typeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithParameters(ImmutableList<ParameterDeclaration> parameters) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithReturnType(Expression returnType) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            returnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithBody(Expression body) =>
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            body,
            this.Location,
            symbol: null,
            returnLabel: null,
            diagnostics: null
            );

    public override int ChildCount =>
        this.TypeParameters.Count 
        + this.Parameters.Count 
        + 2;

    public override SemanticElement? GetChild(int index)
    {
        if (index < this.TypeParameters.Count)
            return this.TypeParameters[index];
        index -= this.TypeParameters.Count;
        if (index < this.Parameters.Count)
            return this.Parameters[index];
        index -= this.Parameters.Count;
        return index switch
        {
            0 => this.Body,
            1 => this.ReturnType,
            _ => null
        };
    }
}
