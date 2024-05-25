namespace Parkour.Semantics;

using Symbols;

public class MethodDeclaration : MemberDeclaration
{
    public override MethodSymbol? Symbol { get; }

    public ImmutableList<TypeParameterDeclaration> TypeParameters { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression? ReturnType { get; }
    public LabelSymbol? ReturnLabel { get; }

    private MethodDeclaration(
        string name, 
        SymbolAccess access, 
        BitSet<SymbolModifier> modifiers, 
        ImmutableList<AttributeExpression> attributes,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<ParameterDeclaration> parameters,
        Expression? returnType,
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
            attributes,
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

    public MethodDeclaration(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression? returnType,
        Expression body,
        ISourceLocation? location)
        : this(
              name, 
              SymbolAccess.Public, 
              SymbolModifier.None,
              ImmutableList<AttributeExpression>.Empty,
              ImmutableList<TypeParameterDeclaration>.Empty,
              parameters, 
              returnType, 
              body, 
              location, 
              null, 
              null, 
              null)
    {
    }

    public override MethodDeclaration WithName(string name) =>
        name == this.Name ? this :
        new MethodDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        location == this.Location ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        symbol == this.Symbol ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
            this.Attributes,
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
        diagnostics == this.Diagnostics ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        access == this.Access ? this :
        new MethodDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.Attributes,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override MethodDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.Attributes,
            this.TypeParameters,
            this.Parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override MethodDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            attributes,
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
        typeParameters == this.TypeParameters ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        parameters == this.Parameters ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.TypeParameters,
            parameters,
            this.ReturnType,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public MethodDeclaration WithReturnType(Expression? returnType) =>
        returnType == this.ReturnType ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        body == this.Body ? this :
        new MethodDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
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
        base.ChildCount
        + this.TypeParameters.Count 
        + this.Parameters.Count 
        + 2;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
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

    public override MethodDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var typeParams = rewriter.Rewrite(this.TypeParameters);
        var parameters = rewriter.Rewrite(this.Parameters);
        var returnType = rewriter.Rewrite(this.ReturnType);
        var body = rewriter.Rewrite(this.Body);
        return this
            .WithAttributes(attributes)
            .WithTypeParameters(typeParams)
            .WithParameters(parameters)
            .WithReturnType(returnType!)
            .WithBody(body!);
    }
}
