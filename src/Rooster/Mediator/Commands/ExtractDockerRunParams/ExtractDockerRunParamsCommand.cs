﻿using Microsoft.Extensions.Logging;
using Rooster.CrossCutting.Docker;
using Rooster.Mediator.Commands.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rooster.Mediator.Commands.ExtractDockerRunParams
{
    public class ExtractDockerRunParamsCommand :
        IOpinionatedRequestHandler<ExtractDockerRunParamsRequest, ExtractDockerRunParamsResponse>
    {
        private const string LogDockerLogLineReceived = "Received docker log line: {DockerLogLine}";

        private readonly ILogger _logger;

        public ExtractDockerRunParamsCommand(ILogger<ExtractDockerRunParamsCommand> logger)
        {
            _logger = logger;
        }

        public Task<ExtractDockerRunParamsResponse> Handle(ExtractDockerRunParamsRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug(LogDockerLogLineReceived, request.LogLine);

            var metadata = LogExtractor.Extract(request.LogLine);

            return Task.FromResult(
                new ExtractDockerRunParamsResponse
                {
                    ServiceName = metadata.ServiceName.ToString(),
                    ContainerName = metadata.ContainerName.ToString(),
                    ImageName = metadata.ImageName.ToString(),
                    ImageTag = metadata.ImageTag.ToString(),
                    InboundPort = metadata.InboundPort.ToString(),
                    OutboundPort = metadata.OutboundPort.ToString(),
                    EventDate = metadata.Date
                });
        }
    }
}