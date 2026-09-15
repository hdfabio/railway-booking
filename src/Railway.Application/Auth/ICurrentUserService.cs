using Railway.Contracts;
namespace Railway.Application.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}
