using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCoreWithDapper.Database
{
    public class ApiDbSqlRunner
    {
        private readonly ILogger<ApiDbSqlRunner> _logger;
        private readonly ApiDbContext _context;

        public ApiDbSqlRunner(
            ILogger<ApiDbSqlRunner> logger,
            ApiDbContext context
        )
        {
            _logger = logger;
            _context = context;
        }

        public async Task<int> ExecuteAsync(
            CancellationToken ct,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            var cmd = BuildCommand(
                text,
                parameters,
                timeout,
                type,
                ct
            );

            LogCommand(cmd);

            var connection = _context.Database.GetDbConnection();
            return await connection.ExecuteAsync(text, cmd);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(
            CancellationToken ct,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            var cmd = BuildCommand(
                text,
                parameters,
                timeout,
                type,
                ct
            );

            LogCommand(cmd);

            var connection = _context.Database.GetDbConnection();
            return await connection.QueryAsync<T>(cmd);
        }

        private CommandDefinition BuildCommand(
            string text,
            object parameters,
            int? timeout,
            CommandType? type,
            CancellationToken ct
        )
        {
            var tx = _context.Database.CurrentTransaction?.GetDbTransaction();

            return new CommandDefinition(
                text,
                parameters,
                tx,
                timeout ?? _context.Database.GetCommandTimeout(),
                type,
                cancellationToken: ct
            );
        }

        private void LogCommand(CommandDefinition cmd)
        {
            if(!_logger.IsEnabled(LogLevel.Debug))
                return;

            _logger.LogDebug(@"
Executing DbCommand [CommandType='{commandType}', CommandTimeout='{commandTimeout}']
{commandText}", cmd.CommandType, cmd.CommandTimeout, cmd.CommandText);
        }
    }
}