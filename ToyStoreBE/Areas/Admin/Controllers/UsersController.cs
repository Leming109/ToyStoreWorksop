
using System;
using System.Linq;
using System.Web.Mvc;
using ToyStoreBE.Helpers;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // Chỉ SuperAdmin được quản lý tài khoản
        protected override bool SuperAdminOnly
        {
            get { return true; }
        }

        // Cho Moderator vào Index để xem thông báo không có quyền
        protected override bool AllowModeratorToViewIndexOnly
        {
            get { return true; }
        }

        // =========================================
        // GET: Admin/Users
        // =========================================
        public ActionResult Index()
        {
            // Moderator chỉ được xem giao diện thông báo.
            // Không truy vấn và không gửi dữ liệu tài khoản xuống View.
            if (ViewBag.AdminRole as string == "M")
            {
                return View(new System.Collections.Generic.List<User>());
            }

            // SuperAdmin xem cả tài khoản A và M
            var users = db.Users
                .Where(u => u.UserRole == "A" || u.UserRole == "M")
                .OrderBy(u => u.UserRole)
                .ThenBy(u => u.Username)
                .ToList();

            return View(users);
        }

        // =========================================
        // GET: Admin/Users/Create
        // =========================================
        public ActionResult Create()
        {
            return View(new AdminUserCreateViewModel());
        }

        // =========================================
        // POST: Admin/Users/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminUserCreateViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(400);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = (model.Username ?? "").Trim();

            // Kiểm tra tên tài khoản
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng nhập tên tài khoản."
                );

                return View(model);
            }

            if (username.Length > 255)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên tài khoản không được vượt quá 255 ký tự."
                );

                return View(model);
            }

            // Kiểm tra tài khoản đã tồn tại
            bool existed = db.Users.Any(
                u => u.Username == username
            );

            if (existed)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên tài khoản này đã tồn tại."
                );

                return View(model);
            }

            // Kiểm tra mật khẩu
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui lòng nhập mật khẩu."
                );

                return View(model);
            }

            // Mã hóa mật khẩu
            string hashedPassword =
                CompactPasswordHasher.HashPassword(model.Password);

            // Database hiện đang dùng NCHAR(50)
            if (hashedPassword.Length > 50)
            {
                ModelState.AddModelError(
                    "",
                    "Mật khẩu sau khi mã hóa vượt quá độ dài cho phép."
                );

                return View(model);
            }

            // LUÔN tạo Moderator, không nhận Role từ form
            User user = new User
            {
                Username = username,
                Password = hashedPassword,
                UserRole = "M",
                IsLocked = false
            };

            db.Users.Add(user);
            db.SaveChanges();

            TempData["Success"] =
                "Tạo tài khoản Moderator thành công.";

            return RedirectToAction("Index");
        }

        // =========================================
        // POST: Admin/Users/ToggleLock
        // Khóa / Mở khóa Moderator
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleLock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400);
            }

            id = id.Trim();

            // Chỉ tìm tài khoản Moderator.
            // Không thể thao tác lên tài khoản SuperAdmin.
            var user = db.Users.FirstOrDefault(u =>
                u.Username == id &&
                u.UserRole == "M"
            );

            if (user == null)
            {
                TempData["Error"] =
                    "Không tìm thấy tài khoản Moderator hợp lệ.";

                return RedirectToAction("Index");
            }

            // Không cho phép tự khóa tài khoản đang đăng nhập
            string currentUsername = User.Identity.Name ?? "";

            if (string.Equals(
                user.Username.TrimEnd(),
                currentUsername.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Bạn không thể khóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            // Đảo trạng thái khóa
            user.IsLocked = !user.IsLocked;

            db.SaveChanges();

            TempData["Success"] = user.IsLocked
                ? "Đã khóa tài khoản Moderator thành công."
                : "Đã mở khóa tài khoản Moderator thành công.";

            return RedirectToAction("Index");
        }

        // =========================================
        // DISPOSE
        // =========================================
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
