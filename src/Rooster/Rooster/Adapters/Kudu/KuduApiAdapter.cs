﻿using MediatR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rooster.DataAccess.Logbooks.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static System.Convert;

namespace Rooster.Adapters.Kudu
{
    public interface IKuduApiAdapter<T>
    {
        Task<IEnumerable<Logbook<T>>> GetDockerLogs(CancellationToken cancellation);

        IAsyncEnumerable<string> ExtractLogsFromStream(Logbook<T> logbook);
    }

    public class KuduApiAdapter<T> : IKuduApiAdapter<T>
    {
        private static readonly Func<string, string, AuthenticationHeaderValue> BuildBasicAuthHeader =
            delegate (string user, string password)
            {
                if (string.IsNullOrWhiteSpace(user))
                    throw new ArgumentNullException(nameof(user));

                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException(nameof(password));

                return new AuthenticationHeaderValue("Basic", ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}")));
            };

        private static readonly Func<HttpClient, KuduAdapterOptions, HttpClient> BuildHttpClient =
            delegate (HttpClient client, KuduAdapterOptions options)
            {
                client.DefaultRequestHeaders.Authorization = BuildBasicAuthHeader(options.User, options.Password);
                client.BaseAddress = options.BaseUri;

                return client;
            };

        private readonly HttpClient _client;
        private readonly IMediator _mediator;

        public KuduApiAdapter(IOptionsMonitor<KuduAdapterOptions> options, HttpClient client, IMediator mediator)
        {
            _ = client ?? throw new ArgumentNullException(nameof(client));
            _ = options?.CurrentValue ?? throw new ArgumentNullException(nameof(options));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            _client = BuildHttpClient(client, options?.CurrentValue);
        }

        public async Task<IEnumerable<Logbook<T>>> GetDockerLogs(CancellationToken cancellation)
        {
            using var response = await _client.GetAsync("api/logs/docker", cancellation);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var logs = JsonConvert.DeserializeObject<IEnumerable<Logbook<T>>>(content);

            return FilterDockerLogbooks(logs);

            static IReadOnlyCollection<Logbook<T>> FilterDockerLogbooks(IEnumerable<Logbook<T>> all)
            {
                return all.Where(
                    x =>
                        !x.MachineName.EndsWith("_default", StringComparison.InvariantCultureIgnoreCase) &&
                        !x.MachineName.EndsWith("_msi", StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();
            }
        }

        public async IAsyncEnumerable<string> ExtractLogsFromStream(Logbook<T> logbook)
        {
            _ = logbook ?? throw new ArgumentNullException(nameof(logbook));

            Stream stream = null;

            try
            {
                stream = await _client.GetStreamAsync(logbook.Href);

                using var logReader = new StreamReader(stream);
                stream = null;

                var line = "init";

                while (line != null)
                {
                    line = await logReader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.Contains("docker", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    yield return line;
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}