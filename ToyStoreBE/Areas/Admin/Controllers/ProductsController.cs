using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ToyStoreBE.Models;
using ToyStoreBE.Models.ViewModels;


namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class ProductsController : AdminBaseController
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // GET: Admin/Products
        public ActionResult Index(string searchTerm, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            var model = new SearchProductVM();
            var products = db.Products.Include("Category").AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            { // Thanh tim kiem
                products = products.Where(p =>
                p.ProductName.Contains(searchTerm) ||
                p.ProductDecription.Contains(searchTerm) ||
                p.Category.CategoryName.Contains(searchTerm)
                );
            }
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice <= maxPrice.Value);
            }
            switch (sortOrder)
            {
                case "name_asc": products = products.OrderBy(p => p.ProductName); break;
                case "name_desc": products = products.OrderByDescending(p => p.ProductName); break;
                case "price_asc": products = products.OrderBy(p => p.ProductPrice); break;
                case "price_desc": products = products.OrderByDescending(p => p.ProductPrice); break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }
            model.SortOrder = sortOrder;

            int pageNumber = page ?? 1;
            int pageSize = 5;
            model.Products = products.ToPagedList(pageNumber, pageSize);
            return View(model);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products
                .Include("Category")
                .FirstOrDefault(p => p.ProductID == id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Admin/Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
    [Bind(Include = "CategoryID,ProductName,ProductDecription,ProductPrice,SalePrice,StockQuantity,ProductImage")]
    Product product)
        {
            if (product == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            product.ProductName = (product.ProductName ?? "").Trim();
            product.ProductDecription = (product.ProductDecription ?? "").Trim();
            product.ProductImage = (product.ProductImage ?? "").Trim();

            if (string.IsNullOrWhiteSpace(product.ProductName))
                ModelState.AddModelError("ProductName", "Vui lòng nhập tên sản phẩm.");

            if (string.IsNullOrWhiteSpace(product.ProductDecription))
                ModelState.AddModelError("ProductDecription", "Vui lòng nhập mô tả sản phẩm.");

            if (!db.Categories.Any(c => c.CategoryID == product.CategoryID))
                ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục hợp lệ.");

            if (product.ProductPrice <= 0)
                ModelState.AddModelError("ProductPrice", "Giá bán phải lớn hơn 0.");

            if (product.SalePrice < 0 || product.SalePrice > product.ProductPrice)
                ModelState.AddModelError("SalePrice", "Giá khuyến mãi không hợp lệ.");

            if (product.StockQuantity < 0)
                ModelState.AddModelError("StockQuantity", "Số lượng tồn kho không được âm.");

            // Không nhập ảnh thì dùng ảnh mặc định
            if (string.IsNullOrWhiteSpace(product.ProductImage))
            {
                product.ProductImage = "~/Content/images/default_img.png";
            }
            else if (!IsValidProductImage(product.ProductImage))
            {
                ModelState.AddModelError(
                    "ProductImage",
                    "Link ảnh phải bắt đầu bằng http:// hoặc https://.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryID = new SelectList(
                    db.Categories,
                    "CategoryID",
                    "CategoryName",
                    product.CategoryID);

                return View(product);
            }

            try
            {
                db.Products.Add(product);
                db.SaveChanges();

                TempData["Success"] = "Thêm sản phẩm thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.CategoryID = new SelectList(
                    db.Categories,
                    "CategoryID",
                    "CategoryName",
                    product.CategoryID);

                ModelState.AddModelError(
                    "",
                    "Không thể thêm sản phẩm: " +
                    ex.GetBaseException().Message);

                return View(product);
            }
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Khóa danh mục khi cập nhật sản phẩm
            ViewBag.LockCategory = true;

            // Lấy tên danh mục hiện tại
            ViewBag.CategoryName = db.Categories
                .Where(c => c.CategoryID == product.CategoryID)
                .Select(c => c.CategoryName)
                .FirstOrDefault();

            return View(product);
        }

        // POST: Admin/Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
    [Bind(Include = "ProductID,ProductName,ProductPrice,SalePrice,StockQuantity,ProductImage")]
    Product product)
        {
            if (product == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var existingProduct = db.Products.Find(product.ProductID);

            if (existingProduct == null)
                return HttpNotFound();

            // Giữ nguyên mô tả và danh mục
            product.ProductDecription = existingProduct.ProductDecription;
            product.CategoryID = existingProduct.CategoryID;

            ModelState.Remove("ProductDecription");
            ModelState.Remove("CategoryID");

            product.ProductName = (product.ProductName ?? "").Trim();

            string imageUrl = (product.ProductImage ?? "").Trim();

            if (string.IsNullOrWhiteSpace(product.ProductName))
                ModelState.AddModelError("ProductName", "Vui lòng nhập tên sản phẩm.");

            if (product.ProductPrice <= 0)
                ModelState.AddModelError("ProductPrice", "Giá bán phải lớn hơn 0.");

            if (product.SalePrice < 0 || product.SalePrice > product.ProductPrice)
                ModelState.AddModelError("SalePrice", "Giá khuyến mãi không hợp lệ.");

            if (product.StockQuantity < 0)
                ModelState.AddModelError("StockQuantity", "Số lượng tồn kho không được âm.");

            // Để trống URL thì giữ ảnh hiện tại
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                product.ProductImage = existingProduct.ProductImage;
                ModelState.Remove("ProductImage");
            }
            else
            {
                product.ProductImage = imageUrl;

                if (!IsValidProductImage(imageUrl))
                {
                    ModelState.AddModelError(
                        "ProductImage",
                        "Link ảnh phải bắt đầu bằng http:// hoặc https://.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LockCategory = true;

                ViewBag.CategoryName = db.Categories
                    .Where(c => c.CategoryID == existingProduct.CategoryID)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                return View(product);
            }

            try
            {
                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductPrice = product.ProductPrice;
                existingProduct.SalePrice = product.SalePrice;
                existingProduct.StockQuantity = product.StockQuantity;

                // Cập nhật URL mới nếu có
                existingProduct.ProductImage = product.ProductImage;

                db.SaveChanges();

                TempData["Success"] = "Cập nhật sản phẩm thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.LockCategory = true;

                ViewBag.CategoryName = db.Categories
                    .Where(c => c.CategoryID == existingProduct.CategoryID)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật sản phẩm: " +
                    ex.GetBaseException().Message);

                return View(product);
            }
        }
        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products
                .Include("Category")
                .FirstOrDefault(p => p.ProductID == id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra sản phẩm đã xuất hiện trong đơn hàng chưa
            bool hasOrderDetails = db.OrderDetails.Any(
                od => od.ProductID == id);

            if (hasOrderDetails)
            {
                TempData["Error"] =
                    "Không thể xóa sản phẩm vì sản phẩm đã xuất hiện trong đơn hàng.";

                return RedirectToAction("Index");
            }

            try
            {
                db.Products.Remove(product);
                db.SaveChanges();

                TempData["Success"] = "Xóa sản phẩm thành công.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa sản phẩm vì vẫn còn dữ liệu liên quan.";
            }

            return RedirectToAction("Index");
        }

        private bool IsValidImageFile(
    HttpPostedFileBase file,
    string extension)
        {
            if (file == null || file.InputStream == null)
                return false;

            var stream = file.InputStream;

            if (!stream.CanSeek)
                return false;

            byte[] header = new byte[12];

            stream.Position = 0;
            int bytesRead = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            if (extension == ".jpg" || extension == ".jpeg")
            {
                return bytesRead >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF;
            }

            if (extension == ".png")
            {
                return bytesRead >= 8
                    && header[0] == 0x89
                    && header[1] == 0x50
                    && header[2] == 0x4E
                    && header[3] == 0x47
                    && header[4] == 0x0D
                    && header[5] == 0x0A
                    && header[6] == 0x1A
                    && header[7] == 0x0A;
            }

            if (extension == ".webp")
            {
                return bytesRead >= 12
                    && header[0] == 0x52
                    && header[1] == 0x49
                    && header[2] == 0x46
                    && header[3] == 0x46
                    && header[8] == 0x57
                    && header[9] == 0x45
                    && header[10] == 0x42
                    && header[11] == 0x50;
            }

            return false;
        }

        private bool IsValidProductImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            imageUrl = imageUrl.Trim();

            // Cho phép giữ lại đường dẫn ảnh nội bộ cũ
            if (imageUrl.StartsWith("~/", StringComparison.Ordinal))
                return true;

            Uri uri;

            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps;
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
