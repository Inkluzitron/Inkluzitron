using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;

namespace Inkluzitron.Data
{
    public class BotDatabaseContext : DbContext
    {
        private readonly string _sqliteDatabaseFilePath;

        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<BdsmTestOrgQuizResult> BdsmTestOrgQuizResults { get; set; }

        public DbSet<QuizItem> QuizItems { get; set; }
        public DbSet<QuizDoubleItem> DoubleQuizItems { get; set; }

        public BotDatabaseContext() : this("inkluzitron.db")
        {
        }

        public BotDatabaseContext(string sqliteDatabaseFilePath)
        {
            _sqliteDatabaseFilePath = sqliteDatabaseFilePath ?? throw new ArgumentNullException(nameof(sqliteDatabaseFilePath));
        }

        private string BuildConnectionString()
        {
            var builder = new DbConnectionStringBuilder()
            {
                { "Data Source", _sqliteDatabaseFilePath }
            };

            return builder.ConnectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite(BuildConnectionString());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QuizResult>().HasKey(r => r.ResultId);
            modelBuilder.Entity<QuizResult>().HasDiscriminator<string>("Discriminator");
            modelBuilder.Entity<QuizResult>().HasMany(r => r.Items);

            modelBuilder.Entity<QuizItem>().HasKey(i => i.ItemId);
            modelBuilder.Entity<QuizItem>().HasOne(i => i.Parent);
            modelBuilder.Entity<QuizItem>().HasDiscriminator<string>("Discriminator");            
        }
    }
}
