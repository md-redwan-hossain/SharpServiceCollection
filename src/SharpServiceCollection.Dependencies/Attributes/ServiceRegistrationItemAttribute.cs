namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ServiceRegistrationItemAttribute : Attribute
{
    public int Priority { get; set; }
}
