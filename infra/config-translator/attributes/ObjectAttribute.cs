using Pulumi;

namespace ConfigTranslator.Attributes;

public class ObjectAttribute : GenericAttribute<object>
{
    public ObjectAttribute(string name, bool isList = false, bool isSecret = false)
        : base(name, isList, isSecret) { }

    protected override object? GetSpecificObject(Config config) =>
        IsSecret switch
        {
            true => config.RequireSecretObject<object>(Name),
            false => config.RequireSecret(Name)
        };
}
