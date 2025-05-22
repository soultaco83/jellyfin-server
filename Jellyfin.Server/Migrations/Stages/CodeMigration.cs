using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jellyfin.Server.Migrations.Stages;

internal class CodeMigration(Type migrationType, JellyfinMigrationAttribute metadata)
{
    public Type MigrationType { get; } = migrationType;

    public JellyfinMigrationAttribute Metadata { get; } = metadata;

    public string BuildCodeMigrationId()
    {
        return Metadata.Order.ToString("yyyyMMddHHmmsss", CultureInfo.InvariantCulture) + "_" + Metadata.Name!;
    }

    private ServiceCollection MigrationServices(IServiceProvider serviceProvider, IStartupLogger logger)
    {
        var childServiceCollection = new ServiceCollection();
        childServiceCollection.AddSingleton(serviceProvider);
        childServiceCollection.AddSingleton(logger);

        foreach (ServiceDescriptor service in serviceProvider.GetRequiredService<IServiceCollection>())
        {
            if (service.Lifetime == ServiceLifetime.Singleton && !service.ServiceType.IsGenericTypeDefinition)
            {
                object? serviceInstance = serviceProvider.GetService(service.ServiceType);
                if (serviceInstance != null)
                {
                    childServiceCollection.AddSingleton(service.ServiceType, serviceInstance);
                    continue;
                }
            }

            childServiceCollection.Add(service);
        }

        return childServiceCollection;
    }

    public async Task Perform(IServiceProvider? serviceProvider, IStartupLogger logger, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (typeof(IMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            if (serviceProvider is null)
            {
                ((IMigrationRoutine)Activator.CreateInstance(MigrationType)!).Perform();
            }
            else
            {
                using var migrationServices = MigrationServices(serviceProvider, logger).BuildServiceProvider();
                ((IMigrationRoutine)ActivatorUtilities.CreateInstance(migrationServices, MigrationType)).Perform();
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        else if (typeof(IAsyncMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            if (serviceProvider is null)
            {
                await ((IAsyncMigrationRoutine)Activator.CreateInstance(MigrationType)!).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using var migrationServices = MigrationServices(serviceProvider, logger).BuildServiceProvider();
                await ((IAsyncMigrationRoutine)ActivatorUtilities.CreateInstance(migrationServices, MigrationType)).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            throw new InvalidOperationException($"The type {MigrationType} does not implement either IMigrationRoutine or IAsyncMigrationRoutine and is not a valid migration type");
        }
    }
}
