using ImageMagick;
using Inkluzitron.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Globalization;

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
        public DbSet<ScheduledTask> ScheduledTasks { get; set; }
        public DbSet<VoteReplyRecord> VoteReplyRecords { get; set; }

        public DbSet<Badge> Badges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(builder =>
            {
                builder.Property(o => o.Gender).HasConversion<string>();
                builder.HasIndex(o => o.KisNickname).IsUnique();
                builder.Ignore(o => o.BirthdayDate);
            });

            modelBuilder.Entity<Invite>().HasIndex(o => o.InviteCode).IsUnique();

            modelBuilder.Entity<BdsmTestOrgResult>().HasIndex(r => r.Link).IsUnique();
            modelBuilder.Entity<BdsmTestOrgItem>().Property("Trait").HasConversion<string>();

            modelBuilder.Entity<RoleMenuMessage>().HasKey(m => new { m.GuildId, m.ChannelId, m.MessageId });
            modelBuilder.Entity<RoleMenuMessage>().HasMany(m => m.Roles)
                .WithOne(i => i.Message)
                .HasForeignKey(i => new { i.GuildId, i.ChannelId, i.MessageId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleMenuMessageRole>().HasKey(i => new { i.RoleId, i.GuildId, i.ChannelId, i.MessageId });

            modelBuilder.Entity<DailyUserActivity>().HasKey(i => new { i.UserId, i.Day });
            modelBuilder.Entity<DailyUserActivity>()
                .Property(i => i.Day)
                .HasConversion(new ValueConverter<DateTime, string>(
                    from => from.ToString("yyyy-MM-dd"),
                    to => DateTime.ParseExact(to, "yyyy-MM-dd", null)
                ));

            var ulongStringConverter = new ValueConverter<ulong, string>(
                from => from.ToString(),
                to => ulong.Parse(to)
            );

            modelBuilder
                .Entity<ScheduledTask>()
                .Property(i => i.When)
                .HasConversion(new ValueConverter<DateTimeOffset, string>(
                    from => from.ToUniversalTime().ToString("O"),
                    to => DateTimeOffset.ParseExact(to, "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                ));

            modelBuilder.Entity<VoteReplyRecord>().Property(i => i.GuildId).HasConversion(ulongStringConverter);
            modelBuilder.Entity<VoteReplyRecord>().Property(i => i.ChannelId).HasConversion(ulongStringConverter);
            modelBuilder.Entity<VoteReplyRecord>().Property(i => i.MessageId).HasConversion(ulongStringConverter);
            modelBuilder.Entity<VoteReplyRecord>().Property(i => i.ReplyId).HasConversion(ulongStringConverter);

            modelBuilder
                .Entity<VoteReplyRecord>()
                .HasKey(nameof(VoteReplyRecord.GuildId), nameof(VoteReplyRecord.ChannelId), nameof(VoteReplyRecord.MessageId));

            modelBuilder
                .Entity<VoteReplyRecord>()
                .HasAlternateKey(nameof(VoteReplyRecord.GuildId), nameof(VoteReplyRecord.ChannelId), nameof(VoteReplyRecord.ReplyId));
        }
    }
}
