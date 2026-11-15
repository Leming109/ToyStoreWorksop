using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Controllers
{
    public class OrderHistoryController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        private Customer GetCurrentCustomer()
        {
            if (User == null ||
                User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return null;
            }

            string username = User.Identity.Name;

            // Chỉ chấp nhận tài khoản khách hàng
            var user = db.Users.FirstOrDefault(u =>
                u.Username == username &&
                u.UserRole == "C");

            if (user == null)
                return null;

            return db.Customers.FirstOrDefault(c =>
                c.Username == user.Username);
        }

        // GET: OrderHistory
        public ActionResult Index()
        {
            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // Chỉ lấy đơn hàng của khách đang đăng nhập
            var orders = db.Orders
                .Where(o => o.CustomerID == customer.CustomerID)
                .Include("OrderDetails")
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderID)
                .ToList();

            return View(orders);
        }

        // GET: OrderHistory/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // Chỉ cho khách xem đơn hàng thuộc về mình
            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o =>
                    o.OrderID == id.Value &&
                    o.CustomerID == customer.CustomerID);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // POST: OrderHistory/BuyAgain
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuyAgain(int id)
        {
            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // Kiểm tra đơn hàng có thuộc khách đang đăng nhập không
            var order = db.Orders
                .Include("OrderDetails")
                .FirstOrDefault(o =>
                    o.OrderID == id &&
                    o.CustomerID == customer.CustomerID);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.OrderDetails == null ||
                !order.OrderDetails.Any())
            {
                TempData["CartMessage"] =
                    "Đơn hàng này không có sản phẩm để mua lại.";

                return RedirectToAction("Index", "Cart");
            }

            // Lấy giỏ hàng hiện tại
            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                cart = new List<CartItem>();
            }

            // Gom các dòng trùng ProductID nếu có
            var orderItems = order.OrderDetails
                .GroupBy(d => d.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Quantity = g.Sum(d => d.Quantity),
                    Note = g.Select(d => d.Note)
                            .FirstOrDefault()
                })
                .ToList();

            var productIds = orderItems
                .Select(x => x.ProductID)
                .Distinct()
                .ToList();

            // Lấy thông tin sản phẩm hiện tại và danh mục
            var products = db.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.ProductID))
                .ToList();

            var productDictionary = products.ToDictionary(
                p => p.ProductID);

            int totalAdded = 0;
            var skippedProducts = new List<string>();

            foreach (var orderItem in orderItems)
            {
                Product product;

                // Sản phẩm không còn tồn tại
                if (!productDictionary.TryGetValue(
                    orderItem.ProductID, out product))
                {
                    skippedProducts.Add(
                        "Sản phẩm #" + orderItem.ProductID);

                    continue;
                }

                // Tìm sản phẩm trong giỏ hàng hiện tại
                var existingItem = cart.FirstOrDefault(
                    x => x.ProductID == product.ProductID);

                int currentQuantity = existingItem != null
                    ? existingItem.Quantity
                    : 0;

                // Kiểm tra tồn kho và giới hạn 99 sản phẩm
                int stockQuantity = Math.Max(
                    0, product.StockQuantity);

                int remainingStock =
                    stockQuantity - currentQuantity;

                int remainingLimit =
                    99 - currentQuantity;

                int quantityToAdd = Math.Min(
                    orderItem.Quantity,
                    Math.Min(remainingStock, remainingLimit));

                // Không thể thêm sản phẩm
                if (quantityToAdd <= 0)
                {
                    skippedProducts.Add(product.ProductName);
                    continue;
                }

                // Lấy giá bán hiện tại
                decimal unitPrice =
                    product.SalePrice > 0 &&
                    product.SalePrice < product.ProductPrice
                    ? product.SalePrice
                    : product.ProductPrice;

                string imagePath =
                    string.IsNullOrWhiteSpace(product.ProductImage)
                    ? "~/Content/images/default_img.png"
                    : product.ProductImage;

                string categoryName = product.Category != null
                    ? product.Category.CategoryName
                    : "Chưa phân loại";

                if (existingItem != null)
                {
                    // Cộng số lượng vào sản phẩm đã có
                    existingItem.Quantity += quantityToAdd;

                    // Cập nhật thông tin và giá hiện tại
                    existingItem.ProductName = product.ProductName;
                    existingItem.ProductImage = imagePath;
                    existingItem.CategoryName = categoryName;
                    existingItem.UnitPrice = unitPrice;
                }
                else
                {
                    // Thêm sản phẩm mới vào giỏ
                    cart.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        ProductImage = imagePath,
                        CategoryName = categoryName,
                        UnitPrice = unitPrice,
                        Quantity = quantityToAdd,
                        Note = (orderItem.Note ?? "").Trim()
                    });
                }

                totalAdded += quantityToAdd;

                // Nếu số lượng cũ vượt tồn kho hoặc giới hạn,
                // thông báo sản phẩm chưa được thêm đầy đủ
                if (quantityToAdd < orderItem.Quantity)
                {
                    skippedProducts.Add(product.ProductName);
                }
            }

            // Lưu giỏ hàng vào Session
            Session["Cart"] = cart;

            if (totalAdded == 0)
            {
                TempData["CartMessage"] =
                    "Không có sản phẩm nào được thêm. " +
                    "Vui lòng kiểm tra tồn kho hoặc giỏ hàng hiện tại.";
            }
            else if (skippedProducts.Any())
            {
                TempData["CartMessage"] =
                    "Đã thêm " + totalAdded +
                    " sản phẩm vào giỏ hàng. Một số sản phẩm không thể " +
                    "thêm đầy đủ do giới hạn số lượng hoặc tồn kho: " +
                    string.Join(", ", skippedProducts.Distinct()) + ".";
            }
            else
            {
                TempData["CartMessage"] =
                    "Đã thêm sản phẩm từ đơn hàng #" +
                    order.OrderID + " vào giỏ hàng thành công.";
            }

            return RedirectToAction("Index", "Cart");
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