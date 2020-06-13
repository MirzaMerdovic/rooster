﻿using Rooster.DataAccess.LogEntries.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster.DataAccess.LogEntries
{
    public interface ILogEntryRepository<T>
    {
        Task Create(LogEntry<T> entry, CancellationToken cancellation);

        Task<DateTimeOffset> GetLatestForAppService(T appServiceId, CancellationToken cancellation);
    }
}