namespace Parkour.Services;

public abstract class ClassificationModifier
{
    private string? _cachedToString;

    public override string ToString() => _cachedToString ??= this.GetType().Name.ToLower();

    public sealed class Declaration : ClassificationModifier;
    public sealed class Definition : ClassificationModifier;
    public sealed class ReadOnly : ClassificationModifier;
    public sealed class Static : ClassificationModifier;

    public sealed class Deprecated : ClassificationModifier;

    public sealed class Abstract : ClassificationModifier;

    public sealed class Async : ClassificationModifier;

    public sealed class Modification : ClassificationModifier;

    public sealed class Documentation : ClassificationModifier;
    public sealed class DefaultLibrary : ClassificationModifier;
}

public static class ClassificationModifierExtensions
{
    private static readonly ClassificationModifier _declaration = new ClassificationModifier.Declaration();
    private static readonly ClassificationModifier _definition = new ClassificationModifier.Definition();
    private static readonly ClassificationModifier _readOnly = new ClassificationModifier.ReadOnly();
    private static readonly ClassificationModifier _static = new ClassificationModifier.Static();
    private static readonly ClassificationModifier _deprecated = new ClassificationModifier.Deprecated();
    private static readonly ClassificationModifier _abstract = new ClassificationModifier.Abstract();
    private static readonly ClassificationModifier _async = new ClassificationModifier.Async();
    private static readonly ClassificationModifier _modification = new ClassificationModifier.Modification();
    private static readonly ClassificationModifier _documentation = new ClassificationModifier.Documentation();
    private static readonly ClassificationModifier _defaultLibrary = new ClassificationModifier.DefaultLibrary();
    extension(ClassificationModifier)
    {
        public static ClassificationModifier Declaration() => _declaration;
        public static ClassificationModifier Definition() => _definition;
        public static ClassificationModifier ReadOnly() => _readOnly;
        public static ClassificationModifier Static() => _static;
        public static ClassificationModifier Deprecated() => _deprecated;
        public static ClassificationModifier Abstract() => _abstract;
        public static ClassificationModifier Async() => _async;
        public static ClassificationModifier Modification() => _modification;
        public static ClassificationModifier Documentation() => _documentation;
        public static ClassificationModifier DefaultLibrary() => _defaultLibrary;
    }
}