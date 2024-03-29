﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TASagentTwitchBot.TTTASDemo.Database;

#nullable disable

namespace TASagentTwitchBot.TTTASDemo.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.7");

            modelBuilder.Entity("TASagentTwitchBot.Core.Database.CustomTextCommand", b =>
                {
                    b.Property<int>("CustomTextCommandId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Command")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Shown")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("CustomTextCommandId");

                    b.ToTable("CustomTextCommands");
                });

            modelBuilder.Entity("TASagentTwitchBot.Core.Database.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AuthorizationLevel")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Color")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("FirstFollowed")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("FirstSeen")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastSuccessfulTTS")
                        .HasColumnType("TEXT");

                    b.Property<string>("TTSEffectsChain")
                        .HasColumnType("TEXT");

                    b.Property<int>("TTSPitchPreference")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TTSSpeedPreference")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TTSVoicePreference")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TwitchUserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TwitchUserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
