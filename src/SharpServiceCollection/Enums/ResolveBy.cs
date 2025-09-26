namespace SharpServiceCollection.Enums;

public enum ResolveBy : byte
{
    Self = 1,
    ImplementedInterface,
    MatchingInterface,
    SelfAndReplace,
    ImplementedInterfaceAndReplace,
    MatchingInterfaceAndReplace
}