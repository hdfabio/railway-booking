using Railway.Contracts;
using Microsoft.EntityFrameworkCore;
using Railway.Application.Auth;
using Railway.Persistence;

namespace Railway.Application.Services;

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
