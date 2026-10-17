using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters
{
    public class SessionAuthAttribute : Attribute, IAuthorizationFilter
    {
        // FIX: Changed from int? to int, with a default of 0
        public int RequiredRoleId { get; set; } = 0;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var userId = session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            // FIX: Check if it is greater than 0 instead of checking for null
            if (RequiredRoleId > 0)
            {
                var roleId = session.GetInt32("RoleId");
                if (roleId != RequiredRoleId)
                {
                    context.Result = new StatusCodeResult(403);
                }
            }
        }
    }
}