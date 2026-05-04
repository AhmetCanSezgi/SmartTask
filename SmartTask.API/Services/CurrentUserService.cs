using SmartTask.Application.Common.Interfaces;
using System.Security.Claims;

namespace SmartTask.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
        => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var claim = _accessor.HttpContext?
                .User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim) : null;
        }
    }

    public bool IsAuthenticated
        => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
