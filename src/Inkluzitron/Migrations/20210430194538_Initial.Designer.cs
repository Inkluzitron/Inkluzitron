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
    [Migration("20210430194538_Initial")]
    partial class Initial
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

            modelBuilder.Entity("Inkluzitron.Data.QuizResult", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}
