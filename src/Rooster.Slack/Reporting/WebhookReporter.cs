﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rooster.QoS.Resilency;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster.Slack.Reporting
{
    public interface IReporter
    {
        Task Send(MemoryStream jsonStream, CancellationToken cancellation);
    }

    public class WebHookReporter : IReporter
    {
        private const string ReportSentToUrlLog = "Report sent to URL: {0} with received status: {1}";

        private static readonly Func<HttpResponseMessage, bool> TransientHttpStatusCodePredicate =
            delegate (HttpResponseMessage response)
        {
            if (response.StatusCode < HttpStatusCode.InternalServerError)
                return response.StatusCode == HttpStatusCode.RequestTimeout;

            return true;
        };

        private static readonly Action<HttpResponseMessage> ThrowHttpRequestException = delegate (HttpResponseMessage response)
        {
            throw new HttpRequestException(response.ReasonPhrase) { Data = { [nameof(HttpStatusCode)] = response.StatusCode } };
        };

        private readonly WebHookReporterOptions _options;
        private readonly HttpClient _client;
        private readonly IRetryProvider _retryProvider;
        private readonly ILogger _logger;

        public WebHookReporter(
            IOptionsMonitor<WebHookReporterOptions> options,
            HttpClient client,
            IRetryProvider retryProvider,
            ILogger<WebHookReporter> logger)
        {
            _options = options.CurrentValue;
            _client = client;
            _retryProvider = retryProvider;
            _logger = logger;
        }

        public async Task Send(MemoryStream jsonStream, CancellationToken cancellation)
        {
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellation);

            try
            {
                await
                    _retryProvider.RetryOn<HttpRequestException, HttpResponseMessage>(
                        CheckError,
                        TransientHttpStatusCodePredicate,
                        () => SendRequest(_client, _options, jsonStream, _logger, linkedSource.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report sending failed", Array.Empty<object>());
            }
        }


        private static bool CheckError(HttpRequestException x)
        {
            if (!x.Data.Contains(nameof(HttpStatusCode)))
                return false;

            var statusCode = (HttpStatusCode)x.Data[nameof(HttpStatusCode)];

            if (statusCode < HttpStatusCode.InternalServerError)
                return statusCode == HttpStatusCode.RequestTimeout;

            return false;
        }

        private static async Task<HttpResponseMessage> SendRequest(
            HttpClient client,
            WebHookReporterOptions options,
            MemoryStream jsonStream,
            ILogger logger,
            CancellationToken cancellation)
        {
            var request = CreatePostMessage(new Uri(client.BaseAddress + options.Url), jsonStream);

            if (options.Authorization != null)
                request.Headers.Authorization = BuildAuthHeader(options.Authorization);

            options.Headers.ToList().ForEach(header => request.Headers.Add(header.Name, header.Value));

            var response = await client.SendAsync(request, cancellation);

            logger.LogDebug(ReportSentToUrlLog, request.RequestUri.ToString(), response.StatusCode);

            if (!response.IsSuccessStatusCode)
                ThrowHttpRequestException(response);

            return response;
        }

        private static HttpRequestMessage CreatePostMessage(Uri uri, MemoryStream jsonStream)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = new ReadOnlyMemoryContent(jsonStream.ToArray())
            };
        }

        private static AuthenticationHeaderValue BuildAuthHeader(Authorization authorizationOptions)
        {
            return new AuthenticationHeaderValue(authorizationOptions.Scheme, authorizationOptions.Parameter);
        }
    }
}
