using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

public class GrainsStreamOptionsValidatorTests
{
    [Fact]
    public void ValidateConfiguration_Throws_WhenMaxQueueCountIsLessThanOne()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 0
        };
        var sut = new GrainsStreamOptionsValidator(options, "provider");

        Assert.Throws<OrleansConfigurationException>(sut.ValidateConfiguration);
    }

    [Fact]
    public void ValidateConfiguration_Throws_WhenNamespaceIsDuplicated()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "orders",
                    QueueCount = 1
                },
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "orders",
                    QueueCount = 2
                }
            ]
        };
        var sut = new GrainsStreamOptionsValidator(options, "provider");

        Assert.Throws<OrleansConfigurationException>(sut.ValidateConfiguration);
    }

    [Fact]
    public void ValidateConfiguration_Throws_WhenNamespaceQueueCountIsLessThanOne()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "payments",
                    QueueCount = 0
                }
            ]
        };
        var sut = new GrainsStreamOptionsValidator(options, "provider");

        Assert.Throws<OrleansConfigurationException>(sut.ValidateConfiguration);
    }

    [Fact]
    public void ValidateConfiguration_Throws_WhenReplayRetentionBatchCountIsLessThanOne()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1
        };
        var property = typeof(GrainsStreamOptions).GetProperty("ReplayRetentionBatchCount");

        Assert.NotNull(property);

        property!.SetValue(options, 0);

        var sut = new GrainsStreamOptionsValidator(options, "provider");

        Assert.Throws<OrleansConfigurationException>(sut.ValidateConfiguration);
    }

    [Fact]
    public void ValidateConfiguration_DoesNotThrow_ForValidOptions()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 2,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "orders",
                    QueueCount = 1
                },
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "payments",
                    QueueCount = 2
                }
            ]
        };
        var sut = new GrainsStreamOptionsValidator(options, "provider");

        sut.ValidateConfiguration();
    }
}
