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
        public ActionResult Create([Bind(Include = "CategoryID,ProductName,ProductDecription,ProductPrice,SalePrice,StockQuantity")] Product product)
        {
            if (product == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Nhận ảnh từ form
            HttpPostedFileBase imageFile = Request.Files["UploadImg"];

            // Khai báo extension bên ngoài để sử dụng ở nhiều vị trí
            string extension = "";

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                extension = Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();
            }

            // Chuẩn hóa dữ liệu
            product.ProductName = product.ProductName == null
                ? null
                : product.ProductName.Trim();

            product.ProductDecription = product.ProductDecription == null
                ? null
                : product.ProductDecription.Trim();

            // Kiểm tra tên sản phẩm
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm.");
            }

            // Kiểm tra mô tả
            if (string.IsNullOrWhiteSpace(product.ProductDecription))
            {
                ModelState.AddModelError(
                    "ProductDecription",
                    "Vui lòng nhập mô tả sản phẩm.");
            }

            // Kiểm tra danh mục
            bool categoryExists = db.Categories.Any(
                c => c.CategoryID == product.CategoryID);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục hợp lệ.");
            }

            // Kiểm tra giá bán
            if (product.ProductPrice <= 0)
            {
                ModelState.AddModelError(
                    "ProductPrice",
                    "Giá bán phải lớn hơn 0.");
            }

            // Kiểm tra giá khuyến mãi
            if (product.SalePrice < 0)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được âm.");
            }

            // Nếu SalePrice là giá bán sau giảm giá,
            // giá này không được cao hơn giá gốc.
            if (product.SalePrice > product.ProductPrice)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được lớn hơn giá bán.");
            }

            // Kiểm tra tồn kho
            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Số lượng tồn kho không được âm.");
            }

            // Kiểm tra ảnh được tải lên
            if (imageFile != null && imageFile.ContentLength > 0)
            {

                string[] allowedExtensions =
                {
            ".jpg", ".jpeg", ".png", ".webp"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
                }
                else if (imageFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "",
                        "Dung lượng ảnh không được vượt quá 5 MB.");
                }
                else if (!IsValidImageFile(imageFile, extension))
                {
                    ModelState.AddModelError(
                        "",
                        "File được chọn không phải ảnh hợp lệ.");
                }
            }

            // Nếu dữ liệu không hợp lệ thì quay lại form
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryID = new SelectList(
                    db.Categories,
                    "CategoryID",
                    "CategoryName",
                    product.CategoryID);

                return View(product);
            }

            // Ảnh mặc định nếu người dùng không chọn ảnh
            product.ProductImage = "~/Content/images/default_img.png";

            string savedFilePath = null;

            try
            {
                // Nếu có chọn ảnh mới
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Content/images");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Tạo tên file riêng để tránh trùng ảnh
                    string fileName = Guid.NewGuid().ToString("N") + extension;

                    savedFilePath = Path.Combine(folderPath, fileName);

                    imageFile.SaveAs(savedFilePath);

                    // Lưu đường dẫn ảnh vào database
                    product.ProductImage = "~/Content/images/" + fileName;
                }

                // Lưu sản phẩm
                db.Products.Add(product);
                db.SaveChanges();

                TempData["Success"] = "Thêm sản phẩm thành công.";

                return RedirectToAction("Index");
            }
            catch
            {
                // Nếu lưu database thất bại thì xóa file ảnh vừa tải lên
                if (savedFilePath != null && System.IO.File.Exists(savedFilePath))
                {
                    System.IO.File.Delete(savedFilePath);
                }

                throw;
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
    [Bind(Include = "ProductID,ProductName,ProductPrice,SalePrice,StockQuantity")]
    Product product)
        {
            if (product == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tìm sản phẩm hiện tại trong database
            var existingProduct = db.Products.Find(product.ProductID);

            if (existingProduct == null)
            {
                return HttpNotFound();
            }

            // Loại bỏ lỗi xác thực của các trường không cho chỉnh sửa
            ModelState.Remove("ProductDecription");
            ModelState.Remove("CategoryID");
            ModelState.Remove("ProductImage");

            // Giữ nguyên mô tả, danh mục và ảnh cũ
            product.ProductDecription = existingProduct.ProductDecription;
            product.CategoryID = existingProduct.CategoryID;
            product.ProductImage = existingProduct.ProductImage;

            // Nhận ảnh mới từ form
            HttpPostedFileBase imageFile = Request.Files["UploadImg"];

            string extension = "";

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                extension = Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();
            }

            // Chuẩn hóa tên sản phẩm
            product.ProductName = product.ProductName == null
                ? null
                : product.ProductName.Trim();

            // Kiểm tra tên sản phẩm
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm.");
            }

            // Kiểm tra giá bán
            if (product.ProductPrice <= 0)
            {
                ModelState.AddModelError(
                    "ProductPrice",
                    "Giá bán phải lớn hơn 0.");
            }

            // Kiểm tra giá khuyến mãi
            if (product.SalePrice < 0)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được âm.");
            }

            if (product.SalePrice > product.ProductPrice)
            {
                ModelState.AddModelError(
                    "SalePrice",
                    "Giá khuyến mãi không được lớn hơn giá bán.");
            }

            // Kiểm tra số lượng tồn kho
            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Số lượng tồn kho không được âm.");
            }

            // Kiểm tra ảnh mới
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string[] allowedExtensions =
                {
            ".jpg", ".jpeg", ".png", ".webp"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
                }
                else if (imageFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "",
                        "Dung lượng ảnh không được vượt quá 5 MB.");
                }
                else if (!IsValidImageFile(imageFile, extension))
                {
                    ModelState.AddModelError(
                        "",
                        "File được chọn không phải ảnh hợp lệ.");
                }
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                ViewBag.LockCategory = true;

                ViewBag.CategoryName = db.Categories
                    .Where(c => c.CategoryID == existingProduct.CategoryID)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                return View(product);
            }

            string savedFilePath = null;

            try
            {
                // Chỉ thay ảnh khi người dùng chọn ảnh mới
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Content/images");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = Guid.NewGuid().ToString("N") + extension;

                    savedFilePath = Path.Combine(folderPath, fileName);

                    imageFile.SaveAs(savedFilePath);

                    existingProduct.ProductImage =
                        "~/Content/images/" + fileName;
                }

                // Cập nhật thông tin được phép chỉnh sửa
                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductPrice = product.ProductPrice;
                existingProduct.SalePrice = product.SalePrice;
                existingProduct.StockQuantity = product.StockQuantity;

                // Không cập nhật ProductDecription
                // Không cập nhật CategoryID

                db.SaveChanges();

                TempData["Success"] = "Cập nhật sản phẩm thành công.";

                return RedirectToAction("Index");
            }
            catch
            {
                // Xóa ảnh mới nếu lưu database thất bại
                if (savedFilePath != null &&
                    System.IO.File.Exists(savedFilePath))
                {
                    System.IO.File.Delete(savedFilePath);
                }

                throw;
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
