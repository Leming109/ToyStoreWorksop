using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity.Validation;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;
using ToyStoreBE.Helpers;

namespace ToyStoreBE.Controllers
{
    public class LoginController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // =========================================
        // GET: Login/Login
        // =========================================
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // =========================================
        // POST: Login/Login
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            // Chỉ cho phép tài khoản khách hàng đăng nhập
            var user = db.Users.SingleOrDefault(u =>
                u.Username == username &&
                u.UserRole == "C");

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
                // Kiểm tra mật khẩu đã được hash
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
                    // Chuyển mật khẩu cũ sang dạng hash
                    user.Password =
                        CompactPasswordHasher.HashPassword(
                            model.Password
                        );

                    db.SaveChanges();
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

            // Tạo cookie đăng nhập
            System.Web.Security.FormsAuthentication.SetAuthCookie(
                user.Username,
                model.RememberMe
            );

            return RedirectToAction("Index", "Home");
        }

        // =========================================
        // GET: Login/NewAccount
        // =========================================
        [HttpGet]
        public ActionResult NewAccount()
        {
            return View();
        }

        // =========================================
        // POST: Login/NewAccount
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewAccount(NewAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Username == null ||
                model.CustomerName == null ||
                model.CustomerEmail == null ||
                model.CustomerAddress == null ||
                model.CustomerPhone == null)
            {
                return View(model);
            }

            string username = model.Username.Trim();

            // Kiểm tra tên tài khoản đã tồn tại
            if (db.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên tài khoản đã tồn tại."
                );

                return View(model);
            }

            // Kiểm tra thông tin khách hàng đã tồn tại
            if (db.Customers.Any(c => c.Username == username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tài khoản này đã được đăng ký."
                );

                return View(model);
            }

            // Tạo tài khoản User
            var user = new User
            {
                Username = username,

                Password =
                    CompactPasswordHasher.HashPassword(
                        model.Password
                    ),

                UserRole = "C"
            };

            // Tạo thông tin Customer
            var customer = new Customer
            {
                Username = username,
                CustomerName = model.CustomerName.Trim(),
                CustomerPhone = model.CustomerPhone.Trim(),
                CustomerEmail = model.CustomerEmail.Trim(),
                CustomerAddress = model.CustomerAddress.Trim()
            };

            try
            {
                db.Users.Add(user);
                db.Customers.Add(customer);

                // Lưu cả User và Customer
                db.SaveChanges();

                TempData["RegisterSuccess"] =
                    "Tạo tài khoản thành công! Vui lòng đăng nhập.";

                return RedirectToAction("Login");
            }
            catch (DbEntityValidationException ex)
            {
                // Hiển thị lỗi Data Annotation cụ thể
                foreach (var entityError in ex.EntityValidationErrors)
                {
                    foreach (var error in entityError.ValidationErrors)
                    {
                        ModelState.AddModelError(
                            error.PropertyName,
                            error.PropertyName + ": " +
                            error.ErrorMessage
                        );
                    }
                }

                return View(model);
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        // POST: Login/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Xóa cookie Forms Authentication
            System.Web.Security.FormsAuthentication.SignOut();

            // Xóa Session
            Session.Clear();
            Session.Abandon();

            // Quay về trang chủ
            return RedirectToAction("Index", "Home");
        }
    }
}