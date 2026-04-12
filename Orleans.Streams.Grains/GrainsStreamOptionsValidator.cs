using Microsoft.CodeAnalysis;
using Orleans.Runtime;

namespace Orleans.Streams.Grains;

public class GrainsStreamOptionsValidator : IConfigurationValidator
{
    private readonly GrainsStreamOptions _options;
    private readonly string _name;

    public GrainsStreamOptionsValidator(GrainsStreamOptions options, string name)
    {
        _options = options;
        _name = name;
    }

    public void ValidateConfiguration()
    {
        if (_options.MaxStreamNamespaceQueueCount < 1)
        {
            throw new OrleansConfigurationException(
                $"{nameof(GrainsStreamOptions)}.{nameof(GrainsStreamOptions.MaxStreamNamespaceQueueCount)} on stream provider {_name} is invalid. {nameof(GrainsStreamOptions.MaxStreamNamespaceQueueCount)} must be greater than 0");
        }

        if (_options.ReplayRetentionBatchCount < 1)
        {
            throw new OrleansConfigurationException(
                $"{nameof(GrainsStreamOptions)}.{nameof(GrainsStreamOptions.ReplayRetentionBatchCount)} on stream provider {_name} is invalid. {nameof(GrainsStreamOptions.ReplayRetentionBatchCount)} must be greater than 0");
        }

        var duplicates = _options.NamespaceQueue
            .GroupBy(x => x.Namespace)
            .Where(x => x.Count() > 1)
            .ToList();

        if (duplicates.Count > 0)
        {
            var duplicateNamespaces = string.Join(", ", duplicates.Select(x => x.Key));
            throw new OrleansConfigurationException(
                $"{nameof(GrainsStreamOptions)}.{nameof(GrainsStreamOptions.NamespaceQueue)} on stream provider {_name} contains duplicate namespaces: {duplicateNamespaces}");
        }

        foreach (var namespaceQueue in _options.NamespaceQueue)
        {
            if (namespaceQueue.QueueCount < 1)
            {
                throw new OrleansConfigurationException(
                    $"{nameof(GrainsStreamOptions)}.{nameof(GrainsStreamOptions.NamespaceQueue)}.{nameof(GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions.QueueCount)} on stream provider {_name} is invalid. {nameof(GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions.QueueCount)} must be greater than 0");
            }
        }
    }

    public static IConfigurationValidator Create(IServiceProvider services, string name)
    {
        GrainsStreamOptions aqOptions = services.GetOptionsByName<GrainsStreamOptions>(name);
        return new GrainsStreamOptionsValidator(aqOptions, name);
    }
}
