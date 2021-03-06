﻿using MediatR;
using Rooster.Mediator.Commands.Common;
using Rooster.Mediator.Commands.Common.Behaviors;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Rooster.Mediator.Commands.SendDockerRunParams
{
    public sealed record SendDockerRunParamsRequest :
        DockerRunParams,
        IRequest,
        IRequestProcessingErrorBehavior
    {
        public void OnError([NotNull] Exception ex)
        {
            return;
        }

        internal static SendDockerRunParamsRequest FromBase(DockerRunParams parameters) =>
            new()
            {
                Created = parameters.Created,
                ServiceName = parameters.ServiceName,
                ContainerName = parameters.ContainerName,
                ImageName = parameters.ImageName,
                ImageTag = parameters.ImageTag,
                InboundPort = parameters.InboundPort,
                OutboundPort = parameters.OutboundPort,
                EventDate = parameters.EventDate
            };
    }
}