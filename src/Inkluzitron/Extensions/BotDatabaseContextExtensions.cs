﻿using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class BotDatabaseContextExtensions
    {
        // Quick fix for user entity manipulation outside of this db context
        // TODO: Fix properly
        static public async Task<User> GetUserEntityAsync(this BotDatabaseContext context, IUser user,
            Func<IQueryable<User>, IQueryable<User>> customQuery = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (customQuery == null)
                customQuery = q => q;

            // Update cached displayname
            var displayName = user.Username;

            var result = await Patiently.HandleDbConcurrency(async () => {
                var userEntity = await customQuery(context.Users.AsQueryable()).FirstOrDefaultAsync(o => o.Id == user.Id);

                if (userEntity != null && userEntity.Name != displayName)
                {
                    userEntity.Name = displayName;
                    await context.SaveChangesAsync();
                }

                return userEntity;
            });

            return result;
        }

        // Quick fix for user entity manipulation outside of this db context
        // TODO: Fix properly
        static public async Task<User> GetOrCreateUserEntityAsync(this BotDatabaseContext context, IUser user,
            Func<IQueryable<User>, IQueryable<User>> customQuery = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var userEntity = await context.GetUserEntityAsync(user, customQuery);

            if (userEntity != null)
                return userEntity;

            userEntity = new User()
            {
                Id = user.Id,
                Name = user.Username
            };

            // Do not store bot data
            if (user.IsBot)
                return userEntity;

            await context.AddAsync(userEntity);
            await context.SaveChangesAsync();

            return userEntity;
        }

        static public async Task UpdateCommandConsentAsync(this BotDatabaseContext context, IUser user, Func<CommandConsent, CommandConsent> consentUpdaterFunc)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await GetOrCreateUserEntityAsync(context, user);
                userEntity.CommandConsents = consentUpdaterFunc(userEntity.CommandConsents);
                await context.SaveChangesAsync();
            });
        }

        static public Task<DailyUserActivity> GetTodayUserActivityAsync(this BotDatabaseContext context, IUser user)
            => GetUserActivityAsync(context, user, DateTime.Now);

        static public async Task<DailyUserActivity> GetUserActivityAsync(this BotDatabaseContext context, IUser user, DateTime day)
        {
            day = day.Date;

            var todayActivity = await context.DailyUsersActivities.AsQueryable()
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Day == day);

            if (todayActivity == null)
            {
                todayActivity = new DailyUserActivity()
                {
                    User = await context.GetOrCreateUserEntityAsync(user)
                };

                await context.DailyUsersActivities.AddAsync(todayActivity);
            }

            return todayActivity;
        }
    }
}
