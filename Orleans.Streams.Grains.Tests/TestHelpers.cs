using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

internal static class TestHelpers
{
    public static StreamId NewStreamId(string streamNamespace = "ns")
    {
        return StreamId.Create(streamNamespace, Guid.NewGuid());
    }

    public static QueueId NewQueueId(string streamNamespace = "ns")
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = streamNamespace,
                    QueueCount = 1
                }
            ]
        };
        var mapper = new GrainsStreamQueueMapper(options);
        return mapper.GetQueueForStream(NewStreamId(streamNamespace));
    }

    public static GrainsQueueBatchContainer NewBatch(long sequence, string streamNamespace = "ns",
        params object[] payload)
    {
        return new GrainsQueueBatchContainer(
            NewStreamId(streamNamespace),
            payload.ToList(),
            new Dictionary<string, object>(),
            new EventSequenceTokenV2(sequence));
    }
}
