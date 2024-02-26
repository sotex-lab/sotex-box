This document provides a high-level overview of our infrastructure and [IaC](https://en.wikipedia.org/wiki/Infrastructure_as_code) practices. It will be continously updated as our infrastructure evolves.

To dive deeper in concrete architecture that is deployed you should go and check out our [software architecture document](/introduction/software-arch.html).

## Pulumi tool

In order to help automate creation and deployment of environments we take advantage of [pulumi](https://www.pulumi.com/). The biggest benefit that we see is the ability to use any programming language and easily manage cloud resources. Since most of our backend stack is dotnet related we've gone and implemented it using c#. All the code that we use to deploy our resources is in `infra/backend` project.

In `Pulumi.yaml` we have defined all the configuration values that our stack uses and we've gone and provided default values that we use for `staging` environment there. All of the values can be overriden in `Pulumi.<env>.yaml` file if necessary.

## How we map configuration

Pulumi provides us with a mechanism for fetching configuration from its `Pulumi(.<env>).yaml` files. However its not strictly typed. You are able to make mistakes easily and you will notice that only once you deploy a stack. On top of that if you write something like
```c#
var value = config.RequireString("networking-vpc-cidrBlock");
```
you will end up with a lot of constant strings that you have to manage as well. For now our project for IaC only contains one file with resources, named `Program.cs` but in the future if we want to support deploying to multiple clouds and reuse some parts of configuration it will be unmanagable. One more issue is that when typing out the name of the config value you want to get you can write anything and there is no lsp support.

To tackle observed problems we've implemented a `config-translator` which is a library that can map pulumi config into classes and after that you can use them with ease.

## Creating your configuration

If you want to add a custom value you should go to `Pulumi.yaml` and add the definition for your value:
```yaml
...
    sotex-box:my-custom-setting:
        description: A custom setting added for demo purposes
        default: "custom value"
        type: string
...
```
after that you can register it in `MappedConfig.cs` with adding a two lines in the class:
```c#
public class MappedConfig {
    ...
    [String("my-custom-setting")]
    public int MyCustomValue { get; set; }
}
```
Having done all this you will be able to use your value within the infrastructure deployment development, testing and deployment process.
