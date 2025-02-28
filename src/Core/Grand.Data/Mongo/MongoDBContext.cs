﻿using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Xml.Linq;

namespace Grand.Data.Mongo;

public class MongoDBContext : IDatabaseContext
{
    private readonly IMongoDatabase _database;

    public MongoDBContext(IMongoDatabase mongodatabase)
    {
        _database = mongodatabase;
    }
    
    public async Task<bool> DatabaseExist()
    {
        var filter = new BsonDocument("name", "GrandNodeVersion");
        var found = await _database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
        return await found.AnyAsync();
    }

    public async Task CreateTable(string name, string collation)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);

        if (!string.IsNullOrEmpty(collation))
        {
            var options = new CreateCollectionOptions {
                Collation = new Collation(collation)
            };
            await _database.CreateCollectionAsync(name, options);
        }
        else
        {
            await _database.CreateCollectionAsync(name);
        }
    }

    public async Task DeleteTable(string name)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        await _database.DropCollectionAsync(name);
    }

    public async Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName,
        bool unique = false) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);

        IList<IndexKeysDefinition<T>> keys = new List<IndexKeysDefinition<T>>();
        foreach (var item in orderBuilder.Fields)
            if (item.selector != null)
                keys.Add(item.value
                    ? Builders<T>.IndexKeys.Ascending(item.selector)
                    : Builders<T>.IndexKeys.Descending(item.selector));
            else
                keys.Add(item.value
                    ? Builders<T>.IndexKeys.Ascending(item.fieldName)
                    : Builders<T>.IndexKeys.Descending(item.fieldName));

        try
        {
            await ((MongoRepository<T>)repository).Collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(
                Builders<T>.IndexKeys.Combine(keys),
                new CreateIndexOptions { Name = indexName, Unique = unique }));
        }
        catch { }
    }

    public async Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);
        try
        {
            await ((MongoRepository<T>)repository).Collection.Indexes.DropOneAsync(indexName);
        }
        catch { }
    }
}