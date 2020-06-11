﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rooster.Adapters.Kudu;
using Rooster.DataAccess.AppServices.Implementations.Sql;
using Rooster.DataAccess.KuduInstances.Implementations.Sql;
using Rooster.DataAccess.Logbooks.Implementations.Sql;
using Rooster.DataAccess.LogEntries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster
{
    internal class SqlAppHost : IHostedService
    {
        private readonly AppHostOptions _options;
        private readonly IKuduApiAdapter _kudu;
        private readonly ILogExtractor _extractor;
        private readonly ISqlLogbookRepository _logbookRepository;
        private readonly ILogEntryRepository _logEntryRepository;
        private readonly ISqlAppServiceRepository _appServiceRepository;
        private readonly ISqlKuduInstanceRepository _kuduInstanceRepository;
        private readonly ILogger _logger;

        public SqlAppHost(
            IOptionsMonitor<AppHostOptions> options,
            IKuduApiAdapter kudu,
            ILogExtractor extractor,
            ISqlLogbookRepository logbookRepository,
            ILogEntryRepository logEntryRepository,
            ISqlAppServiceRepository appServiceRepository,
            ISqlKuduInstanceRepository kuduInstanceRepository,
            ILogger<SqlAppHost> logger)
        {
            _options = options.CurrentValue ?? throw new ArgumentNullException(nameof(options));
            _kudu = kudu ?? throw new ArgumentNullException(nameof(kudu));
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            _logbookRepository = logbookRepository ?? throw new ArgumentNullException(nameof(logbookRepository));
            _logEntryRepository = logEntryRepository ?? throw new ArgumentNullException(nameof(logEntryRepository));
            _appServiceRepository = appServiceRepository ?? throw new ArgumentNullException(nameof(appServiceRepository));
            _kuduInstanceRepository = kuduInstanceRepository ?? throw new ArgumentNullException(nameof(kuduInstanceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            while (true)
            {
                var logbooks = await _kudu.GetLogs(cancellationToken);

                foreach (var logbook in logbooks)
                {
                    var latestLogbook = await _logbookRepository.GetLast(cancellationToken);

                    if (latestLogbook == null)
                    {
                        logbook.KuduInstance = await _kuduInstanceRepository.GetIdByName(logbook.Href.Host, cancellationToken);

                        if (logbook.KuduInstance == default)
                            logbook.KuduInstance = await _kuduInstanceRepository.Create(logbook.KuduInstance, cancellationToken);

                        await _logbookRepository.Create(logbook, cancellationToken);
                        latestLogbook = logbook;
                    }

                    if (logbook.LastUpdated < latestLogbook.LastUpdated)
                        continue;

                    await _kudu.ExtractLogsFromStream(logbook.Href, cancellationToken, ExtractAndPersistDockerLogLine);
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PoolingIntervalInSeconds));
            }
        }

        private async Task ExtractAndPersistDockerLogLine(string line, CancellationToken cancellation)
        {
            var logEntry = _extractor.Extract(line);

            logEntry.AppService = await _appServiceRepository.GetIdByName(logEntry.AppService.Name, cancellation);

            if (logEntry.AppService.Id == default)
                logEntry.AppService = await _appServiceRepository.Create(logEntry.AppService, cancellation);

            var latestLogEntry = await _logEntryRepository.GetLatestForAppService(logEntry.AppService.Id);

            if (logEntry.Date <= latestLogEntry)
                return;

            await _logEntryRepository.Create(logEntry);

            // TODO: Add Slack integration.
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
