﻿// <auto-generated />
using System;
using Inkluzitron.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inkluzitron.Migrations
{
    [DbContext(typeof(BotDatabaseContext))]
    [Migration("20210722133740_SupportForManualInvites")]
    partial class SupportForManualInvites
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("Inkluzitron.Data.Entities.BdsmTestOrgItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Score")
                        .HasColumnType("REAL");

                    b.Property<string>("Trait")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("BdsmTestOrgItems");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.BdsmTestOrgResult", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("SubmittedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Link")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("BdsmTestOrgResults");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.DailyUserActivity", b =>
                {
                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Day")
                        .HasColumnType("TEXT");

                    b.Property<long>("MessagesSent")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Points")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ReactionsAdded")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.HasKey("UserId", "Day");

                    b.ToTable("DailyUsersActivities");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.Invite", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("GeneratedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GeneratedByUserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("InviteLink")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("UsedByUserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("GeneratedByUserId");

                    b.HasIndex("InviteLink")
                        .IsUnique();

                    b.HasIndex("UsedByUserId");

                    b.ToTable("Invites");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RicePurityResult", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("Score")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("SubmittedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("RicePurityResults");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RoleMenuMessage", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("CanSelectMultiple")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "ChannelId", "MessageId");

                    b.ToTable("RoleMenuMessages");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RoleMenuMessageRole", b =>
                {
                    b.Property<ulong>("RoleId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("Emote")
                        .HasColumnType("TEXT");

                    b.Property<string>("Mention")
                        .HasColumnType("TEXT");

                    b.HasKey("RoleId", "GuildId", "ChannelId", "MessageId");

                    b.HasIndex("GuildId", "ChannelId", "MessageId");

                    b.ToTable("RoleMenuMessageRoles");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.ScheduledTask", b =>
                {
                    b.Property<long>("ScheduledTaskId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Data")
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("FailCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Tag")
                        .HasColumnType("TEXT");

                    b.Property<string>("When")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ScheduledTaskId");

                    b.ToTable("ScheduledTasks");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.User", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CommandConsents")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("KisLastCheck")
                        .HasColumnType("TEXT");

                    b.Property<string>("KisNickname")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastMessagePointsIncrement")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastReactionPointsIncrement")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Pronouns")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.HasIndex("KisNickname")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.VoteReplyRecord", b =>
                {
                    b.Property<string>("GuildId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ChannelId")
                        .HasColumnType("TEXT");

                    b.Property<string>("MessageId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RecordCreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReplyId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "ChannelId", "MessageId");

                    b.HasAlternateKey("GuildId", "ChannelId", "ReplyId");

                    b.ToTable("VoteReplyRecords");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.BdsmTestOrgItem", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.BdsmTestOrgResult", "Parent")
                        .WithMany("Items")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.BdsmTestOrgResult", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.DailyUserActivity", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.User", "User")
                        .WithMany("DailyActivity")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.Invite", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.User", "GeneratedBy")
                        .WithMany()
                        .HasForeignKey("GeneratedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Inkluzitron.Data.Entities.User", "UsedBy")
                        .WithMany()
                        .HasForeignKey("UsedByUserId");

                    b.Navigation("GeneratedBy");

                    b.Navigation("UsedBy");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RicePurityResult", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RoleMenuMessageRole", b =>
                {
                    b.HasOne("Inkluzitron.Data.Entities.RoleMenuMessage", "Message")
                        .WithMany("Roles")
                        .HasForeignKey("GuildId", "ChannelId", "MessageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Message");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.BdsmTestOrgResult", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.RoleMenuMessage", b =>
                {
                    b.Navigation("Roles");
                });

            modelBuilder.Entity("Inkluzitron.Data.Entities.User", b =>
                {
                    b.Navigation("DailyActivity");
                });
#pragma warning restore 612, 618
        }
    }
}
