﻿using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rooster.CrossCutting.Serilog;
using Rooster.DependencyInjection;
using Rooster.Mediator.Commands.Common.Behaviors;
using Rooster.Mediator.Commands.ExtractDockerRunParams;
using Rooster.Mediator.Commands.InitKuduPollers;
using Rooster.Mediator.Commands.ProcessAppLogSources;
using Rooster.Mediator.Commands.ProcessDockerLog;
using Rooster.Mediator.Commands.ProcessLogSource;
using Rooster.Mediator.Commands.SendDockerRunParams;
using Rooster.Mediator.Commands.StartKuduPoller;
using Rooster.Mediator.Commands.ValidateExportedRunParams;
using Rooster.Mock.Commands.ProcessLogEntry;
using Rooster.Mock.Commands.SendDockerRunParams;
using Rooster.Mock.Reporters;
using System.Collections.Concurrent;

namespace Rooster.Mock.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IHost AddMockHost(this IHostBuilder builder)
        {
            return builder.AddHost((ctx, services) => AddMock(ctx.Configuration, services));
        }

        private static IServiceCollection AddMock(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton(new HostNameEnricher(nameof(MockHost)));
            services.AddSingleton(new ConcurrentBag<ProcessDockerLogRequest>());

            services.AddTransient<IMockReporter, MockReporter>();

            services.AddKuduApiAdapterCache(configuration);

            services.AddMediatR(new[]
            {
                typeof(ExtractDockerRunParamsRequest),
                typeof(InitKuduPollersRequest),
                typeof(ProcessAppLogSourcesRequest),
                typeof(ProcessDockerLogRequest),
                typeof(ProcessLogSourceRequest),
                typeof(SendDockerRunParamsRequest),
                typeof(StartKuduPollerRequest),
                typeof(ValidateExportedRunParamsRequest)
            });

            services.AddTransient<IRequestHandler<ExtractDockerRunParamsRequest, ExtractDockerRunParamsResponse>, ExtractDockerRunParamsCommand>();
            services.AddTransient<IRequestHandler<InitKuduPollersRequest, Unit>, InitKuduPollersCommand>();
            services.AddTransient<IRequestHandler<ProcessAppLogSourcesRequest, Unit>, ProcessAppLogSourcesCommand>();
            services.AddTransient<IRequestHandler<ProcessLogSourceRequest, Unit>, ProcessLogSourceCommand>();
            services.AddTransient<IRequestHandler<StartKuduPollerRequest, Unit>, StartKuduPollerCommand>();
            services.AddTransient<IRequestHandler<ValidateExportedRunParamsRequest, ValidateExportedRunParamsResponse>, ValidateExportedRunParamsCommand>();
            services.AddTransient<IRequestHandler<ProcessDockerLogRequest, Unit>, MockProcessDockerLogCommand>();
            services.AddTransient<IRequestHandler<SendDockerRunParamsRequest, Unit>, MockSendDockerRunParamsCommand>();

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InstrumentingPipelineBehavior<,>));

            services.AddHostedService<MockHost>();

            return services;
        }
    }
}