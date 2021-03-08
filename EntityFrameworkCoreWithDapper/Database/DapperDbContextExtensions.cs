using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreWithDapper.Database
{
    public static class DapperDbContextExtensions
    {
        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this DbContext context,
            CancellationToken ct,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(
                context,
                text,
                parameters,
                timeout,
                type,
                ct
            );

            var connection = context.Database.GetDbConnection();
            return await connection.QueryAsync<T>(command.Definition);
        }

        public static async Task<int> ExecuteAsync(
            this DbContext context,
            CancellationToken ct,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(
                context,
                text,
                parameters,
                timeout,
                type,
                ct
            );

            var connection = context.Database.GetDbConnection();
            return await connection.ExecuteAsync(command.Definition);
        }
    }
}