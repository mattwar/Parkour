namespace Parkour;

/// <summary>
/// A closed-hierarchy of runtime member modifiers understood by IL metadata.
/// </summary>
public class RuntimeModifier : Modifier
{
    /// <summary>
    /// Private constructor to close hierarchy.
    /// </summary>
    private RuntimeModifier()
    {
    }

    public sealed class Abstract : RuntimeModifier { private Abstract() { } public static readonly Abstract Instance = new(); }
    public sealed class Constant : RuntimeModifier { private Constant() { } public static readonly Constant Instance = new(); }

    public sealed class HideBySig : RuntimeModifier { private HideBySig() { } public static readonly HideBySig Instance = new(); }

    public sealed class Override : RuntimeModifier { private Override() { } public static readonly Override Instance = new(); }

    public sealed class Sealed : RuntimeModifier { private Sealed() { } public static readonly Sealed Instance = new(); }

    public sealed class Static : RuntimeModifier { private Static() { } public static readonly Static Instance = new(); }

    public sealed class Virtual : RuntimeModifier { private Virtual() { } public static readonly Virtual Instance = new(); }    

    public sealed class ReadOnly : RuntimeModifier { private ReadOnly() { } public static readonly ReadOnly Instance = new(); }

    public sealed class Special : RuntimeModifier { private Special() { } public static readonly Special Instance = new(); }

    public sealed class In : RuntimeModifier { private In() { } public static readonly In Instance = new(); }

    public sealed class Out : RuntimeModifier { private Out() { } public static readonly Out Instance = new(); }

    public sealed class Ref : RuntimeModifier { private Ref() { } public static readonly Ref Instance = new(); }
}

/// <summary>
/// An extension that provides easy access to runtime modifiers as static properties on the <see cref="Modifier"/> class.
/// </summary>
public static class RuntimeModifierExtensions
{
    extension(Modifier)
    {
        public static RuntimeModifier.Abstract Abstract => RuntimeModifier.Abstract.Instance;
        public static RuntimeModifier.Constant Constant => RuntimeModifier.Constant.Instance;
        public static RuntimeModifier.HideBySig HideBySig => RuntimeModifier.HideBySig.Instance;
        public static RuntimeModifier.Override Override => RuntimeModifier.Override.Instance;
        public static RuntimeModifier.Sealed Sealed => RuntimeModifier.Sealed.Instance;
        public static RuntimeModifier.Static Static => RuntimeModifier.Static.Instance;
        public static RuntimeModifier.Virtual Virtual => RuntimeModifier.Virtual.Instance;
        public static RuntimeModifier.ReadOnly ReadOnly => RuntimeModifier.ReadOnly.Instance;
        public static RuntimeModifier.Special Special => RuntimeModifier.Special.Instance;
        public static RuntimeModifier.In In => RuntimeModifier.In.Instance;
        public static RuntimeModifier.Out Out => RuntimeModifier.Out.Instance;
        public static RuntimeModifier.Ref Ref => RuntimeModifier.Ref.Instance;
    }
}