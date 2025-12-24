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
        var args = this.Arguments.SelectSame(a => a.Substitute(context));
        var members = this.Members.SelectSame(m => m.Substitute(context));

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

public abstract record AttributeValue()
{
    public abstract AttributeValue Substitute(SubstitutionContext context);
}

public record AttributeConstantValue(object? Value) : AttributeValue()
{
    public override AttributeValue Substitute(SubstitutionContext context) =>
        this;
}

public record AttributeTypeValue(TypeSymbol Type) : AttributeValue()
{
    public override AttributeValue Substitute(SubstitutionContext context)
    {
        var subbed = context.Substitute(this.Type);
        return (subbed == this.Type)
            ? this
            : new AttributeTypeValue(subbed);
    }
}

public record AttributeArrayValue(TypeSymbol ElementType, ImmutableList<AttributeValue> Values) : AttributeValue
{
    public override AttributeValue Substitute(SubstitutionContext context)
    {
        var elemType = context.Substitute(this.ElementType);
        var values = this.Values.SelectSame(v => v.Substitute(context));
        return (elemType == this.ElementType && values == this.Values)
            ? this
            : new AttributeArrayValue(elemType, values);
    }
}

public record AttributeArgument(ParameterSymbol Parameter, AttributeValue Value)
{
    public AttributeArgument Substitute(SubstitutionContext context)
    {
        var parameter = context.Substitute(this.Parameter);
        var value = this.Value.Substitute(context);
        return parameter == this.Parameter && value == this.Value
            ? this
            : new AttributeArgument(parameter, value);
    }
}

public record AttributeMember(MemberSymbol Member, AttributeValue Value)
{
    public AttributeMember Substitute(SubstitutionContext context)
    {
        var member = context.Substitute(this.Member);
        var value = this.Value.Substitute(context);
        return member == this.Member && value == this.Value
            ? this
            : new AttributeMember(member, value);
    }
}