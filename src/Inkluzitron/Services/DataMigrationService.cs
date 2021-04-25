using Inkluzitron.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class DataMigrationService
    {
        public DataContext Context { get; }

        public DataMigrationService(DataContext context)
        {
            Context = context;
        }        

        public async Task StartAsync()
        {
            await Context.Database.MigrateAsync();
        }
    }
}
