using Pulumi;

namespace ConfigTranslator.Attributes;

public class DoubleAttribute : GenericAttribute<double>
{
    public DoubleAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isSecret) { }

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecretDouble(Name),
            false => config.RequireDouble(Name)
        };
}
