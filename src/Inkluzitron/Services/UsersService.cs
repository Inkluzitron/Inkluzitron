using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class UsersService
    {
        private DatabaseFactory DatabaseFactory { get; }

        public UsersService(DatabaseFactory databaseFactory)
        {
            DatabaseFactory = databaseFactory;
        }

        public async Task<User> GetUserDbEntityAsync(IUser user)
        {
            using var context = DatabaseFactory.Create();

            var displayName = user.GetDisplayName();
            var userEntity = await context.Users.AsQueryable()
                .FirstOrDefaultAsync(o => o.Id == user.Id);

            if (userEntity != null)
            {
                // Update cached displayname
                if (userEntity.Name != displayName)
                {
                    userEntity.Name = displayName;
                    await context.SaveChangesAsync();
                }

                return userEntity;
            }

            userEntity = new User()
            {
                Id = user.Id,
                Name = displayName
            };

            await context.AddAsync(userEntity);
            await context.SaveChangesAsync();

            return userEntity;
        }
    }
}
