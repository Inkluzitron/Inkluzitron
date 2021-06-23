using Inkluzitron.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Data
{
    public class BotDatabaseContext : DbContext
    {
        public BotDatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<RicePurityResult> RicePurityResults { get; set; }

        public DbSet<BdsmTestOrgResult> BdsmTestOrgResults { get; set; }
        public DbSet<BdsmTestOrgItem> BdsmTestOrgItems { get; set; }

        public DbSet<RoleMenuMessage> RoleMenuMessages { get; set; }
        public DbSet<RoleMenuMessageRole> RoleMenuMessageRoles { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<DailyUserActivity> DailyUsersActivities { get; set; }

        public DbSet<Invite> Invites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property("Gender").HasConversion<string>();

            modelBuilder.Entity<BdsmTestOrgResult>().HasIndex(r => r.Link).IsUnique();
            modelBuilder.Entity<BdsmTestOrgItem>().Property("Trait").HasConversion<string>();

            modelBuilder.Entity<RoleMenuMessage>().HasKey(m => new { m.GuildId, m.ChannelId, m.MessageId });
            modelBuilder.Entity<RoleMenuMessage>().HasMany(m => m.Roles)
                .WithOne(i => i.Message)
                .HasForeignKey(i => new { i.GuildId, i.ChannelId, i.MessageId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleMenuMessageRole>().HasKey(i => new { i.RoleId, i.GuildId, i.ChannelId, i.MessageId });
        }
    }
}
