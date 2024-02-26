using Pulumi;

namespace ConfigTranslator.Attributes;

public class BooleanAttribute : GenericAttribute<bool>
{
    public BooleanAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isList) { }

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecretBoolean(Name),
            false => config.RequireBoolean(Name)
        };
}
