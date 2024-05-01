namespace Parkour.Emitting;
using Symbols;

/// <summary>
/// Builds a CLR Module from symbols and instructions.
/// </summary>
public abstract class ModuleBuilder
{
    // step 1: declare all types
    public abstract void DeclareClass(ClassSymbol classSymbol);
    public abstract void DeclareStruct(StructSymbol structSymbol);
    public abstract void DeclareInterface(InterfaceSymbol interfaceSymbol);

    // step 2: declare all base types and interfaces
    public abstract void DeclareClassBaseType(ClassSymbol classSymbol);
    public abstract void DeclareStructBaseType(StructSymbol structSymbol);
    public abstract void DeclareInterfaceBaseType(InterfaceSymbol interfaceSymbol);

    // step 3: declare all non-type members
    public abstract void DeclareField(FieldSymbol fieldSymbol);
    public abstract void DeclareMethod(MethodSymbol methodSymbol);
    public abstract void DeclareConstructor(ConstructorSymbol constructorSymbol);
    public abstract void DeclareProperty(PropertySymbol propertySymbol);
    public abstract void DeclareIndexer(IndexerSymbol indexerSymbol);

    // step 4: build all bodies
    public abstract void BuildMethodBody(MethodSymbol methodSymbol, Action<MethodSymbol, BodyBuilder> fnBuildBody);
    public abstract void BuildConstructorBody(ConstructorSymbol constructorSymbol, Action<ConstructorSymbol, BodyBuilder> fnBuildBody);

    /// <summary>
    /// Finishes building defined type and members.
    /// </summary>
    public abstract BuildResult Build();

    public class BuildResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public BuildResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}

