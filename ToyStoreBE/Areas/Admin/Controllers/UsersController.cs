using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Mvc;
using ToyStoreBE.Helpers;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class UsersController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // GET: Admin/Users
        public ActionResult Index()
        {
            var users = db.Users
       .Where(u => u.UserRole == "A")
       .OrderBy(u => u.Username)
       .ToList();

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Admin/Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminUserCreateViewModel model)
        {
            //if (ModelState.IsValid)
            //{
            //    db.Users.Add(user);
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            //}

            //return View(user);

            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            // Kiểm tra tên tài khoản đã tồn tại
            bool existed = db.Users.Any(u => u.Username == username);

            if (existed)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên tài khoản này đã tồn tại.");

                return View(model);
            }

            // Mã hóa mật khẩu trước khi lưu
            string hashedPassword =
                CompactPasswordHasher.HashPassword(model.Password);

            // Password trong database là NCHAR(50)
            if (hashedPassword.Length > 50)
            {
                ModelState.AddModelError(
                    "",
                    "Mật khẩu sau khi mã hóa vượt quá độ dài cho phép.");

                return View(model);
            }

            User user = new User
            {
                Username = username,
                Password = hashedPassword,
                UserRole = "A"
            };

            db.Users.Add(user);
            db.SaveChanges();

            TempData["Success"] = "Thêm tài khoản quản trị thành công.";

            return RedirectToAction("Index");
        }

        // GET: Admin/Users/Edit/5
        public ActionResult Edit(string id)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //User user = db.Users.Find(id);
            //if (user == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(user);

            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();

            var user = db.Users.FirstOrDefault(u =>
                u.Username == id && u.UserRole == "A");

            if (user == null)
                return HttpNotFound();

            var model = new AdminUserEditViewModel
            {
                Username = user.Username.TrimEnd()
            };

            return View(model);
        }

        // POST: Admin/Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, AdminUserEditViewModel model)
        {
            //if (ModelState.IsValid)
            //{
            //    db.Entry(user).State = EntityState.Modified;
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            //}
            //return View(user);

            if (string.IsNullOrWhiteSpace(id) || model == null)
                return HttpNotFound();

            id = id.Trim();
            model.Username = id;

            if (!ModelState.IsValid)
                return View(model);

            var user = db.Users.FirstOrDefault(u =>
                u.Username == id && u.UserRole == "A");

            if (user == null)
                return HttpNotFound();

            // Chỉ đổi mật khẩu khi người dùng nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                string hashedPassword =
                    CompactPasswordHasher.HashPassword(model.NewPassword);

                if (hashedPassword.Length > 50)
                {
                    ModelState.AddModelError(
                        "",
                        "Mật khẩu sau khi mã hóa vượt quá độ dài cho phép.");

                    return View(model);
                }

                user.Password = hashedPassword;
            }

            db.SaveChanges();

            TempData["Success"] = "Cập nhật tài khoản quản trị thành công.";

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: Admin/Users/Delete/5
        public ActionResult Delete(string id)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //User user = db.Users.Find(id);
            //if (user == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(user);

            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();

            var user = db.Users.FirstOrDefault(u =>
                u.Username == id && u.UserRole == "A");

            if (user == null)
                return HttpNotFound();

            // Không cho phép tự xóa tài khoản đang đăng nhập
            string currentUsername = User.Identity.Name ?? "";

            if (string.Equals(
                user.Username.Trim(),
                currentUsername.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Bạn không thể xóa tài khoản quản trị đang đăng nhập.";

                return RedirectToAction("Index");
            }

            // Không cho phép xóa tài khoản quản trị cuối cùng
            int adminCount = db.Users.Count(u => u.UserRole == "A");

            if (adminCount <= 1)
            {
                TempData["Error"] =
                    "Hệ thống phải có ít nhất một tài khoản quản trị.";

                return RedirectToAction("Index");
            }

            db.Users.Remove(user);
            db.SaveChanges();

            TempData["Success"] = "Xóa tài khoản quản trị thành công.";

            return RedirectToAction("Index");
        }




        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
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
