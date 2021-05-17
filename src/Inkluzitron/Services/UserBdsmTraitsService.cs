using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Inkluzitron.Services
{
    public class UserBdsmTraitsService : IDisposable
    {
        private BotDatabaseContext DbContext { get; }
        private DatabaseFactory DatabaseFactory { get; }
        private BdsmTestOrgSettings Settings { get; }

        public UserBdsmTraitsService(DatabaseFactory databaseFactory, BdsmTestOrgSettings settings)
        {
            DatabaseFactory = databaseFactory;
            Settings = settings;

            DbContext = DatabaseFactory.Create();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
        }

        public double GetTraitScore(IUser user, BdsmTraits trait)
        {
            var traitName = trait.GetType()
                .GetMember(trait.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()
                .GetName();

            var userTrait = DbContext.BdsmTestOrgQuizResults
                .Include(r => r.Items)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefault(r => r.SubmittedById == user.Id)?.Items
                .OfType<QuizDoubleItem>()
                .FirstOrDefault(i => i.Key == traitName);

            return userTrait?.Value ?? 0;
        }

        public bool HasStrongTrait(IUser user, BdsmTraits trait)
            => GetTraitScore(user, trait) >= Settings.StrongTraitThreshold;

        public bool IsDominant(IUser user)
            => HasStrongTrait(user, BdsmTraits.Dominant) ||
                HasStrongTrait(user, BdsmTraits.MasterOrMistress);

        public bool IsSubmissive(IUser user)
            => HasStrongTrait(user, BdsmTraits.Submissive) ||
                HasStrongTrait(user, BdsmTraits.Slave);

        public bool IsDominantOnly(IUser user)
            => IsDominant(user) && !IsSubmissive(user);

        public bool IsSubmissiveOnly(IUser user)
            => IsSubmissive(user) && !IsDominant(user);
    }
}
