using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;

namespace SharpServiceCollection.Tests.TestData.AggregatorFixtures;

/// <summary>
/// Aggregator that records every registry call against the static
/// <see cref="AggregatorCallLog"/>. The aggregator-level <c>Order</c>
/// property is varied per instance so tests can assert the deterministic
/// per-context emit order.
/// </summary>
public abstract class CallLoggingAggregator
{
    public const string NonGenericContextKey = "non-generic-call";
    public const string StringContextKey = "string-context-call";
    public const string IntContextKey = "int-context-call";

    protected static void Record(string aggregator, string contextKind)
    {
        AggregatorCallLog.Record(aggregator, contextKind);
    }
}

[ServiceRegistration(Order = 30)]
public sealed class LastNonGenericAggregator : CallLoggingAggregator
{
    public Task ExecuteAsync(IServiceCollection services)
    {
        Record(nameof(LastNonGenericAggregator), NonGenericContextKey);
        return Task.CompletedTask;
    }
}

[ServiceRegistration(Order = 10)]
public sealed class FirstNonGenericAggregator : CallLoggingAggregator
{
    public Task ExecuteAsync(IServiceCollection services)
    {
        Record(nameof(FirstNonGenericAggregator), NonGenericContextKey);
        return Task.CompletedTask;
    }
}

[ServiceRegistration<string>(Order = 10)]
public sealed class FirstStringAggregator : CallLoggingAggregator
{
    public Task ExecuteAsync(IServiceCollection services, string context)
    {
        Record(nameof(FirstStringAggregator), StringContextKey);
        return Task.CompletedTask;
    }
}

[ServiceRegistration<string>(Order = 30)]
public sealed class LastStringAggregator : CallLoggingAggregator
{
    public Task ExecuteAsync(IServiceCollection services, string context)
    {
        Record(nameof(LastStringAggregator), StringContextKey);
        return Task.CompletedTask;
    }
}

[ServiceRegistration<int>(Order = 5)]
public sealed class FirstIntAggregator : CallLoggingAggregator
{
    public Task ExecuteAsync(IServiceCollection services, int context)
    {
        Record(nameof(FirstIntAggregator), IntContextKey);
        return Task.CompletedTask;
    }
}