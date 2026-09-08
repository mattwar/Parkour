namespace Parkour.Services;

public record struct Classification(ClassificationKind Kind, ImmutableList<ClassificationModifier> Modifiers)
{
    public Classification(ClassificationKind kind)
        : this(kind, ImmutableList<ClassificationModifier>.Empty)
    {
    }

    public static implicit operator Classification(ClassificationKind kind) =>
        new Classification(kind);
}
