
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using ToyStoreBE.Models;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public abstract class AdminBaseController : Controller
    {
        // Mặc định chỉ yêu cầu quyền quản trị A hoặc M
        protected virtual bool SuperAdminOnly
        {
            get { return false; }
        }

        // Cho phép Moderator chỉ xem giao diện Index bị giới hạn
        protected virtual bool AllowModeratorToViewIndexOnly
        {
            get { return false; }
        }

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            var identity = filterContext.HttpContext.User.Identity;

            // 1. Kiểm tra đăng nhập
            if (identity == null || !identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        area = "Admin",
                        controller = "Account",
                        action = "Login",
                        returnUrl = filterContext.HttpContext.Request.RawUrl
                    })
                );

                return;
            }

            User account;

            // 2. Kiểm tra tài khoản trong database
            using (var db = new ToyStoreBEEntities())
            {
                string username = identity.Name;

                account = db.Users.FirstOrDefault(
                    u => u.Username == username
                );
            }

            // 3. Tài khoản không còn tồn tại
            if (account == null)
            {
                FormsAuthentication.SignOut();
                Session.Clear();

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        area = "Admin",
                        controller = "Account",
                        action = "Login"
                    })
                );

                return;
            }

            // 4. Tài khoản bị khóa
            if (account.IsLocked)
            {
                FormsAuthentication.SignOut();
                Session.Clear();

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        area = "Admin",
                        controller = "Account",
                        action = "Login"
                    })
                );

                return;
            }

            string role = (account.UserRole ?? "").Trim();

            // 5. Chỉ A và M được vào khu vực Admin
            if (role != "A" && role != "M")
            {
                filterContext.Result = new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "Bạn không có quyền truy cập khu vực quản trị."
                );

                return;
            }

            string controllerName = Convert.ToString(
                filterContext.RouteData.Values["controller"]
            );

            string actionName = Convert.ToString(
                filterContext.RouteData.Values["action"]
            );

            // 6. Xác định Moderator có đang mở Users/Index không
            bool isUsersIndex =
                string.Equals(
                    controllerName,
                    "Users",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                string.Equals(
                    actionName,
                    "Index",
                    StringComparison.OrdinalIgnoreCase
                );

            // 7. Kiểm tra quyền SuperAdmin
            if (SuperAdminOnly && role != "A")
            {
                // Ngoại lệ duy nhất:
                // Moderator được mở Users/Index để xem thông báo không có quyền.
                bool allowRestrictedIndex =
                    role == "M" &&
                    AllowModeratorToViewIndexOnly &&
                    isUsersIndex;

                if (!allowRestrictedIndex)
                {
                    filterContext.Result = new HttpStatusCodeResult(
                        HttpStatusCode.Forbidden,
                        "Chỉ SuperAdmin mới có quyền thực hiện chức năng này."
                    );

                    return;
                }
            }

            // 8. Truyền thông tin tài khoản sang View
            ViewBag.AdminUsername = account.Username.TrimEnd();
            ViewBag.AdminRole = role;

            ViewBag.CanManageUsers = role == "A";

            ViewBag.ShowRestrictedUsersOverlay =
                role == "M" &&
                isUsersIndex &&
                AllowModeratorToViewIndexOnly;

            base.OnActionExecuting(filterContext);
        }
    }
}
