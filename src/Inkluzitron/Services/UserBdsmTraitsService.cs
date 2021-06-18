using Discord;
using Inkluzitron.Data;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class UserBdsmTraitsService
    {
        private DatabaseFactory DatabaseFactory { get; }
        private BdsmTestOrgSettings Settings { get; }
        private UsersService UsersService { get; }
        public BdsmTraitOperationCheckTranslations CheckTranslations { get; }

        public double StrongTraitThreshold => Settings.StrongTraitThreshold;
        public double WeakTraitThreshold => Settings.WeakTraitThreshold;

        public IMemoryCache Cache { get; }

        public UserBdsmTraitsService(DatabaseFactory databaseFactory, BdsmTestOrgSettings settings,
            BdsmTraitOperationCheckTranslations checkTranslations, IMemoryCache cache, UsersService usersService)
        {
            DatabaseFactory = databaseFactory;
            Settings = settings;
            CheckTranslations = checkTranslations;
            Cache = cache;
            UsersService = usersService;
        }

        public async Task<bool> TestExists(IUser user)
        {
            using var dbContext = DatabaseFactory.Create();

            return await dbContext.BdsmTestOrgResults
                .AsQueryable()
                .AnyAsync(r => r.UserId == user.Id);
        }

        public async Task<double> GetTraitScore(IUser user, BdsmTrait trait)
        {
            using var dbContext = DatabaseFactory.Create();

            var userTrait = (await dbContext.BdsmTestOrgResults
                .Include(r => r.Items)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync(r => r.UserId == user.Id))?
                .Items
                .Find(i => i.Trait == trait);

            return userTrait?.Score ?? 0;
        }

        public async Task<bool> HasStrongTrait(IUser user, BdsmTrait trait)
            => (await GetTraitScore(user, trait)) >= StrongTraitThreshold;

        public async Task<bool> IsDominant(IUser user)
            => (await HasStrongTrait(user, BdsmTrait.Dominant)) ||
                (await HasStrongTrait(user, BdsmTrait.MasterOrMistress));

        public async Task<bool> IsSubmissive(IUser user)
            => (await HasStrongTrait (user, BdsmTrait.Submissive)) ||
                (await HasStrongTrait (user, BdsmTrait.Slave));

        public async Task<bool> IsDominantOnly(IUser user)
            => (await IsDominant(user)) && !(await IsSubmissive(user));

        public async Task<bool> IsSubmissiveOnly(IUser user)
            => (await IsSubmissive(user)) && !(await IsDominant(user));

        static private string ToLastOperationCheckKey(IUser user)
            => $"{nameof(BdsmTraitOperationCheck)}_{user.Id}";

        public bool TryGetLastOperationCheck(IUser user, out BdsmTraitOperationCheck lastCheck)
            => Cache.TryGetValue(ToLastOperationCheckKey(user), out lastCheck);

        private void SetLastOperationCheck(IUser user, BdsmTraitOperationCheck lastCheck)
            => Cache.Set(ToLastOperationCheckKey(user), lastCheck);

        static private double Janchsinus(double score)
            => Math.Sin(Math.Abs(score) * Math.PI * 0.5);

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

            using var dbContext = DatabaseFactory.Create();

            var userDb = await dbContext.GetOrCreateUserEntityAsync(user);
            var targetDb = await dbContext.GetOrCreateUserEntityAsync(target);

            var check = new BdsmTraitOperationCheck(CheckTranslations, userDb.Gender, targetDb.Gender)
            {
                UserDisplayName = await UsersService.GetDisplayNameAsync(user),
                TargetDisplayName = await UsersService.GetDisplayNameAsync(target)
            };

            SetLastOperationCheck(user, check);

            if (user.Equals(target))
            {
                check.Result = BdsmTraitOperationCheckResult.Self;
                return check;
            }

            if (!userDb.HasGivenConsentTo(CommandConsent.BdsmImageCommands))
            {
                check.Result = BdsmTraitOperationCheckResult.UserDidNotConsent;
                return check;
            }

            if (!targetDb.HasGivenConsentTo(CommandConsent.BdsmImageCommands))
            {
                check.Result = BdsmTraitOperationCheckResult.TargetDidNotConsent;
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

            var userDominance = await GetTraitScore(user, BdsmTrait.Dominant);
            var userSubmissiveness = await GetTraitScore(user, BdsmTrait.Submissive);
            var targetDominance = await GetTraitScore(target, BdsmTrait.Dominant);
            var targetSubmissiveness = await GetTraitScore(target, BdsmTrait.Submissive);

            check.UserDominance = userDominance.ToIntPercentage();
            check.UserSubmissiveness = userSubmissiveness.ToIntPercentage();
            check.TargetDominance = targetDominance.ToIntPercentage();
            check.TargetSubmissiveness = targetSubmissiveness.ToIntPercentage();

            var score = 0.0;
            score += userDominance * targetSubmissiveness;
            score -= targetDominance * userSubmissiveness;

            if (score >= 0)
            {
                check.Result = BdsmTraitOperationCheckResult.InCompliance;
            }
            else
            {
                const int rollMaximum = 100;
                check.RequiredValue = (int)Math.Round(rollMaximum * Janchsinus(score));
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
