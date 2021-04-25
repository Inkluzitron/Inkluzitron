using Inkluzitron.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class DataMigrationService
    {
        public BotDatabaseContext Context { get; }

        public DataMigrationService(BotDatabaseContext context)
        {
            Context = context;
        }        

        public async Task StartAsync()
        {
            await Context.Database.MigrateAsync();
        }
    }
}
