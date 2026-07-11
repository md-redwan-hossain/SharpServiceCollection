using System.Text.RegularExpressions;

namespace SharpServiceCollection.Generators.InternalTypes;

internal static class AssemblyNameSanitizer
{
    private const string FallbackAssemblyName = "Assembly";
    private static readonly Regex NonAlphanumericRegex = new("[^a-zA-Z0-9]+", RegexOptions.Compiled);

    internal static string Sanitize(string? assemblyName)
    {
        var name = assemblyName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return FallbackAssemblyName;
        }

        var sanitised = NonAlphanumericRegex.Replace(name, string.Empty);
        return string.IsNullOrEmpty(sanitised) ? FallbackAssemblyName : sanitised;
    }
}
