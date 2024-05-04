using Parkour.Lowering;
using Parkour.Symbols;

namespace Parkour.Emitting;

/// <summary>
/// Emits lowered symbols as a module.
/// </summary>
public abstract class StandardModuleEmitter : ModuleEmitter
{
    protected StandardModuleEmitter()
    {
    }

    /// <summary>
    /// Emits lowered symbols as a module.
    /// </summary>
    public EmitResult EmitModule(DeclarationLowering lowering) =>
        EmitModule(lowering, CreateBodyEmitter);

    /// <summary>
    /// Emits lowered symbols as a module.
    /// </summary>
    public EmitResult EmitModule(
        DeclarationLowering lowering,
        Func<MemberSymbol, SymbolTable, ILEmitter, BodyEmitter> fnCreateBodyEmitter)
    {
        return EmitModule(
            lowering.DeclaredSymbols,
            (m, e) => EmitMemberBody(lowering, m, e, fnCreateBodyEmitter));
    }

    /// <summary>
    /// Creates a new default <see cref="BodyEmitter"/>
    /// </summary>
    protected virtual BodyEmitter CreateBodyEmitter(
        MemberSymbol member, SymbolTable cache, ILEmitter ilEmitter)
    {
        return new StandardBodyEmitter(member, cache, ilEmitter, false);
    }

    /// <summary>
    /// Emits the body of a <see cref="MemberSymbol"/>
    /// </summary>
    protected virtual void EmitMemberBody(
        DeclarationLowering lowering, 
        MemberSymbol memberSymbol,
        ILEmitter ilEmitter,
        Func<MemberSymbol, SymbolTable, ILEmitter, BodyEmitter> fnCreateBodyEmitter)
    {
        var symbols = lowering.Binding.ExternalSymbols;
        var bodyEmitter = new StandardBodyEmitter(memberSymbol, symbols, ilEmitter, false);
        switch (memberSymbol)
        {
            case MethodSymbol methodSymbol:
                var methodDecl = lowering.GetMethodDeclaration(methodSymbol);
                if (methodDecl != null)
                {
                    bodyEmitter.EmitBody(methodDecl.Body, methodSymbol.ReturnType, methodDecl.ReturnLabel);
                }
                break;
            case ConstructorSymbol constructorSymbol:
                var constructorDecl = lowering.GetConstructorDeclaration(constructorSymbol);
                if (constructorDecl != null)
                {
                    bodyEmitter.EmitBody(constructorDecl.Body, SpecialSymbols.Void, constructorDecl.ReturnLabel);
                }
                break;
        }
    }

    /// <summary>
    /// Emits all declared types and members as a module.
    /// </summary>
    public override EmitResult EmitModule(
        GlobalNamespaceSymbol declaredSymbols,
        Action<MemberSymbol, ILEmitter> fnBuildBody)
    {
        DeclareTypeSymbols(declaredSymbols);
        DeclareBaseTypes(declaredSymbols);
        DeclareTypeMembers(declaredSymbols);
        BuildMemberBodies(declaredSymbols);
        return this.EmitModule();

        void DeclareTypeSymbols(Symbol symbol)
        {
            if (symbol is NamespaceSymbol ns)
            {
                foreach (var member in ns.Members)
                {
                    DeclareTypeSymbols(member);
                }
            }
            else if (symbol is TypeSymbol typeSymbol)
            {
                switch (typeSymbol)
                {
                    case ClassSymbol:
                    case StructSymbol:
                    case InterfaceSymbol:
                        this.DeclareType(typeSymbol);
                        break;
                    default:
                        this.ReportDiagnostic(new Diagnostic($"Cannot declare base type for '{symbol.FullName}'"));
                        return;
                }

                foreach (var member in typeSymbol.Members.OfType<TypeSymbol>())
                {
                    DeclareTypeSymbols(member);
                }
            }
        }

        void DeclareBaseTypes(Symbol symbol)
        {
            if (symbol is NamespaceSymbol ns)
            {
                foreach (var member in ns.Members)
                {
                    DeclareBaseTypes(member);
                }
            }
            else if (symbol is TypeSymbol typeSymbol)
            {
                switch (typeSymbol)
                {
                    case ClassSymbol:
                    case StructSymbol:
                    case InterfaceSymbol:
                        this.DeclareBaseTypes(typeSymbol);
                        break;
                    default:
                        this.ReportDiagnostic(new Diagnostic($"Cannot declare base type for '{symbol.FullName}'"));
                        return;
                }

                foreach (var member in typeSymbol.Members.OfType<TypeSymbol>())
                {
                    DeclareBaseTypes(member);
                }
            }
        }

        void DeclareTypeMembers(Symbol symbol)
        {
            switch (symbol)
            {
                case NamespaceSymbol namespaceSymbol:
                    foreach (var nsMember in namespaceSymbol.Members)
                    {
                        DeclareTypeMembers(nsMember);
                    }
                    break;

                case TypeSymbol typeSymbol:
                    foreach (var typeMember in typeSymbol.Members)
                    {
                        DeclareTypeMembers(typeMember);
                    }
                    break;

                case MemberSymbol memberSymbol:
                    this.DeclareTypeMember(memberSymbol);
                    break;
            }
        }

        void BuildMemberBodies(Symbol symbol)
        {
            switch (symbol)
            {
                case NamespaceSymbol ns:
                    foreach (var member in ns.Members)
                    {
                        BuildMemberBodies(member);
                    }
                    break;

                case TypeSymbol ts:
                    foreach (var member in ts.Members)
                    {
                        BuildMemberBodies(member);
                    }
                    break;

                case MethodSymbol method:
                    this.EmitMemberBody(method, fnBuildBody);
                    break;

                case ConstructorSymbol constructor:
                    this.EmitMemberBody(constructor, fnBuildBody);
                    break;
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
    protected abstract void EmitMemberBody(MemberSymbol memberSymbol, Action<MemberSymbol, ILEmitter> fnBuildbody);

    /// <summary>
    /// Finishes any remaining steps to produce the module.
    /// </summary>
    protected abstract EmitResult EmitModule();

    /// <summary>
    /// Reports diagnostics into the <see cref="ModuleEmitter.EmitResult"/>
    /// </summary>
    protected abstract void ReportDiagnostic(Diagnostic diagnostic);
}