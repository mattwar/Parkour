namespace Parkour.Semantics;
using Symbols;
using Syntax;

public class MethodDeclaration : MemberDeclaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression ReturnType { get; }
    public MethodSymbol? MethodSymbol { get; }

    public MethodDeclaration(
        string name, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        ImmutableList<ParameterDeclaration> parameters, 
        Expression body, 
        Expression returnType,
        ISourceLocation? location,
        MethodSymbol? methodSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters) 
            | body.State
            | returnType.State
            | NotNullState(methodSymbol),
            name, 
            access, 
            modifiers, 
            location,
            diagnostics)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.ReturnType = returnType;
        this.MethodSymbol = methodSymbol;
    }

    public override int ChildCount =>
        this.Parameters.Count + 2;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : (index - this.Parameters.Count) switch
                {
                    0 => this.Body,
                    1 => this.ReturnType,
                    _ => null
                };
}
