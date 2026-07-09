namespace SharpServiceCollection.SourceGenerator.InternalTypes;

internal static class GeneratorConstants
{
    internal const string GeneratedFileName = "SharpServiceCollection.Generated.g.cs";
    internal const string DiagnosticCategory = "SharpServiceCollection.SourceGenerator";
    internal const string InterfaceNamePrefix = "I";
    internal const string UnsupportedLifetimeMessage = "Unsupported lifetime";

    internal static class AttributeMetadata
    {
        internal const string Name = "InjectableDependencyAttribute";
        internal const string Namespace = "SharpServiceCollection.Attributes";
    }

    internal static class AttributeProperties
    {
        internal const string TryAdd = nameof(RegistrationModel.TryAdd);
        internal const string Enumerable = nameof(RegistrationModel.Enumerable);
        internal const string Key = nameof(RegistrationModel.Key);
        internal const string Order = nameof(RegistrationModel.Order);
    }

    internal static class GeneratedCode
    {
        internal const string Namespace = "SharpServiceCollection.Generated";
        internal const string ExtensionsClassName = "GeneratedServiceCollectionExtensions";
        internal const string AddServicesMethodName = "AddGeneratedServices";
    }

    internal static class DependencyInjection
    {
        internal const string ServiceCollectionType =
            "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

        internal const string ServiceDescriptorType =
            "global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor";

        internal const string ServiceLifetimeType =
            "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime";

        internal const string TryAddEnumerableMethod = "TryAddEnumerable";
        internal const string DescribeMethod = "Describe";

        internal static class MethodPrefixes
        {
            internal const string TryAdd = "TryAdd";
            internal const string Add = "Add";
            internal const string Keyed = "Keyed";
        }

        internal static class Lifetimes
        {
            internal const string Singleton = "Singleton";
            internal const string Scoped = "Scoped";
            internal const string Transient = "Transient";
        }

        internal static class Methods
        {
            internal const string TryAddSingleton = MethodPrefixes.TryAdd + Lifetimes.Singleton;
            internal const string TryAddScoped = MethodPrefixes.TryAdd + Lifetimes.Scoped;
            internal const string TryAddTransient = MethodPrefixes.TryAdd + Lifetimes.Transient;
            internal const string AddSingleton = MethodPrefixes.Add + Lifetimes.Singleton;
            internal const string AddScoped = MethodPrefixes.Add + Lifetimes.Scoped;
            internal const string AddTransient = MethodPrefixes.Add + Lifetimes.Transient;
            internal const string TryAddKeyedSingleton = MethodPrefixes.TryAdd + MethodPrefixes.Keyed + Lifetimes.Singleton;
            internal const string TryAddKeyedScoped = MethodPrefixes.TryAdd + MethodPrefixes.Keyed + Lifetimes.Scoped;
            internal const string TryAddKeyedTransient = MethodPrefixes.TryAdd + MethodPrefixes.Keyed + Lifetimes.Transient;
            internal const string AddKeyedSingleton = MethodPrefixes.Add + MethodPrefixes.Keyed + Lifetimes.Singleton;
            internal const string AddKeyedScoped = MethodPrefixes.Add + MethodPrefixes.Keyed + Lifetimes.Scoped;
            internal const string AddKeyedTransient = MethodPrefixes.Add + MethodPrefixes.Keyed + Lifetimes.Transient;
        }
    }
}
