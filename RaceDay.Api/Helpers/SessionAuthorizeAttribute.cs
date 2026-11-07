using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Helpers;

// This is our "security guard". Put it above a controller or an endpoint.
//
//   [SessionAuthorize]               -> any logged-in user can enter
//   [SessionAuthorize("Organiser")]  -> only Organisers can enter
//   [SessionAuthorize("Participant")]-> only Participants can enter
//
// It runs BEFORE your endpoint code.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    // If no role is given, any logged-in user is allowed
    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Step 1: is anyone logged in? Look for the UserId in the session.
        var userId = context.HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId == null)
        {
            // 401 = "You are not logged in"
            context.Result = new UnauthorizedObjectResult(new { message = "Please log in first." });
            return;
        }

        // Step 2: if a role is needed, does the logged-in user have it?
        if (_requiredRole != null)
        {
            var role = context.HttpContext.Session.GetString(SessionKeys.Role);
            if (role != _requiredRole)
            {
                // 403 = "You are logged in, but you are not allowed here"
                context.Result = new ObjectResult(new { message = $"Only {_requiredRole}s can do this." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}