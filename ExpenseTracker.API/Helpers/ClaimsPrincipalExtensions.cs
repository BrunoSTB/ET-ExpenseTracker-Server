using System.Security.Claims;

namespace ExpenseTracker.API.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal principal)
        {
            return long.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}
