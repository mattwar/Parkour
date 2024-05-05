namespace Parkour.Emitting;

using Semantics;
using Symbols;

/// <summary>
/// Emits declared symbols as a module.
/// </summary>
public abstract class StandardDeclarationEmitter : DeclarationEmitter
{
    protected StandardDeclarationEmitter()
    {
    }

    /// <summary>
    /// Emits all declared types and members as a module.
    /// </summary>
    public override EmitResult Emit(
        ImmutableList<Declaration> declarations)
    {
        //var map = Declaration.BuildSymbolToDeclarationMap(declarations);

        DeclareTypes(declarations);
        DeclareBaseTypes(declarations);
        DeclareTypeMembers(declarations);
        BuildMemberBodies(declarations);

        return this.FinishEmit();

        void DeclareTypes(ImmutableList<Declaration> declarations)
        {
            foreach (var decl in declarations)
            {
                Declare(decl);
            }

            void Declare(Declaration decl)
            {
                if (decl is NamespaceDeclaration nd)
                {
                    DeclareTypes(nd.Declarations);
                }
                else if (decl is ClassDeclaration cd
                    && decl.DeclaredSymbol is TypeSymbol ts)
                {
                    this.DeclareType(ts);
                    DeclareTypes(cd.Declarations);
                }
            }
        }

        void DeclareBaseTypes(ImmutableList<Declaration> declarations)
        {
            foreach (var d in declarations)
            {
                Declare(d);
            }

            void Declare(Declaration decl)
            {
                if (decl is NamespaceDeclaration nd)
                {
                    DeclareBaseTypes(nd.Declarations);
                }
                else if (decl is ClassDeclaration cd
                    && decl.DeclaredSymbol is TypeSymbol ts)
                {
                    this.DeclareBaseTypes(ts);
                    DeclareBaseTypes(cd.Declarations);
                }
            }
        }

        void DeclareTypeMembers(ImmutableList<Declaration> declarations)
        {
            foreach (var decl in declarations)
            {
                Declare(decl);
            }

            void Declare(Declaration decl)
            {
                if (decl is NamespaceDeclaration nd)
                {
                    DeclareTypeMembers(nd.Declarations);
                }
                else if (decl is ClassDeclaration cd)
                {
                    DeclareTypeMembers(cd.Declarations);
                }
                else if (decl is PropertyDeclaration pd
                    && pd.PropertySymbol is PropertySymbol ps)
                {
                    if (pd.BackingField != null)
                        Declare(pd.BackingField);

                    if (pd.GetMethod != null)
                        Declare(pd.GetMethod);

                    if (pd.SetMethod != null)
                        Declare(pd.SetMethod);

                    this.DeclareTypeMember(ps);
                }
                else if (decl is IndexerDeclaration xd
                    && xd.IndexerSymbol is IndexerSymbol xs)
                {
                    if (xd.GetMethod != null)
                        Declare(xd.GetMethod);

                    if (xd.SetMethod != null)
                        Declare(xd.SetMethod);

                    this.DeclareTypeMember(xs);
                }
                else if ((decl is FieldDeclaration
                    || decl is MethodDeclaration
                    || decl is ConstructorDeclaration
                    || decl is IndexerDeclaration)
                    && decl.DeclaredSymbol is MemberSymbol memberSymbol)
                {
                    this.DeclareTypeMember(memberSymbol);
                }
            }
        }

        void BuildMemberBodies(ImmutableList<Declaration> declarations)
        {
            foreach (var decl in declarations)
            {
                Build(decl);
            }

            void Build(Declaration decl)
            {
                if (decl is NamespaceDeclaration nd)
                {
                    BuildMemberBodies(nd.Declarations);
                }
                else if (decl is ClassDeclaration cd)
                {
                    BuildMemberBodies(cd.Declarations);
                }
                else if (decl is MethodDeclaration md
                    && md.DeclaredSymbol is MethodSymbol ms)
                {
                    this.EmitMemberBody(ms, md);
                }
                else if (decl is ConstructorDeclaration cod
                    && cod.DeclaredSymbol is ConstructorSymbol cs)
                {
                    this.EmitMemberBody(cs, cod);
                }
                else if (decl is PropertyDeclaration pd
                    && pd.PropertySymbol is PropertySymbol ps)
                {
                    if (pd.GetMethod != null)
                        Build(pd.GetMethod);
                    if (pd.SetMethod != null)
                        Build(pd.SetMethod);
                }
                else if (decl is IndexerDeclaration xd
                    && xd.IndexerSymbol is IndexerSymbol xs)
                {
                    if (xd.GetMethod != null)
                        Build(xd.GetMethod);
                    if (xd.SetMethod != null)
                        Build(xd.SetMethod);
                }
            }
        }
    }

    /// <summary>
    /// Declares the type within the module.
    /// </summary>
    protected abstract void DeclareType(TypeSymbol typeSymbol);

    /// <summary>
    /// Declares the base type and interfaces of types already declared.
    /// </summary>
    protected abstract void DeclareBaseTypes(TypeSymbol typeSymbol);

    /// <summary>
    /// Declares the members of types already declared.
    /// </summary>
    protected abstract void DeclareTypeMember(MemberSymbol memberSymbol);

    /// <summary>
    /// Emits the IL for a body for a member that is already declared.
    /// </summary>
    protected abstract void EmitMemberBody(MemberSymbol memberSymbol, Declaration declaration);

    /// <summary>
    /// Finishes any remaining steps to produce the module.
    /// </summary>
    protected abstract EmitResult FinishEmit();

    /// <summary>
    /// Reports diagnostics into the <see cref="DeclarationEmitter.EmitResult"/>
    /// </summary>
    protected abstract void ReportDiagnostic(Diagnostic diagnostic);
}