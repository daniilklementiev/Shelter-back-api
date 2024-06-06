using System.Reflection;
using Microsoft.Azure.Cosmos;

namespace Shelter.Services;

public class CosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["Azure:CosmosDb:DatabaseName"];
        var containerName = configuration["Azure:CosmosDb:CollectionName"];
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<IEnumerable<T>> GetItemsAsync<T>(string query, params (string, object)[] parameters)
    {
        var queryDefinition = new QueryDefinition(query);

        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.Item1, param.Item2);
        }

        var iterator = _container.GetItemQueryIterator<T>(queryDefinition);
        List<T> results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task AddItemAsync<T>(T item)
    {
        PropertyInfo partitionKeyProperty = item.GetType().GetProperty("partitionKey");
        if (partitionKeyProperty == null)
        {
            throw new ArgumentException("Type must have a 'partitionKey' property");
        }

        string k = item.GetType().GetProperty("partitionKey").GetValue(item).ToString();
        PartitionKey partitionKey = new PartitionKey(k);
        await _container.CreateItemAsync(item, new PartitionKey(k));
    }

    public async Task UpdateItemAsync<T>(string id, T item)
    {

        PropertyInfo partitionKeyProperty = item.GetType().GetProperty("partitionKey");
        if (partitionKeyProperty == null)
        {
            throw new ArgumentException("Type must have a 'partitionKey' property");
        }

        string k = item.GetType().GetProperty("partitionKey").GetValue(item).ToString();
        PartitionKey partitionKey = new PartitionKey(k);

        await _container.ReplaceItemAsync<T>(item, id, partitionKey);
    }

    public async Task DeleteItemAsync<T>(string id, T item)
    {
        PropertyInfo partitionKeyProperty = item.GetType().GetProperty("partitionKey");
        if (partitionKeyProperty == null)
        {
            throw new ArgumentException("Type must have a 'partitionKey' property");
        }

        string k = item.GetType().GetProperty("partitionKey").GetValue(item).ToString();
        PartitionKey partitionKey = new PartitionKey(k);

        await _container.DeleteItemAsync<T>(id, partitionKey);
    }
}