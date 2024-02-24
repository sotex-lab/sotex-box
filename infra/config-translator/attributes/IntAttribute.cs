using Pulumi;

namespace ConfigTranslator.Attributes;

public class IntAttribute : GenericAttribute<int>
{
    public IntAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isSecret) { }

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecretInt32(Name),
            false => config.RequireInt32(Name)
        };
}
