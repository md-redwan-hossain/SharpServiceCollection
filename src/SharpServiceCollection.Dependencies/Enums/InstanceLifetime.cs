namespace SharpServiceCollection.Enums;

public enum InstanceLifetime : byte
{
    Singleton = 1,
    Scoped = 2,
    Transient = 3
}