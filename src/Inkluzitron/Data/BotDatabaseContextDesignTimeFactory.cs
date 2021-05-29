using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inkluzitron.Data
{
    // https://go.microsoft.com/fwlink/?linkid=851728
    public class BotDatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<BotDatabaseContext>
    {
        public BotDatabaseContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BotDatabaseContext>();
            optionsBuilder.UseSqlite("Data Source=inkluzitron.db");

            return new BotDatabaseContext(optionsBuilder.Options);
        }
    }
}
