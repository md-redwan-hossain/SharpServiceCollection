; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

 Rule ID | Category                               | Severity | Notes                                                                 
---------|----------------------------------------|----------|-----------------------------------------------------------------------
 SSC001  | SharpServiceCollection.SourceGenerator | Error    | Enumerable=true requires TryAdd=true                                  
 SSC002  | SharpServiceCollection.SourceGenerator | Error    | ResolveBy.MatchingInterface requires a matching I{Name} interface     
 SSC003  | SharpServiceCollection.SourceGenerator | Error    | InjectableDependency lifetime must be Singleton, Scoped, or Transient 
 SSC004  | SharpServiceCollection.SourceGenerator | Error    | InjectableDependency resolve strategy is not supported                 
 SSC006  | SharpServiceCollection.SourceGenerator | Error    | ServiceRegistrationBase subclass must be sealed                       
 SSC007  | SharpServiceCollection.SourceGenerator | Error    | ServiceRegistration requires an accessible parameterless constructor  
 SSR008  | SharpServiceCollection.SourceGenerator | Error    | Service registration type must implement ExecuteAsync                 
 SSR999  | SharpServiceCollection.SourceGenerator | Warning  | Debug diagnostic for source generator                                 

