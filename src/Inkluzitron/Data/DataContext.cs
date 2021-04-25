using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;

namespace Inkluzitron.Data
{
    public class DataContext : DbContext
    {
        private readonly string _sqliteDatabaseFilePath;

        public DbSet<BaseTestResult> TestResults { get; set; }
        public DbSet<BorgTestResult> BorgTestResults { get; set; }

        public DbSet<BaseTestResultItem> TestResultItems { get; set; }
        public DbSet<DoubleTestResultItem> DoubleTestResultItems { get; set; }

        public DataContext() : this("inkluzitron.db")
        {
        }

        public DataContext(string sqliteDatabaseFilePath)
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
            modelBuilder.Entity<BaseTestResult>().HasKey(r => r.ResultId);
            modelBuilder.Entity<BaseTestResult>().HasDiscriminator<string>("Discriminator");

            modelBuilder.Entity<BaseTestResultItem>().HasKey(i => i.ItemId);
            modelBuilder.Entity<BaseTestResultItem>().HasOne(i => i.TestResult);
            modelBuilder.Entity<BaseTestResultItem>().HasDiscriminator<string>("Discriminator");

            modelBuilder.Entity<BaseTestResult>().HasMany(r => r.Items);
        }
    }
}
