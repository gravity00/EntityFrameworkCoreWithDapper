using System;
using System.Data;
using Dapper;
using EntityFrameworkCoreWithDapper.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCoreWithDapper
{
    public class Startup
    {
        private const string ConnectionString = "Data Source=EntityFrameworkCoreWithDapper;Mode=Memory;Cache=Shared";
        private static SqliteConnection _keepAliveConnection;

        public void ConfigureServices(IServiceCollection services)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            services.AddDbContext<ApiDbContext>(o =>
            {
                o.UseSqlite(ConnectionString);
            });

            services.AddMvc();

            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app)
        {
            _keepAliveConnection = new SqliteConnection(ConnectionString);
            _keepAliveConnection.Open();

            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                ctx.Database.EnsureCreated();
            }

            app.UseDeveloperExceptionPage();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "EF Core with Dapper Example Api V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value;

            public override Guid Parse(object value) => Guid.Parse((string) value);
        }
    }
}
