using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class UsersService
    {
        private DatabaseFactory DatabaseFactory { get; }
        private BotDatabaseContext DbContext { get; set; }

        public UsersService(DatabaseFactory databaseFactory)
        {
            DatabaseFactory = databaseFactory;
            DbContext = DatabaseFactory.Create();
        }

        public Task<User> GetUserDbEntityAsync(IUser user)
            => GetUserDbEntityAsync(user, DbContext);

        // Quick fix for user entity manipulation outside of this db context
        // TODO: Fix properly
        public async Task<User> GetUserDbEntityAsync(IUser user, BotDatabaseContext context)
        {
            var displayName = user.GetDisplayName();
            var userEntity = await context.Users.AsQueryable()
                .FirstOrDefaultAsync(o => o.Id == user.Id);

            // Update cached displayname
            if (userEntity != null && userEntity.Name != displayName)
            {
                userEntity.Name = displayName;
                await context.SaveChangesAsync();
            }

            return userEntity;
        }

        public Task<User> GetOrCreateUserDbEntityAsync(IUser user)
            => GetOrCreateUserDbEntityAsync(user, DbContext);

        // Quick fix for user entity manipulation outside of this db context
        // TODO: Fix properly
        public async Task<User> GetOrCreateUserDbEntityAsync(IUser user, BotDatabaseContext context)
        {
            var userEntity = await GetUserDbEntityAsync(user, context);

            if (userEntity != null)
                return userEntity;

            userEntity = new User()
            {
                Id = user.Id,
                Name = user.GetDisplayName()
            };

            await context.AddAsync(userEntity);
            await context.SaveChangesAsync();

            return userEntity;
        }

        public async Task SetUserGenderAsync(IUser user, Gender gender)
        {
            var userEntity = await GetOrCreateUserDbEntityAsync(user);

            userEntity.Gender = gender;

            await DbContext.SaveChangesAsync();
        }
    }
}
