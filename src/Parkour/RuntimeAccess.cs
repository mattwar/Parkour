namespace Parkour;

/// <summary>
/// The closed-hierarchy of runtime access restrictions.
/// </summary>
public abstract class RuntimeAccess : Access
{
    /// <summary>
    /// Private constructor to close hierarchy.
    /// </summary>
    private RuntimeAccess() { }

    public sealed class Public : RuntimeAccess { private Public() { } public static Public Instance { get; } = new(); }

    public sealed class Private : RuntimeAccess { private Private() { } public static Private Instance { get; } = new(); }

    public sealed class Protected : RuntimeAccess { private Protected() { } public static Protected Instance { get; } = new(); }

    public sealed class ProtectedAndInternal : RuntimeAccess { private ProtectedAndInternal() { } public static ProtectedAndInternal Instance { get; } = new(); }

    public sealed class ProtectedOrInternal : RuntimeAccess { private ProtectedOrInternal() { } public static ProtectedOrInternal Instance { get; } = new(); }

    public sealed class Internal : RuntimeAccess { private Internal() { } public static Internal Instance { get; } = new(); }
}

/// <summary>
/// An extension that provides easy access to runtime access restrictions as static properties on the <see cref="Access"/> class.
/// </summary>
public static class RuntimeAccessExtensions
{
    extension(Access)
    {
        public static RuntimeAccess Public => RuntimeAccess.Public.Instance;
        public static RuntimeAccess Private => RuntimeAccess.Private.Instance;
        public static RuntimeAccess Protected => RuntimeAccess.Protected.Instance;
        public static RuntimeAccess ProtectedAndInternal => RuntimeAccess.ProtectedAndInternal.Instance;
        public static RuntimeAccess ProtectedOrInternal => RuntimeAccess.ProtectedOrInternal.Instance;    
        public static RuntimeAccess Internal => RuntimeAccess.Internal.Instance;
    }
}