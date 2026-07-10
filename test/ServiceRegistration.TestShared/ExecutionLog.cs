namespace ServiceRegistration.TestShared;

public static class ExecutionLog
{
    private static readonly Lock Gate = new();
    private static readonly List<string> Entries = [];

    public static void Clear()
    {
        lock (Gate)
        {
            Entries.Clear();
        }
    }

    public static void Add(string entry)
    {
        lock (Gate)
        {
            Entries.Add(entry);
        }
    }

    public static IReadOnlyList<string> Snapshot()
    {
        lock (Gate)
        {
            return Entries.ToList();
        }
    }
}

public sealed class TestRegistrationContext
{
    public List<string> Calls { get; } = [];
}
