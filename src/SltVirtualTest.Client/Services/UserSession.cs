using SltVirtualTest.Shared.Dtos;

namespace SltVirtualTest.Client.Services;

public class UserSession
{
    public UserDto? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public void SetUser(UserDto user) => CurrentUser = user;

    public void Clear() => CurrentUser = null;
}
