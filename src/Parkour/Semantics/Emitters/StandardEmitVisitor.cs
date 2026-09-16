namespace Parkour.Semantics;

/// <summary>
/// An encapsulation of common logic to visit a set of declarations 
/// in multiple passes to allow for construction of a target representation that is typically cyclic.
/// </summary>
public static class StandardEmitVisitor
{
    /// <summary>
    /// Visits the type and member declarations with specific callbacks to perform common operations for building a target representation.
    /// </summary>
    /// <param name="declarations">The set of declarations to visit</param>
    /// <param name="fnDeclareType">Used to declare a representation for a type, but not its members</param>
    /// <param name="fnDeclareBaseTypeAndInterfaces">Used to add references to base types and interfaces to a previously declared type.</param>
    /// <param name="fnDeclareMember">Used to declare a member of a type, but not to emit its body</param>
    /// <param name="fnDeclareAccessors">Used to declare special accessors for properties</param>
    /// <param name="fnDeclareAttributes">Used to declare custom attributes on types, members or parameters</param>
    /// <param name="fnEmitMemberBody">Used to emit member bodies (IL)</param>
    public static void Visit(
        ImmutableList<Declaration> declarations,
        Action<TypeDeclaration> fnDeclareType, 
        Action<TypeDeclaration> fnDeclareBaseTypeAndInterfaces,
        Action<MemberDeclaration> fnDeclareMember,
        Action<MemberDeclaration> fnDeclareAccessors,
        Action<MemberDeclaration> fnDeclareAttributes,
        Action<MemberDeclaration> fnEmitMemberBody)
    {
        VisitTypeDeclarations(declarations, fnDeclareType);
        VisitTypeDeclarations(declarations, fnDeclareBaseTypeAndInterfaces);
        VisitMemberDeclarations(declarations, fnDeclareMember, includeTypes: false);
        VisitMemberDeclarations(declarations, fnDeclareAccessors, includeTypes: false);
        VisitMemberDeclarations(declarations, fnDeclareAttributes, includeTypes: true);
        VisitMemberDeclarations(declarations, fnEmitMemberBody);
    }

    /// <summary>
    /// Visits all type declarations in the list of declarations.
    /// Will visit nested type declarations inside namespace declarations.
    /// </summary>
    private static void VisitTypeDeclarations(ImmutableList<Declaration> declarations, Action<TypeDeclaration> action)
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

    /// <summary>
    /// Visits all member declarations.
    /// Override to change the order of visiting members.
    /// </summary>
    private static void VisitMemberDeclarations(ImmutableList<Declaration> declarations, Action<MemberDeclaration> action, bool includeTypes = true)
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
}
