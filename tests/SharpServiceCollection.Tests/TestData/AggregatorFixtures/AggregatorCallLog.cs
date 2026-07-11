using System.Collections.Concurrent;

namespace SharpServiceCollection.Tests.TestData.AggregatorFixtures;

/// <summary>
/// In-memory log shared by the aggregator fixtures so tests can inspect the
/// order in which the generated extension method invoked each entry.
/// </summary>
public static class AggregatorCallLog
{
    private static readonly ConcurrentQueue<(string Aggregator, string ContextKind)> Entries = new();

    public static IReadOnlyCollection<(string Aggregator, string ContextKind)> Snapshot => Entries.ToArray();

    public static void Record(string aggregator, string contextKind)
        => Entries.Enqueue((aggregator, contextKind));

    public static void Reset() => Entries.Clear();
}