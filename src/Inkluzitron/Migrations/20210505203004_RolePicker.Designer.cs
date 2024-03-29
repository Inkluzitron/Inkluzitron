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
    [Migration("20210505203004_RolePicker")]
    partial class RolePicker
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.5");

            modelBuilder.Entity("Inkluzitron.Data.QuizItem", b =>
                {
                    b.Property<ulong>("ItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("ParentResultId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ItemId");

                    b.HasIndex("ParentResultId");

                    b.ToTable("QuizItems");

                    b.HasDiscriminator<string>("Discriminator").HasValue("QuizItem");
                });

            modelBuilder.Entity("Inkluzitron.Data.QuizResult", b =>
                {
                    b.Property<ulong>("ResultId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("SubmittedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("SubmittedById")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SubmittedByName")
                        .HasColumnType("TEXT");

                    b.HasKey("ResultId");

                    b.ToTable("QuizResults");

                    b.HasDiscriminator<string>("Discriminator").HasValue("QuizResult");
                });

            modelBuilder.Entity("Inkluzitron.Data.RolePickerMessage", b =>
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

                    b.ToTable("UserRoleMessage");
                });

            modelBuilder.Entity("Inkluzitron.Data.RolePickerMessageRole", b =>
                {
                    b.Property<ulong>("Id")
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

                    b.HasKey("Id", "GuildId", "ChannelId", "MessageId");

                    b.HasIndex("GuildId", "ChannelId", "MessageId");

                    b.ToTable("UserRoleMessageItem");
                });

            modelBuilder.Entity("Inkluzitron.Data.QuizDoubleItem", b =>
                {
                    b.HasBaseType("Inkluzitron.Data.QuizItem");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasDiscriminator().HasValue("QuizDoubleItem");
                });

            modelBuilder.Entity("Inkluzitron.Data.BdsmTestOrgQuizResult", b =>
                {
                    b.HasBaseType("Inkluzitron.Data.QuizResult");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("BdsmTestOrgQuizResult");
                });

            modelBuilder.Entity("Inkluzitron.Data.QuizItem", b =>
                {
                    b.HasOne("Inkluzitron.Data.QuizResult", "Parent")
                        .WithMany("Items")
                        .HasForeignKey("ParentResultId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Inkluzitron.Data.RolePickerMessageRole", b =>
                {
                    b.HasOne("Inkluzitron.Data.RolePickerMessage", "Message")
                        .WithMany("Roles")
                        .HasForeignKey("GuildId", "ChannelId", "MessageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Message");
                });

            modelBuilder.Entity("Inkluzitron.Data.QuizResult", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("Inkluzitron.Data.RolePickerMessage", b =>
                {
                    b.Navigation("Roles");
                });
#pragma warning restore 612, 618
        }
    }
}
