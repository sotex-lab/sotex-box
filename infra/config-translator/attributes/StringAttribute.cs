using Pulumi;

namespace ConfigTranslator.Attributes;

public class StringAttribute : GenericAttribute<string>
{
    public StringAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isSecret) { }

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecret(Name),
            false => config.Require(Name)
        };
}
