﻿using MongoDB.Bson;
using MongoDB.Driver;
using Rooster.Connectors.MongoDb.Colections;
using Rooster.DataAccess.KuduInstances.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster.DataAccess.KuduInstances.Implementations
{
    public class MongoDbKuduInstanceRepository : IKuduInstaceRepository<ObjectId>
    {
        private static readonly Func<InsertOneOptions> GetInsertOneOptions = delegate
        {
            return new InsertOneOptions();
        };

        private readonly IKuduInstanceCollectionFactory _collectionFactory;

        public MongoDbKuduInstanceRepository(IKuduInstanceCollectionFactory collectionFactory)
        {
            _collectionFactory = collectionFactory ?? throw new ArgumentNullException(nameof(collectionFactory));
        }

        public async Task<ObjectId> Create(KuduInstance<ObjectId> kuduInstance, CancellationToken cancellation)
        {
            _ = kuduInstance ?? throw new ArgumentNullException(nameof(kuduInstance));

            var collection = await _collectionFactory.Get<KuduInstance<ObjectId>>(cancellation);

            await collection.InsertOneAsync(kuduInstance, GetInsertOneOptions(), cancellation);

            return kuduInstance.Id;
        }

        public async Task<ObjectId> GetIdByName(string name, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(name))
                return default;

            var trimmedName = name.Trim();
            var collection = await _collectionFactory.Get<KuduInstance<ObjectId>>(cancellation);

            var cursor = await collection.FindAsync(x => x.Name == trimmedName, null, cancellation);

            var kuduInstance = await cursor.FirstOrDefaultAsync();

            return kuduInstance.Id;
        }

        public async Task<string> GetNameById(ObjectId id, CancellationToken cancellation)
        {
            if (id == ObjectId.Empty)
                return default;

            var collection = await _collectionFactory.Get<KuduInstance<ObjectId>>(cancellation);

            var cursor = await collection.FindAsync(x => x.Id == id, null, cancellation);

            var kuduInstance = await cursor.FirstOrDefaultAsync();

            return kuduInstance.Name;
        }
    }
}