using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace EmpPortal.Authorize
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly int _requiredUserLevel;
        public SessionAuthorizeAttribute(int requiredUserLevel = 0)
        {
            _requiredUserLevel = requiredUserLevel;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var username = session.GetString("USER_NAME");
            var userLevelStr = session.GetString("USER_LEVEL");

            if (string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Login" },
                    { "action", "Index" }
                });
                return;
            }
            if (_requiredUserLevel != 0) 
            {
                if (!int.TryParse(userLevelStr, out var userLevel) || userLevel != _requiredUserLevel)
                {
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary
                    {
                        { "controller", "AccessedError" },
                        { "action", "Index" }
                    });
                    return;
                }
            }
            //How to call[SessionAuthorize(1)]
        }
    }
}
