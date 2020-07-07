﻿using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rooster.Adapters.Kudu;
using Rooster.Adapters.Kudu.Handlers;
using Rooster.DataAccess.AppServices;
using Rooster.DataAccess.ContainerInstances;
using Rooster.DataAccess.Logbooks;
using Rooster.DataAccess.LogEntries;
using Rooster.Hosting;
using Rooster.Mediator.Requests;
using Rooster.SqlServer.Connectors;
using Rooster.SqlServer.DataAccess.AppServices;
using Rooster.SqlServer.DataAccess.ContainerInstances;
using Rooster.SqlServer.DataAccess.Logbooks;
using Rooster.SqlServer.DataAccess.LogEntries;
using Rooster.SqlServer.Handlers;

namespace Rooster.SqlServer.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private const string SqlConfigPath = "Connectors:Sql";

        public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ConnectionFactoryOptions>(configuration.GetSection($"{SqlConfigPath}:{nameof(ConnectionFactoryOptions)}"));

            services.AddSingleton<IConnectionFactory, ConnectionFactory>();

            services.AddTransient<IAppServiceRepository<int>, SqlAppServiceRepository>();
            services.AddTransient<IContainerInstanceRepository<int>, SqlContainerInstanceRepository>();
            services.AddTransient<ILogbookRepository<int>, SqlLogbookRepository>();
            services.AddTransient<ILogEntryRepository<int>, SqlLogEntryRepository>();

            services
                .AddHttpClient<IKuduApiAdapter<int>, KuduApiAdapter<int>>()
                .AddHttpMessageHandler<RequestsInterceptor>();

            services.AddTransient<IRequestHandler<LogEntryRequest<int>>, SqlLogEntryRequestHandler>();

            services.AddHostedService<AppHost<int>>();

            return services;
        }
    }
}