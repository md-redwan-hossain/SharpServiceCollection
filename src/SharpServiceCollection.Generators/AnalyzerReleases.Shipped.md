; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

 Rule ID | Category                               | Severity | Notes                                                                 
---------|----------------------------------------|----------|-----------------------------------------------------------------------
 SSC001  | SharpServiceCollection.Generators | Error    | Enumerable=true requires TryAdd=true                                  
 SSC002  | SharpServiceCollection.Generators | Error    | ResolveBy.MatchingInterface requires a matching I{Name} interface     
 SSC003  | SharpServiceCollection.Generators | Error    | InjectableDependency lifetime must be Singleton, Scoped, or Transient 
 SSC004  | SharpServiceCollection.Generators | Error    | InjectableDependency resolve strategy is not supported                 
 SSC006  | SharpServiceCollection.Generators | Error    | ServiceRegistrationBase subclass must be sealed                       
 SSC007  | SharpServiceCollection.Generators | Error    | ServiceRegistration requires an accessible parameterless constructor   

