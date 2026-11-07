using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Helpers;

// A small shortcut so we can write:  HttpContext.Session.GetCurrentUserId()
public static class SessionHelper
{
    // Returns the logged-in user's id.
    // Only call this AFTER [SessionAuthorize] has checked the user is logged in.
    public static int GetCurrentUserId(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId) ?? 0;
    }
}