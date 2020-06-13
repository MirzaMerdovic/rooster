﻿using MongoDB.Bson;
using MongoDB.Driver;
using Rooster.Connectors.MongoDb.Colections;
using Rooster.DataAccess.LogEntries.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster.DataAccess.LogEntries.Implementations
{
    public class MongoDbLogEntryRepository : ILogEntryRepository<ObjectId>
    {
        private static readonly Func<InsertOneOptions> GetInsertOneOptions = delegate
        {
            return new InsertOneOptions();
        };

        private readonly ILogEntryCollectionFactory _collectionFactory;

        public MongoDbLogEntryRepository(ILogEntryCollectionFactory collectionFactory)
        {
            _collectionFactory = collectionFactory ?? throw new ArgumentNullException(nameof(collectionFactory));
        }

        public async Task Create(LogEntry<ObjectId> entry, CancellationToken cancellation)
        {
            _ = entry ?? throw new ArgumentNullException(nameof(entry));

            var collection = await _collectionFactory.Get<LogEntry<ObjectId>>(cancellation);

            await collection.InsertOneAsync(entry, GetInsertOneOptions(), cancellation);
        }

        public async Task<DateTimeOffset> GetLatestForAppService(ObjectId appServiceId, CancellationToken cancellation)
        {
            if (appServiceId == ObjectId.Empty)
                return default;

            var collection = await _collectionFactory.Get<LogEntry<ObjectId>>(cancellation);

            var filter = Builders<LogEntry<ObjectId>>.Filter.Where(x => x.AppServiceId == appServiceId);
            var sort = Builders<LogEntry<ObjectId>>.Sort.Descending(x => x.Created);

            var cursor = await collection.FindAsync(filter, new FindOptions<LogEntry<ObjectId>, LogEntry<ObjectId>>
            {
                Sort = sort,
                Limit = 1
            });

            var entry = await cursor.FirstOrDefaultAsync();

            return entry.Date;
        }
    }
}