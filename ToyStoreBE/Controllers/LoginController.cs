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

        // =========================================
        // GET: Login/InfoProfile
        // =========================================
        [Authorize]
        [HttpGet]
        public ActionResult InfoProfile()
        {
            string username = User.Identity.Name;

            // Chỉ cho phép tài khoản khách hàng
            var user = db.Users.FirstOrDefault(u =>
                u.Username == username && u.UserRole == "C");

            if (user == null)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden);
            }

            // Lấy thông tin khách hàng đang đăng nhập
            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == user.Username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            // Hiển thị tên tài khoản
            ViewBag.Username = user.Username.TrimEnd();

            var model = new InfoProfileViewModel
            {
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress
            };

            return View(model);
        }


        // =========================================
        // POST: Login/InfoProfile
        // =========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InfoProfile(InfoProfileViewModel model)
        {
            string username = User.Identity.Name;

            // Xác định tài khoản khách hàng đang đăng nhập
            var user = db.Users.FirstOrDefault(u =>
                u.Username == username && u.UserRole == "C");

            if (user == null)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden);
            }

            // Chỉ lấy Customer thuộc tài khoản hiện tại
            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == user.Username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            ViewBag.Username = user.Username.TrimEnd();

            if (model == null)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            // Chuẩn hóa dữ liệu
            model.CustomerName = model.CustomerName?.Trim();
            model.CustomerPhone = model.CustomerPhone?.Trim();
            model.CustomerEmail = model.CustomerEmail?.Trim();
            model.CustomerAddress = model.CustomerAddress?.Trim();

            // Kiểm tra lại Data Annotation sau khi chuẩn hóa
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Chỉ cập nhật thông tin Customer
            // Không thay đổi Username hoặc Password
            customer.CustomerName = model.CustomerName;
            customer.CustomerPhone = model.CustomerPhone;
            customer.CustomerEmail = model.CustomerEmail;
            customer.CustomerAddress = model.CustomerAddress;

            try
            {
                db.SaveChanges();

                TempData["ProfileSuccess"] =
                    "Cập nhật thông tin cá nhân thành công.";

                return RedirectToAction("InfoProfile");
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật thông tin. Vui lòng kiểm tra lại dữ liệu.");

                return View(model);
            }
        }
    }
}