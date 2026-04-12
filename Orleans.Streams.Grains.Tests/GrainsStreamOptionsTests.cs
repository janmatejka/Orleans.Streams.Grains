namespace Orleans.Streams.Grains.Tests;

public class GrainsStreamOptionsTests
{
    [Fact]
    public void ReplayRetentionBatchCount_DefaultsTo1000()
    {
        var property = typeof(GrainsStreamOptions).GetProperty("ReplayRetentionBatchCount");

        Assert.NotNull(property);

        var options = new GrainsStreamOptions();

        Assert.Equal(1000, Convert.ToInt32(property!.GetValue(options)));
    }
}
