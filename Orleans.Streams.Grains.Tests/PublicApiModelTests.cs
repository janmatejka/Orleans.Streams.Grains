namespace Orleans.Streams.Grains.Tests;

public class PublicApiModelTests
{
    [Fact]
    public void QueueMessageDroppedException_Constructors_ExposeMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");

        var sut = new QueueMessageDroppedException("msg", inner);

        Assert.Equal("msg", sut.Message);
        Assert.Same(inner, sut.InnerException);
    }

    [Fact]
    public void QueueStatus_ToString_ContainsKeyFields()
    {
        var status = new QueueStatus("etag", 10, 3, 1, 2, 4);
        var replayMessagesProperty = typeof(QueueStatus).GetProperty("ReplayMessagesCount");

        Assert.NotNull(replayMessagesProperty);
        Assert.Equal(4, status.ReplayMessagesCount);

        var text = status.ToString();

        Assert.Contains("ETag:etag", text);
        Assert.Contains("LastReadMessage:10", text);
        Assert.Contains("ReplayMessagesCount:4", text);
    }

    [Fact]
    public void GrainsQueueStatus_BehavesAsDictionary()
    {
        var queueId = TestHelpers.NewQueueId();
        var status = new QueueStatus("etag", 1, 2, 3, 4);
        var sut = new GrainsQueueStatus
        {
            [queueId] = status
        };

        Assert.Single(sut);
        Assert.Same(status, sut[queueId]);
    }

    [Fact]
    public void ProviderConsts_HaveExpectedValues()
    {
        Assert.Equal("Queue", GrainsStreamProviderConsts.QueueStateName);
        Assert.Equal("QueueStore", GrainsStreamProviderConsts.QueueStorageName);
        Assert.Equal("GrainsQueueStorage", GrainsStreamProviderConsts.ProviderType);
    }
}
