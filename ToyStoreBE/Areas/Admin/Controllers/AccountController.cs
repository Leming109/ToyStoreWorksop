using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ToyStoreBE.Helpers;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // GET: Admin/Account/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            // Nếu đã đăng nhập bằng tài khoản quản trị
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name
                );

                if (currentUser != null &&
                    (currentUser.UserRole == "A" ||
                     currentUser.UserRole == "M"))
                {
                    if (currentUser.IsLocked)
                    {
                        FormsAuthentication.SignOut();
                    }
                    else
                    {
                        return RedirectToAction(
                            "Index",
                            "Admin",
                            new { area = "Admin" }
                        );
                    }
                }
            }

            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(
            AdminLoginViewModel model,
            string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = model.Username.Trim();

            // Chỉ lấy tài khoản SuperAdmin hoặc Moderator
            var user = db.Users.FirstOrDefault(u =>
                u.Username == username &&
                (u.UserRole == "A" || u.UserRole == "M")
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Tên tài khoản hoặc mật khẩu không chính xác."
                );

                return View(model);
            }

            string storedPassword = (user.Password ?? "").TrimEnd();

            bool isValid = false;

            if (storedPassword.StartsWith(
                "P2$",
                StringComparison.Ordinal))
            {
                isValid = CompactPasswordHasher.VerifyPassword(
                    model.Password,
                    storedPassword
                );
            }
            else
            {
                // Hỗ trợ tài khoản cũ lưu mật khẩu dạng chữ thường
                isValid = storedPassword == model.Password;

                if (isValid)
                {
                    string hashedPassword =
                        CompactPasswordHasher.HashPassword(
                            model.Password
                        );

                    if (hashedPassword.Length <= 50)
                    {
                        user.Password = hashedPassword;
                        db.SaveChanges();
                    }
                }
            }

            if (!isValid)
            {
                ModelState.AddModelError(
                    "",
                    "Tên tài khoản hoặc mật khẩu không chính xác."
                );

                return View(model);
            }

            // Kiểm tra trạng thái khóa sau khi xác thực mật khẩu
            if (user.IsLocked)
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản quản trị của bạn đã bị khóa. Vui lòng liên hệ SuperAdmin."
                );

                return View(model);
            }

            // Tạo cookie đăng nhập
            FormsAuthentication.SetAuthCookie(
                user.Username.TrimEnd(),
                model.RememberMe
            );

            // Chỉ chấp nhận đường dẫn quay lại nội bộ
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Admin",
                new { area = "Admin" }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account",
                new { area = "Admin" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
