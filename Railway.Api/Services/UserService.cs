using Microsoft.EntityFrameworkCore;
using Railway.Api.Auth;
using Railway.Api.Data;

namespace Railway.Api.Services;

public sealed class UserService(RailwayDbContext db, ICurrentUserService currentUser)
{
    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == currentUser.UserId);
        if (user is null)
        {
            return null;
        }

        return new UserProfile(user.Id.ToString(), user.FullName, user.Email, user.Phone, user.LoyaltyTier);
    }
}
