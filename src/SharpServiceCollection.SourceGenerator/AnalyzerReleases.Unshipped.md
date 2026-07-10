; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SSC005 | SharpServiceCollection.SourceGenerator | Error | ServiceRegistrationBase subclass must be named ServiceRegistration
SSC006 | SharpServiceCollection.SourceGenerator | Error | ServiceRegistrationBase subclass must be sealed
SSC007 | SharpServiceCollection.SourceGenerator | Error | ServiceRegistration requires an accessible parameterless constructor
