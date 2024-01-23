namespace Parkour.Semantics;
using Symbols;

public sealed class ClassDeclaration : MemberDeclaration
{
    public ImmutableList<Expression> BaseTypes { get; }
    public ImmutableList<Declaration> Declarations { get; }
    public TypeSymbol? ClassSymbol { get; }

    public ClassDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        TypeSymbol? classSymbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          CombineState(declarations)
          | NotNullState(classSymbol),
          name,
          access,
          modifiers,
          location,
          diagnostics)
    {
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
        this.ClassSymbol = classSymbol;
    }

    public override int ChildCount => 
        this.BaseTypes.Count + this.Declarations.Count;

    public override SemanticElement? GetChild(int index) =>
        index < this.BaseTypes.Count
            ? this.BaseTypes[index]
            : this.Declarations[index - this.BaseTypes.Count];
}

