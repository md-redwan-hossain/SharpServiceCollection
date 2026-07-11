namespace SharpServiceCollection.Generators.Constants;

internal static class InjectableDependencyConstants
{
    internal const string RuntimeAssemblyName = "SharpServiceCollection";
    internal const string GeneratedFileName = "SharpServiceCollection.Generated.g.cs";
    internal const string DiagnosticCategory = "SharpServiceCollection.Generators";
    internal const string InterfaceNamePrefix = "I";
    internal const string UnsupportedLifetimeMessage = "Unsupported lifetime";

    // Tracking names for every pipeline stage. Centralised so a rename touches
    // one place instead of every WithTrackingName call site. Each entry is the
    // full string passed to `WithTrackingName(...)`; consumers reference these
    // via `using static` so call sites stay short.
    internal static class TrackingNames
    {
        // ServiceRegistrationGenerator
        internal const string ServiceRegistrationNonGeneric = "ServiceRegistration.NonGeneric";
        internal const string ServiceRegistrationGeneric = "ServiceRegistration.Generic";
        internal const string ServiceRegistrationCollectDiagnostics = "ServiceRegistration.CollectDiagnostics";
        internal const string ServiceRegistrationCombineCompilation = "ServiceRegistration.CombineCompilation";

        // InjectableDependencyGenerator
        internal const string InjectableDependencyNonGeneric = "InjectableDependency.NonGeneric";
        internal const string InjectableDependencyGeneric = "InjectableDependency.Generic";
        internal const string InjectableDependencyCollectNonGeneric = "InjectableDependency.CollectNonGeneric";
        internal const string InjectableDependencyCollectGeneric = "InjectableDependency.CollectGeneric";
        internal const string InjectableDependencyCombineStreams = "InjectableDependency.CombineStreams";
        internal const string InjectableDependencyCombineAssembly = "InjectableDependency.CombineAssembly";
    }

    internal static class AttributeMetadata
    {
        internal const string Name = "InjectableDependencyAttribute";
        internal const string ServiceRegistrationName = "ServiceRegistrationAttribute";
        internal const string Namespace = "SharpServiceCollection.Attributes";
        internal const string ServiceRegistrationMetadataName = $"{Namespace}.{ServiceRegistrationName}";
        internal const string ServiceRegistrationGenericMetadataName = $"{Namespace}.{ServiceRegistrationName}`1";
    }

    internal static class AttributeProperties
    {
        internal const string TryAdd = "TryAdd";
        internal const string Enumerable = "Enumerable";
        internal const string Key = "Key";
        internal const string Order = "Order";
    }

    internal static class GeneratedCode
    {
        internal const string Namespace = "SharpServiceCollection.Generated";
        internal const string ExtensionsClassName = "GeneratedServiceCollectionExtensions";
        internal const string AddServicesMethodName = "AddAttributedServices";
        internal const string AddServicesMethodNamePrefix = "AddAttributedServicesFrom";
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
            internal const string TryAdd = nameof(TryAdd);
            internal const string Add = nameof(Add);
            internal const string Keyed = nameof(Keyed);
        }

        internal static class Lifetimes
        {
            internal const string Singleton = nameof(Singleton);
            internal const string Scoped = nameof(Scoped);
            internal const string Transient = nameof(Transient);
        }

        internal static class Methods
        {
            internal const string TryAddSingleton = $"{MethodPrefixes.TryAdd}{Lifetimes.Singleton}";
            internal const string TryAddScoped = $"{MethodPrefixes.TryAdd}{Lifetimes.Scoped}";
            internal const string TryAddTransient = $"{MethodPrefixes.TryAdd}{Lifetimes.Transient}";
            internal const string AddSingleton = $"{MethodPrefixes.Add}{Lifetimes.Singleton}";
            internal const string AddScoped = $"{MethodPrefixes.Add}{Lifetimes.Scoped}";
            internal const string AddTransient = $"{MethodPrefixes.Add}{Lifetimes.Transient}";

            internal const string TryAddKeyedSingleton =
                $"{MethodPrefixes.TryAdd}{MethodPrefixes.Keyed}{Lifetimes.Singleton}";

            internal const string TryAddKeyedScoped =
                $"{MethodPrefixes.TryAdd}{MethodPrefixes.Keyed}{Lifetimes.Scoped}";

            internal const string TryAddKeyedTransient =
                $"{MethodPrefixes.TryAdd}{MethodPrefixes.Keyed}{Lifetimes.Transient}";

            internal const string AddKeyedSingleton =
                $"{MethodPrefixes.Add}{MethodPrefixes.Keyed}{Lifetimes.Singleton}";

            internal const string AddKeyedScoped = $"{MethodPrefixes.Add}{MethodPrefixes.Keyed}{Lifetimes.Scoped}";

            internal const string AddKeyedTransient =
                $"{MethodPrefixes.Add}{MethodPrefixes.Keyed}{Lifetimes.Transient}";
        }
    }
}