using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkluzitron.Data
{
    public class DatabaseFactory
    {
        private IServiceProvider Provider { get; }

        public DatabaseFactory(IServiceProvider provider)
        {
            Provider = provider;
        }

        public BotDatabaseContext Create()
        {
            var options = Provider.GetRequiredService<DbContextOptions>();
            return new BotDatabaseContext(options);
        }
    }
}
