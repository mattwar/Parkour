namespace Parkour.Expressions;
using Symbols;

public class MethodDeclaration : Declaration
{
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public Expression ReturnType { get; }

    public MethodDeclaration(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression body, Expression returnType, ImmutableList<Diagnostic>? diagnostics = null)
        : base(body.State, name, access, modifiers, diagnostics)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.ReturnType = returnType;
    }
}
