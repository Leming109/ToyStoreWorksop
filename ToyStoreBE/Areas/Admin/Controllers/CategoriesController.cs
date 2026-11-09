using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using ToyStoreBE.Models;
using ToyStoreBE.Models.ViewModel;
using System.Data.Entity.Infrastructure;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class CategoriesController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // GET: Admin/Categories
        public ActionResult Index(int page = 1)
        {
            const int pageSize = 5;

            var totalCategories = db.Categories.Count();
            var totalPages = (int)Math.Ceiling(totalCategories / (double)pageSize);

            if (totalPages == 0)
            {
                page = 1;
            }
            else
            {
                page = Math.Max(1, Math.Min(page, totalPages));
            }

            var categories = db.Categories
                .OrderBy(c => c.CategoryID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryRowViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    ProductCount = db.Products.Count(p => p.CategoryID == c.CategoryID)
                })
                .ToList();

            var model = new CategoryIndexViewModel
            {
                Categories = categories,
                Page = page,
                TotalPages = totalPages,
                TotalCategories = totalCategories,
                TotalProducts = db.Products.Count()
            };

            return View(model);
        }

        // GET: Admin/Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // GET: Admin/Categories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            //if (ModelState.IsValid)
            //{
            //    db.Categories.Add(category);
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            //}

            //return View(category);

            if (category == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (category.CategoryName != null)
                category.CategoryName = category.CategoryName.Trim();

            if (ModelState.IsValid)
            {
                // Kiểm tra tên danh mục đã tồn tại
                bool existed = db.Categories.Any(c =>
                    c.CategoryName == category.CategoryName);

                if (existed)
                {
                    ModelState.AddModelError(
                        "CategoryName",
                        "Tên danh mục này đã tồn tại.");

                    return View(category);
                }

                db.Categories.Add(category);
                db.SaveChanges();

                TempData["Success"] = "Thêm danh mục thành công.";
                return RedirectToAction("Index");
            }

            return View(category);

        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
            //return View(Details(id));
        }

        // POST: Admin/Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category model)
        {
            //if (ModelState.IsValid)
            //{
            //    db.Entry(category).State = EntityState.Modified;
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            //}
            //return View(category);


            if (model == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (model.CategoryName != null)
                model.CategoryName = model.CategoryName.Trim();

            if (!ModelState.IsValid)
                return View(model);

            var category = db.Categories.Find(model.CategoryID);

            if (category == null)
                return HttpNotFound();

            bool existed = db.Categories.Any(c =>
                c.CategoryName == model.CategoryName &&
                c.CategoryID != model.CategoryID);

            if (existed)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Tên danh mục này đã được sử dụng.");

                return View(model);
            }

            // Chỉ cập nhật tên danh mục
            category.CategoryName = model.CategoryName;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật danh mục thành công.";
            return RedirectToAction("Index");
        }

        // GET: Admin/Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
            //return View(Details(id));
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra sản phẩm thuộc danh mục
            bool hasProducts = db.Products.Any(p =>
                p.CategoryID == id);

            if (hasProducts)
            {
                TempData["Error"] =
                    "Không thể xóa danh mục vì vẫn còn sản phẩm thuộc danh mục này.";

                return RedirectToAction("Index");
            }

            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();

                TempData["Success"] = "Xóa danh mục thành công.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa danh mục vì đang có dữ liệu liên quan.";
            }

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
