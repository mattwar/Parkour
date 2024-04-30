namespace Parkour.Semantics;
using Symbols;

public sealed class ClassDeclaration : MemberDeclaration
{
    public ImmutableList<TypeParameterDeclaration> TypeParameters { get; }
    public ImmutableList<Expression> BaseTypes { get; }
    public ImmutableList<Declaration> Declarations { get; }
    public TypeSymbol? ClassSymbol { get; }

    public ClassDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        TypeSymbol? classSymbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          CombineState(typeParameters)
          | CombineState(declarations)
          | NotNullState(classSymbol),
          name,
          access,
          modifiers,
          location,
          diagnostics)
    {
        this.TypeParameters = typeParameters;
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
        this.ClassSymbol = classSymbol;
    }

    public override int ChildCount =>
        this.TypeParameters.Count
        + this.BaseTypes.Count 
        + this.Declarations.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index < this.TypeParameters.Count)
            return this.TypeParameters[index];
        index -= this.TypeParameters.Count;
        if (index < this.BaseTypes.Count)
            return this.BaseTypes[index];
        index -= this.BaseTypes.Count;
        if (index < this.Declarations.Count)
            return this.Declarations[index];
        return null;
    }
}

