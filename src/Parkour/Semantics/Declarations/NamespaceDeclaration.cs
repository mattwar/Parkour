namespace Parkour.Semantics;

using Parkour;
using Symbols;
using System.Xml.Linq;

public class NamespaceDeclaration : MemberDeclaration
{
    public override NamespaceSymbol? Symbol { get; }
    public ImmutableList<Declaration> Declarations { get; }

    private NamespaceDeclaration(
        string name,
        Access access,
        BitSet<Modifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
        ImmutableList<Declaration> declarations,
        ISourceLocation? location,
        NamespaceSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(declarations)
            | NotNullState(symbol), 
            name, 
            access, 
            modifiers, 
            attributes,
            location,
            diagnostics)
    {
        this.Declarations = declarations;
        this.Symbol = symbol;    
    }

    public NamespaceDeclaration(
        string name,
        ImmutableList<Declaration> declarations,
        ISourceLocation? location)
        : this(
              name,
              Access.Public,
              Modifier.None,
              ImmutableList<AttributeExpression>.Empty,
              declarations, 
              location, 
              null, 
              null)
    {
    }

    public override NamespaceDeclaration WithName(string name) =>
        name == this.Name ? this :
        new NamespaceDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.Declarations,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public NamespaceDeclaration WithSymbol(NamespaceSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.Declarations,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.Declarations,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override NamespaceDeclaration WithAccess(Access access) =>
        access == this.Access ? this :
        new NamespaceDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.Attributes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );


    public override NamespaceDeclaration WithModifiers(BitSet<Modifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.Attributes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            attributes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public NamespaceDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        declarations == this.Declarations ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public bool IsGlobalNamespace => 
        this.Name == "";

    public override int ChildCount =>
        base.ChildCount
        + this.Declarations.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        return index < this.Declarations.Count
            ? this.Declarations[index]
            : null;
    }

    public override NamespaceDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var declarations = rewriter.Rewrite(this.Declarations);
        return this
            .WithAttributes(attributes)
            .WithDeclarations(declarations);
    }
}
