using Inkluzitron.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Data
{
    public class BotDatabaseContext : DbContext
    {
        public BotDatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<BdsmTestOrgQuizResult> BdsmTestOrgQuizResults { get; set; }

        public DbSet<QuizItem> QuizItems { get; set; }
        public DbSet<QuizDoubleItem> DoubleQuizItems { get; set; }

        public DbSet<RolePickerMessage> UserRoleMessage { get; set; }
        public DbSet<RolePickerMessageRole> UserRoleMessageItem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QuizResult>().HasKey(r => r.ResultId);
            modelBuilder.Entity<QuizResult>().HasDiscriminator<string>("Discriminator");
            modelBuilder.Entity<QuizResult>().HasMany(r => r.Items);

            modelBuilder.Entity<QuizItem>().HasKey(i => i.ItemId);
            modelBuilder.Entity<QuizItem>().HasOne(i => i.Parent);
            modelBuilder.Entity<QuizItem>().HasDiscriminator<string>("Discriminator");

            modelBuilder.Entity<RolePickerMessage>().HasKey(m => new { m.GuildId, m.ChannelId, m.MessageId });
            modelBuilder.Entity<RolePickerMessage>().HasMany(m => m.Roles)
                .WithOne(i => i.Message)
                .HasForeignKey(i => new { i.GuildId, i.ChannelId, i.MessageId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePickerMessageRole>().HasKey(i => new { i.Id, i.GuildId, i.ChannelId, i.MessageId });
        }
    }
}
