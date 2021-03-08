using System;
using System.Data;
using System.Threading;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCoreWithDapper.Database
{
    public readonly struct DapperEFCoreCommand : IDisposable
    {
        private readonly ILogger<DapperEFCoreCommand> _logger;

        public DapperEFCoreCommand(
            DbContext context,
            string text,
            object parameters,
            int? timeout,
            CommandType? type,
            CancellationToken ct
        )
        {
            _logger = context.GetService<ILogger<DapperEFCoreCommand>>();

            var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            var commandType = type ?? CommandType.Text;
            var commandTimeout = timeout ?? context.Database.GetCommandTimeout() ?? 30;

            Definition = new CommandDefinition(
                text,
                parameters,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken: ct
            );

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    @"Executing DbCommand [CommandType='{commandType}', CommandTimeout='{commandTimeout}']
{commandText}", Definition.CommandType, Definition.CommandTimeout, Definition.CommandText);
            }
        }

        public CommandDefinition Definition { get; }

        public void Dispose()
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    @"Executed DbCommand [CommandType='{commandType}', CommandTimeout='{commandTimeout}']
{commandText}", Definition.CommandType, Definition.CommandTimeout, Definition.CommandText);
            }
        }
    }
}