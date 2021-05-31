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

        public async Task<User> GetUserDbEntityAsync(IUser user)
        {
            var displayName = user.GetDisplayName();
            var userEntity = await DbContext.Users.AsQueryable()
                .FirstOrDefaultAsync(o => o.Id == user.Id);

            // Update cached displayname
            if (userEntity != null && userEntity.Name != displayName)
            {
                userEntity.Name = displayName;
                await DbContext.SaveChangesAsync();
            }

            return userEntity;
        }

        public async Task<User> GetOrCreateUserDbEntityAsync(IUser user)
        {
            var userEntity = await GetUserDbEntityAsync(user);

            if (userEntity != null)
                return userEntity;

            userEntity = new User()
            {
                Id = user.Id,
                Name = user.GetDisplayName()
            };

            await DbContext.AddAsync(userEntity);
            await DbContext.SaveChangesAsync();

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
