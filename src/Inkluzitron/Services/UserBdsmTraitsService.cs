using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
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

        public Task<BdsmTestOrgResult> FindTestResultAsync(IUser user)
        {
            using var dbContext = DatabaseFactory.Create();

            return dbContext.BdsmTestOrgResults.AsQueryable()
                .Include(r => r.Items)
                .Include(r => r.User)
                .Where(r => r.UserId == user.Id)
                .FirstOrDefaultAsync();
        }

        public bool HasStrongTrait(BdsmTestOrgResult testResult, BdsmTrait trait)
            => testResult[trait] >= StrongTraitThreshold;

        public bool IsDominant(BdsmTestOrgResult userTestResult)
            => HasStrongTrait(userTestResult, BdsmTrait.Dominant) ||
               HasStrongTrait(userTestResult, BdsmTrait.MasterOrMistress);

        public bool IsSubmissive(BdsmTestOrgResult userTestResult)
            => HasStrongTrait(userTestResult, BdsmTrait.Submissive) ||
               HasStrongTrait(userTestResult, BdsmTrait.Slave);

        public bool IsDominantOnly(BdsmTestOrgResult userTestResult)
            => IsDominant(userTestResult) && !IsSubmissive(userTestResult);

        public bool IsSubmissiveOnly(BdsmTestOrgResult userTestResult)
            => IsSubmissive(userTestResult) && !IsDominant(userTestResult);

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
        public async Task<BdsmTraitOperationCheck> CheckTraitOperationAsync(IUser user, IUser target)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            using var dbContext = DatabaseFactory.Create();

            var userDb = await dbContext.GetOrCreateUserEntityAsync(user);
            var targetDb = await dbContext.GetOrCreateUserEntityAsync(target);

            var userTest = await FindTestResultAsync(user);
            var targetTest = await FindTestResultAsync(target);
            var userPoints = await dbContext.DailyUsersActivities.AsQueryable()
                .Where(a => a.UserId == user.Id)
                .SumAsync(a => a.Points);

            var check = new BdsmTraitOperationCheck(Settings, CheckTranslations, userDb.Gender, targetDb.Gender)
            {
                UserDisplayName = Format.Sanitize(await UsersService.GetDisplayNameAsync(user)),
                TargetDisplayName = Format.Sanitize(await UsersService.GetDisplayNameAsync(target))
            };

            CalculateTraitOperation(check, userTest, targetTest, userPoints);
            SetLastOperationCheck(user, check);
            return check;
        }

        static internal BdsmTraitOperationFactor AssessPairwiseTraitFactor(BdsmTestOrgResult user, BdsmTestOrgResult target, BdsmTrait protagonistTrait, BdsmTrait antagonistTrait)
        {
            var userProtagony = user[protagonistTrait];
            var userAntagony = user[antagonistTrait];
            var targetProtagony = target[protagonistTrait];
            var targetAntagony = target[antagonistTrait];

            var score = 0.0;
            score += userProtagony * targetAntagony;
            score -= targetProtagony * userAntagony;

            var ptdn = protagonistTrait.GetDisplayName();
            var atdn = antagonistTrait.GetDisplayName();

            var sum = Math.Max(userProtagony + targetAntagony, userAntagony + targetProtagony);
            var weight = Math.Min(1, sum);

            return new BdsmTraitOperationFactor
            {
                Score = score,
                Weight = double.Epsilon + weight,
                Values =
                {
                    { ptdn, (userProtagony.ToIntPercentage().ToString(),  targetProtagony.ToIntPercentage().ToString())},
                    { atdn, (userAntagony.ToIntPercentage().ToString(),  targetAntagony.ToIntPercentage().ToString())}
                }
            };
        }

        static internal void CalculateTraitOperation(BdsmTraitOperationCheck check, BdsmTestOrgResult testOfUser, BdsmTestOrgResult testOfTarget, long userPoints)
        {
            if (testOfUser == null)
            {
                check.Result = BdsmTraitOperationCheckResult.UserHasNoTest;
                return;
            }

            if (testOfTarget == null)
            {
                check.Result = BdsmTraitOperationCheckResult.TargetHasNoTest;
                return;
            }

            if (testOfUser.Id == testOfTarget.Id)
            {
                check.Result = BdsmTraitOperationCheckResult.Self;
                return;
            }

            if (!testOfUser.User.HasGivenConsentTo(CommandConsent.BdsmImageCommands))
            {
                check.Result = BdsmTraitOperationCheckResult.UserDidNotConsent;
                return;
            }

            if (!testOfTarget.User.HasGivenConsentTo(CommandConsent.BdsmImageCommands))
            {
                check.Result = BdsmTraitOperationCheckResult.TargetDidNotConsent;
                return;
            }

            if (userPoints < 0)
            {
                check.Result = BdsmTraitOperationCheckResult.UserNegativePoints;
                return;
            }

            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.Dominant, BdsmTrait.Submissive));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.BratTamer, BdsmTrait.Brat));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.DaddyOrMommy, BdsmTrait.BoyOrGirl));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.Degrader, BdsmTrait.Degradee));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.Sadist, BdsmTrait.Masochist));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.MasterOrMistress, BdsmTrait.Slave));
            check.Factors.Add(AssessPairwiseTraitFactor(testOfUser, testOfTarget, BdsmTrait.PrimalHunter, BdsmTrait.PrimalPrey));
            check.Compute();
        }
    }
}
