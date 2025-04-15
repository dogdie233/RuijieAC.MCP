namespace RuijieAC.MCP.Utils;

public static class QueueExtension
{
    public static void EnqueueRange<T>(this Queue<T> queue, ReadOnlySpan<T> elements)
    {
        foreach (var elem in elements)
            queue.Enqueue(elem);
    }

    public static int DequeueRangeTo<T>(this Queue<T> queue, Span<T> dest)
    {
        var index = 0;
        while (queue.Count > 0 && index < dest.Length)
            dest[index++] = queue.Dequeue();
        return index;
    }
}