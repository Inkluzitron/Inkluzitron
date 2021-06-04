using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
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
        static public async Task<User> GetUserEntityAsync(this BotDatabaseContext context, IUser user)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

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

        // Quick fix for user entity manipulation outside of this db context
        // TODO: Fix properly
        static public async Task<User> GetOrCreateUserEntityAsync(this BotDatabaseContext context, IUser user)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var userEntity = await context.GetUserEntityAsync(user);

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
    }
}
