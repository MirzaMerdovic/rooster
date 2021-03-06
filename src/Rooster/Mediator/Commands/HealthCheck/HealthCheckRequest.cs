﻿using MediatR;
using Rooster.Mediator.Commands.Common.Behaviors;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Rooster.Mediator.Commands.HealthCheck
{
    public abstract record HealthCheckRequest :
        IRequest<HealthCheckResponse>,
        IRequestProcessingErrorBehavior
    {
        public void OnError([NotNull] Exception ex)
        {
            return;
        }
    }
}
