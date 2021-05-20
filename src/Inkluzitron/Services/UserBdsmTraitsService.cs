using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        public BdsmTraitOperationCheckTranslations CheckTranslations { get; }

        public double StrongTraitThreshold => Settings.StrongTraitThreshold;

        public IMemoryCache Cache { get; }

        public UserBdsmTraitsService(DatabaseFactory databaseFactory, BdsmTestOrgSettings settings, BdsmTraitOperationCheckTranslations checkTranslations, IMemoryCache cache)
        {
            DatabaseFactory = databaseFactory;
            Settings = settings;
            CheckTranslations = checkTranslations;
            Cache = cache;
        }

        public async Task<bool> TestExists(IUser user)
        {
            using var dbContext = DatabaseFactory.Create();

            return await dbContext.BdsmTestOrgQuizResults
                .AsQueryable()
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

        static private int ToIntPercentage(double x)
            => (int)Math.Round(100 * x);

        static private string ToLastOperationCheckKey(IUser user)
            => $"{nameof(BdsmTraitOperationCheck)}_{user.Id}";

        public bool TryGetLastOperationCheck(IUser user, out BdsmTraitOperationCheck lastCheck)
            => Cache.TryGetValue(ToLastOperationCheckKey(user), out lastCheck);

        private void SetLastOperationCheck(IUser user, BdsmTraitOperationCheck lastCheck)
            => Cache.Set(ToLastOperationCheckKey(user), lastCheck);

        /// <summary>
        /// Check based on BDSM results.
        /// This check probabilistically prevents sub users from whipping dom users.
        /// </summary>
        /// <param name="user">Initiator (whipping user)</param>
        /// <param name="target">Target (user to be whipped)</param>
        /// <returns>Detailed results of the check.</returns>
        public async Task<BdsmTraitOperationCheck> CheckDomSubOperationAsync(IUser user, IUser target)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var check = new BdsmTraitOperationCheck(CheckTranslations) { User = user, Target = target };
            SetLastOperationCheck(user, check);

            if (user.Equals(target))
            {
                check.Result = BdsmTraitOperationCheckResult.Self;
                return check;
            }

            if (!await TestExists(user))
            {
                check.Result = BdsmTraitOperationCheckResult.UserHasNoTest;
                return check;
            }

            if (!await TestExists(target))
            {
                check.Result = BdsmTraitOperationCheckResult.TargetHasNoTest;
                return check;
            }

            var userDominance = await GetTraitScore(user, BdsmTraits.Dominant);
            var userSubmissiveness = await GetTraitScore(user, BdsmTraits.Submissive);
            var targetDominance = await GetTraitScore(target, BdsmTraits.Dominant);
            var targetSubmissiveness = await GetTraitScore(target, BdsmTraits.Submissive);

            check.UserDominance = ToIntPercentage(userDominance);
            check.UserSubmissiveness = ToIntPercentage(userSubmissiveness);
            check.TargetDominance = ToIntPercentage(targetDominance);
            check.TargetSubmissiveness = ToIntPercentage(targetSubmissiveness);

            var score = 0.0;

            var domDiff = Math.Abs(targetDominance - userDominance);
            if (targetDominance > StrongTraitThreshold && targetDominance > userDominance)
                score -= domDiff;
            else if (userDominance > StrongTraitThreshold && userDominance > targetDominance)
                score += domDiff;

            var subDiff = Math.Abs(targetSubmissiveness - userSubmissiveness);
            if (targetSubmissiveness > StrongTraitThreshold && targetSubmissiveness > userSubmissiveness) score += subDiff;
            else if (userSubmissiveness > StrongTraitThreshold && userSubmissiveness > targetSubmissiveness) score -= subDiff;

            if (score >= 0)
            {
                check.Result = BdsmTraitOperationCheckResult.InCompliance;
            }
            else
            {
                const int rollMaximum = 100;
                score /= -2.0;
                var easeOutCubic = 1 - Math.Pow(1.0 - score, 3);
                check.RequiredValue = (int)Math.Round(100 * easeOutCubic);
                check.RolledValue = ThreadSafeRandom.Next(1, rollMaximum + 1);
                check.RollMaximum = rollMaximum;

                if (check.RolledValue >= check.RequiredValue)
                    check.Result = BdsmTraitOperationCheckResult.RollSucceeded;
                else
                    check.Result = BdsmTraitOperationCheckResult.RollFailed;
            }

            return check;
        }
    }
}
