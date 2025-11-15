namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Emits low-level elements into a final representation.
/// </summary>
public abstract class SemanticEmitter
{
    /// <summary>
    /// Emits all low-level elements into final representation.
    /// </summary>
    public abstract EmitResult Emit(
        SemanticLowering lowering);


    protected virtual void Declare(ImmutableList<Declaration> declarations)
    {
        VisitTypeDeclarations(declarations, DeclareType);
        VisitTypeDeclarations(declarations, DeclareBaseTypesAndInterfaces);
        VisitMemberDeclarations(declarations, DeclareMember, includeTypes: false);
        VisitMemberDeclarations(declarations, DeclareAccessors, includeTypes: false);
        VisitMemberDeclarations(declarations, DeclareAttributes, includeTypes: true);
        VisitMemberDeclarations(declarations, EmitMemberBody);
    }

    protected virtual void DeclareType(TypeDeclaration declaration)
    {
    }

    protected virtual void DeclareBaseTypesAndInterfaces(TypeDeclaration declaration)
    {
    }

    protected virtual void DeclareMember(MemberDeclaration declaration)
    {
    }

    protected virtual void DeclareAccessors(MemberDeclaration declaration)
    {
    }

    protected virtual void DeclareAttributes(MemberDeclaration declaration)
    {
    }

    protected virtual void EmitMemberBody(MemberDeclaration declaration)
    {
    }

    protected virtual void VisitTypeDeclarations(ImmutableList<Declaration> declarations, Action<TypeDeclaration> action)
    {
        VisitAll(declarations);

        void VisitAll<TDecl>(IEnumerable<TDecl> declarations)
            where TDecl : Declaration
        {
            foreach (var decl in declarations)
            {
                Visit(decl);
            }
        }

        void Visit(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                VisitAll(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                action(td);
                VisitAll(td.Declarations.OfType<TypeDeclaration>());
            }
        }
    }

    protected virtual void VisitMemberDeclarations(ImmutableList<Declaration> declarations, Action<MemberDeclaration> action, bool includeTypes = true)
    {
        VisitAll(declarations);

        void VisitAll<TDecl>(IEnumerable<TDecl> declarations)
            where TDecl : Declaration
        {
            foreach (var decl in declarations)
            {
                Visit(decl);
            }
        }

        void Visit(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                VisitAll(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                if (includeTypes)
                    action(td);
                VisitAll(td.Declarations);
            }
            else if (decl is MemberDeclaration md)
            {
                action(md);
            }
        }
    }

    public class EmitResult
    {
        /// <summary>
        /// Any diagnostics produced during emission.
        /// </summary>
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}