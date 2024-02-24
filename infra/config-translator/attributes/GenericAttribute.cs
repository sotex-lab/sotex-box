using Pulumi;

namespace ConfigTranslator.Attributes;

public abstract class GenericAttribute<T> : GenericAttributeLower
{
    protected GenericAttribute(string name, bool isList, bool isSecret)
        : base(name, isList, isSecret) { }

    public override object? GetConfigValue(Config config) =>
        (IsList, IsSecret) switch
        {
            (true, true) => config.RequireSecretObject<IEnumerable<T>>(Name),
            (true, false) => config.RequireObject<IEnumerable<T>>(Name),
            (false, _) => GetSpecificObject(config),
        };

    protected abstract object? GetSpecificObject(Config config);
}

public abstract class GenericAttributeLower : Attribute
{
    public string Name { get; set; }
    public bool IsList { get; set; }
    public bool IsSecret { get; set; }

    protected GenericAttributeLower(string name, bool isList, bool isSecret)
    {
        Name = name;
        IsList = isList;
        IsSecret = isSecret;
    }

    public abstract object? GetConfigValue(Config config);
}
