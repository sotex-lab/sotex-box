using System.Reflection;
using ConfigTranslator.Attributes;
using Pulumi;

namespace config_translator;

public class PulumiMapper
{
    private readonly Config _config;

    public PulumiMapper(Config config)
    {
        _config = config;
    }

    public T Map<T>()
    {
        var instance = Activator.CreateInstance<T>();

        var properties = typeof(T)
            .GetProperties()
            .Where(x => x.GetCustomAttributes(typeof(GenericAttribute<>)).Any());

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<GenericAttributeLower>()!;

            var value = attribute.GetConfigValue(_config);

            property.SetValue(instance, value);
        }

        return instance;
    }
}
