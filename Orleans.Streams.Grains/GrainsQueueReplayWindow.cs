using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streams.Grains;

[Serializable]
[GenerateSerializer]
[Alias("Orleans.Streams.Grains.GrainsQueueReplayWindow")]
public class GrainsQueueReplayWindow
{
    [Id(0)]
    public List<GrainsQueueBatchContainer> Messages { get; set; } = [];

    [Id(1)]
    public StreamSequenceToken? WarmupCutoffToken { get; set; }
}
