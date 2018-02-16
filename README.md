VULCAN
------

[![Build status](https://ci.appveyor.com/api/projects/status/4266xwr9m0caeb4t?svg=true)](https://ci.appveyor.com/project/dan-matthews/vulcan)

Firstly, what is it not; It’s NOT Episerver Find and it’s NOT supported – not by Episerver, by me or anyone else. If you want a proper, enterprise-level, supported product with fabulous UI and integration, go dig a little in your pockets and get Find. 

## What is Vulcan?
It is a small, lightweight wrapper around Elasticsearch’s NEST client that provides helpers and tools to index and search for CMS and Commerce content. It’s simple and as it’s Open Source, you can do what you like with it when it comes to extending and customizing it. You can even host your own Elasticsearch instance, so it could be very cost effective!

## Coming Soon

Elastic Search / NEST 5.x support. Its actually in the code base now, just disabled using conditional compilation symbols of NEST2 and NEST5.

## Breaking Changes

Much of the project has been rewritten to take advantage of Episerver's dependency injection system. In doing so, many implementations no longer have empty constructors. Also, implementations for interfaces such as **IVulcanIndexer** and **IVulcanIndexingModifier**, are no longer discovered, they must be manually registered during Episerver initialization.

To Register **IVulcanIndexer** implementations, they must be done in an **IConfigurableModule** as shown below:

```cs
[ModuleDependency(typeof(ServiceContainerInitialization))]
public class RegisterImplementations : IConfigurableModule, IInitializableModule
{
    void IConfigurableModule.ConfigureContainer(ServiceConfigurationContext context)
    {
        // hack: using manual registration as scheduled job doesn't inject otherwise
        context.Services.AddSingleton<IVulcanIndexer, Implementation.VulcanCmsIndexer>();
    }

    void IInitializableModule.Initialize(InitializationEngine context) { }

    void IInitializableModule.Uninitialize(InitializationEngine context) { }
}
```

All other interfaces may be registered using the **ServiceConfigurationAttribute** as noted below

```cs
[ServiceConfiguration(typeof(IVulcanHandler), Lifecycle = ServiceInstanceScope.Singleton)]
```

Another big change is on **IVulcanIndexingModifier** implementations. The interface has changed from:

```cs
void ProcessContent(IContent content, Stream writableStream);
```

to:

```cs
void ProcessContent(IVulcanIndexingModifierArgs modifierArgs);
```

Where **IVulcanIndexingModifierArgs** provides the IContent.Content instance and IDictionary<string,object>.AdditionalItems for adding more fields to be indexed. The AdditionalItems property is merged with the main IContent serialization. Below is an example of adding a new field to be indexed:

```cs
void ProcessContent(IVulcanIndexingModifierArgs args)
{
    // index ancestors
    var ancestors = new List<ContentReference>();

    // constructor injected service
    if (_VulcanContentAncestorLoaders?.Any() == true)
    {
        foreach (var ancestorLoader in _VulcanContentAncestorLoaders)
        {
            IEnumerable<ContentReference> ancestorsFound = ancestorLoader.GetAncestors(args.Content);

            if (ancestorsFound?.Any() == true)
            {
                ancestors.AddRange(ancestorsFound);
            }
        }
    }

    args.AdditionalItems[VulcanFieldConstants.Ancestors] = ancestors.Select(x => x.ToReferenceWithoutVersion()).Distinct();
}
```