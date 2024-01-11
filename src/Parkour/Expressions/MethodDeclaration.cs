namespace Parkour.Expressions;
using Symbols;
using Syntax;

public class MethodDeclaration : MemberDeclaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression ReturnType { get; }
    public MethodSymbol? Symbol { get; }

    public MethodDeclaration(
        string name, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        ImmutableList<ParameterDeclaration> parameters, 
        Expression body, 
        Expression returnType, 
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        MethodSymbol? symbol)
        : base(
            CombineState(parameters) | body.State, 
            name, 
            access, 
            modifiers, 
            diagnostics,
            syntax)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.ReturnType = returnType;
        this.Symbol = symbol;
    }
}
