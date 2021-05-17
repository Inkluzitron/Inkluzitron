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
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class UserBdsmTraitsService
    {
        private DatabaseFactory DatabaseFactory { get; }
        private BdsmTestOrgSettings Settings { get; }

        public double StrongTraitThreshold => Settings.StrongTraitThreshold;

        public UserBdsmTraitsService(DatabaseFactory databaseFactory, BdsmTestOrgSettings settings)
        {
            DatabaseFactory = databaseFactory;
            Settings = settings;
        }

        public async Task<bool> TestExists(IUser user)
        {
            using var dbContext = DatabaseFactory.Create();

            return await dbContext.BdsmTestOrgQuizResults
                .AsAsyncEnumerable()
                .AnyAsync(r => r.SubmittedById == user.Id);
        }

        public async Task<double> GetTraitScore(IUser user, BdsmTraits trait)
        {
            var traitName = trait.GetType()
                .GetMember(trait.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()
                .GetName();

            using var dbContext = DatabaseFactory.Create();

            var userTrait = (await dbContext.BdsmTestOrgQuizResults
                .Include(r => r.Items)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync(r => r.SubmittedById == user.Id))?
                .Items.OfType<QuizDoubleItem>()
                .FirstOrDefault(i => i.Key == traitName);

            return userTrait?.Value ?? 0;
        }

        public async Task<bool> HasStrongTrait(IUser user, BdsmTraits trait)
            => (await GetTraitScore(user, trait)) >= StrongTraitThreshold;

        public async Task<bool> IsDominant(IUser user)
            => (await HasStrongTrait(user, BdsmTraits.Dominant)) ||
                (await HasStrongTrait(user, BdsmTraits.MasterOrMistress));

        public async Task<bool> IsSubmissive(IUser user)
            => (await HasStrongTrait (user, BdsmTraits.Submissive)) ||
                (await HasStrongTrait (user, BdsmTraits.Slave));

        public async Task<bool> IsDominantOnly(IUser user)
            => (await IsDominant(user)) && !(await IsSubmissive(user));

        public async Task<bool> IsSubmissiveOnly(IUser user)
            => (await IsSubmissive(user)) && !(await IsDominant(user));
    }
}
