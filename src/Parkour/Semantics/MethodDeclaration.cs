namespace Parkour.Semantics;
using Symbols;

public class MethodDeclaration : MemberDeclaration
{
    public ImmutableList<TypeParameterDeclaration> TypeParameters { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression ReturnType { get; }
    public MethodSymbol? MethodSymbol { get; }

    public MethodDeclaration(
        string name, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<ParameterDeclaration> parameters, 
        Expression body, 
        Expression returnType,
        ISourceLocation? location,
        MethodSymbol? methodSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(typeParameters)
            | CombineState(parameters) 
            | body.State
            | returnType.State
            | NotNullState(methodSymbol),
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
        this.MethodSymbol = methodSymbol;
    }

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
