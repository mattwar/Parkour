namespace Parkour.Symbols;

public record AttributeInfo(
    ConstructorSymbol Constructor,
    ImmutableList<AttributeArgument> Arguments,
    ImmutableList<AttributeMember> Members
    )
{
    public AttributeInfo Substitute(SubstitutionContext context)
    {
        var constructor = context.Substitute(this.Constructor);
        var args = this.Arguments.Map(a => a.Substitute(context));
        var members = this.Members.Map(m => m.Substitute(context));

        if (constructor == this.Constructor
            && args == this.Arguments
            && members == this.Members)
        {
            return this;
        }
        else
        {
            return new AttributeInfo(constructor, args, members);
        }
    }
}

public record AttributeArgument(ParameterSymbol Parameter, object? Value)
{
    public AttributeArgument Substitute(SubstitutionContext context)
    {
        var subbed = context.Substitute(this.Parameter);
        return subbed == this.Parameter ? this : new AttributeArgument(subbed, this.Value);
    }
}

public record AttributeMember(MemberSymbol Member, object? Value)
{
    public AttributeMember Substitute(SubstitutionContext context)
    {
        var subbed = context.Substitute(this.Member);
        return subbed == this.Member ? this : new AttributeMember(subbed, this.Value);
    }
}

