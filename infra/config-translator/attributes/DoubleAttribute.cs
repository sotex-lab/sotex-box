using Pulumi;

namespace ConfigTranslator.Attributes;

public class DoubleAttribute : GenericAttribute<double>
{
    public DoubleAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isSecret) { }

    public override object? GetConfigValue(Config config) =>
        (IsList, IsSecret) switch
        {
            (true, true) => config.RequireSecretObject<IEnumerable<double>>(Name),
            (true, false) => config.RequireObject<IEnumerable<double>>(Name),
            (false, true) => config.RequireSecretDouble(Name),
            (false, false) => config.RequireDouble(Name),
        };

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecretDouble(Name),
            false => config.RequireDouble(Name)
        };
}
