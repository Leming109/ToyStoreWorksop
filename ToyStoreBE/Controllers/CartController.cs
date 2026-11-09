using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Controllers
{
    public class CartController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        private List<CartItem> GetCart()
        {
            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }

            return cart;
        }
        // GET: Cart
        public ActionResult Index()
        {
            var cart = GetCart();

            ViewBag.TotalQuantity = cart.Sum(x => x.Quantity);
            ViewBag.TotalItems = cart.Count;
            ViewBag.TotalAmount = cart.Sum(x => x.TotalPrice);

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(int id, int quantity = 1)
        {
            if (quantity < 1)
                quantity = 1;

            if (quantity > 99)
                quantity = 99;

            var product = db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            var cart = GetCart();

            // Kiểm tra sản phẩm đã tồn tại trong giỏ chưa
            var existingItem = cart.FirstOrDefault(
                x => x.ProductID == id
            );

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(
                    existingItem.Quantity + quantity,
                    99
                );
            }
            else
            {
                // Ưu tiên SalePrice nếu có giá khuyến mãi hợp lệ
                decimal unitPrice =
                    product.SalePrice > 0 &&
                    product.SalePrice < product.ProductPrice
                    ? product.SalePrice
                    : product.ProductPrice;

                var item = new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,

                    ProductImage = string.IsNullOrWhiteSpace(
                        product.ProductImage)
                        ? "~/Content/images/default_img.png"
                        : product.ProductImage,

                    CategoryName = product.Category != null
                        ? product.Category.CategoryName
                        : "Chưa phân loại",

                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    Note = ""
                };

                cart.Add(item);
            }

            Session["Cart"] = cart;

            TempData["CartMessage"] =
                "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateItem(int id, int quantity, string note)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                x => x.ProductID == id
            );

            if (item == null)
                return RedirectToAction("Index");

            // Số lượng bằng 0 thì xóa sản phẩm
            if (quantity <= 0)
            {
                cart.Remove(item);

                Session["Cart"] = cart;

                TempData["CartMessage"] =
                    "Đã xóa sản phẩm khỏi giỏ hàng.";

                return RedirectToAction("Index");
            }

            if (quantity > 99)
                quantity = 99;

            item.Quantity = quantity;

            // Lưu ghi chú tối đa 500 ký tự
            item.Note = (note ?? "").Trim();

            if (item.Note.Length > 500)
            {
                item.Note = item.Note.Substring(0, 500);
            }

            Session["Cart"] = cart;

            TempData["CartMessage"] =
                "Đã cập nhật giỏ hàng thành công.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                x => x.ProductID == id
            );

            if (item != null)
            {
                cart.Remove(item);
            }

            Session["Cart"] = cart;

            TempData["CartMessage"] =
                "Đã xóa sản phẩm khỏi giỏ hàng.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear()
        {
            var cart = GetCart();

            cart.Clear();

            Session["Cart"] = cart;

            TempData["CartMessage"] =
                "Đã xóa toàn bộ giỏ hàng.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Checkout()
        {
            // Không cho thanh toán nếu chưa đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                TempData["CheckoutMessage"] =
                    "Vui lòng đăng nhập trước khi đặt hàng.";

                return RedirectToAction("Login", "Login");
            }

            var cart = GetCart();
            // Cập nhật thông tin và giá hiện tại từ database
            var productIds = cart.Select(x => x.ProductID).Distinct().ToList();

            var currentProducts = db.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToList();

            var productDictionary = currentProducts.ToDictionary(
                p => p.ProductID
            );

            var categoryIds = currentProducts
                .Select(p => p.CategoryID)
                .Distinct()
                .ToList();

            var categoryNames = db.Categories
                .Where(c => categoryIds.Contains(c.CategoryID))
                .ToDictionary(c => c.CategoryID, c => c.CategoryName);

            foreach (var item in cart.ToList())
            {
                Product product;

                if (!productDictionary.TryGetValue(item.ProductID, out product))
                {
                    cart.Remove(item);
                    continue;
                }

                item.ProductName = product.ProductName;

                item.ProductImage = string.IsNullOrWhiteSpace(product.ProductImage)
                    ? "~/Content/images/default_img.png"
                    : product.ProductImage;

                item.UnitPrice =
                    product.SalePrice > 0 &&
                    product.SalePrice < product.ProductPrice
                    ? product.SalePrice
                    : product.ProductPrice;

                item.CategoryName = categoryNames.ContainsKey(product.CategoryID)
                    ? categoryNames[product.CategoryID]
                    : "Chưa phân loại";
            }

            Session["Cart"] = cart;

            // Giỏ hàng rỗng thì quay lại giỏ hàng
            if (cart == null || !cart.Any())
            {
                TempData["CartMessage"] =
                    "Giỏ hàng đang trống, chưa thể thanh toán.";

                return RedirectToAction("Index");
            }

            // Lấy thông tin khách hàng đang đăng nhập
            string username = User.Identity.Name;

            var customer = db.Customers.FirstOrDefault(
                c => c.Username == username
            );

            if (customer == null)
            {
                TempData["CheckoutMessage"] =
                    "Không tìm thấy thông tin khách hàng.";

                return RedirectToAction("Index");
            }

            var model = new CheckOutVM
            {
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress,
                CartItems = cart
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckOutVM model)
        {
            // Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }

            var cart = GetCart();

            // Kiểm tra giỏ hàng rỗng
            if (cart == null || !cart.Any())
            {
                TempData["CartMessage"] =
                    "Giỏ hàng đang trống, không thể đặt hàng.";

                return RedirectToAction("Index");
            }

            // Gắn lại sản phẩm để hiển thị nếu có lỗi validation
            model.CartItems = cart;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = User.Identity.Name;

            // Tìm thông tin khách hàng
            var customer = db.Customers.FirstOrDefault(
                c => c.Username == username
            );

            if (customer == null)
            {
                ModelState.AddModelError(
                    "",
                    "Không tìm thấy thông tin khách hàng."
                );

                return View(model);
            }

            // Kiểm tra số lượng sản phẩm
            if (cart.Any(x => x.Quantity <= 0 || x.Quantity > 99))
            {
                ModelState.AddModelError(
                    "",
                    "Số lượng sản phẩm không hợp lệ."
                );

                return View(model);
            }

            // Lấy lại dữ liệu sản phẩm từ database
            var productIds = cart
                .Select(x => x.ProductID)
                .Distinct()
                .ToList();

            var products = db.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToList();

            var productDictionary = products.ToDictionary(
                p => p.ProductID
            );

            // Kiểm tra các sản phẩm có còn tồn tại không
            if (products.Count != productIds.Count)
            {
                ModelState.AddModelError(
                    "",
                    "Một hoặc nhiều sản phẩm trong giỏ hàng không còn tồn tại."
                );

                return View(model);
            }

            // Bắt đầu transaction
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Cập nhật thông tin khách hàng
                    customer.CustomerName = model.CustomerName.Trim();
                    customer.CustomerPhone = model.CustomerPhone.Trim();
                    customer.CustomerEmail = model.CustomerEmail.Trim();
                    customer.CustomerAddress = model.CustomerAddress.Trim();

                    // Tính tổng tiền từ giá database
                    decimal totalAmount = 0;

                    var details = new List<OrderDetail>();

                    foreach (var item in cart)
                    {
                        var product = productDictionary[item.ProductID];

                        // Lấy giá bán hiện tại từ database
                        decimal unitPrice =
                            product.SalePrice > 0 &&
                            product.SalePrice < product.ProductPrice
                            ? product.SalePrice
                            : product.ProductPrice;

                        decimal subTotal = unitPrice * item.Quantity;

                        totalAmount += subTotal;

                        // Tạo chi tiết đơn hàng
                        details.Add(new OrderDetail
                        {
                            ProductID = product.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            Note = item.Note
                        });
                    }

                    // Tạo đơn hàng chính
                    var order = new Order
                    {
                        CustomerID = customer.CustomerID,
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount,
                        PaymentStatus = "COD",
                        AddressDelivery = model.CustomerAddress.Trim()
                    };

                    // Lưu Order trước để lấy OrderID
                    db.Orders.Add(order);

                    db.SaveChanges();

                    // Gắn OrderID vào từng OrderDetail
                    foreach (var detail in details)
                    {
                        detail.OrderID = order.OrderID;
                        db.OrderDetails.Add(detail);
                    }

                    // Lưu toàn bộ chi tiết đơn hàng
                    db.SaveChanges();

                    // Xác nhận transaction
                    transaction.Commit();

                    // Chỉ xóa giỏ hàng sau khi lưu thành công
                    cart.Clear();
                    Session["Cart"] = cart;

                    // Chuyển đến trang thông báo thành công
                    return RedirectToAction(
                        "OrderSuccess",
                        new { id = order.OrderID }
                    );
                }
                catch (DbEntityValidationException ex)
                {
                    transaction.Rollback();

                    foreach (var entityError in ex.EntityValidationErrors)
                    {
                        foreach (var error in entityError.ValidationErrors)
                        {
                            ModelState.AddModelError(
                                error.PropertyName,
                                error.PropertyName + ": " + error.ErrorMessage
                            );
                        }
                    }

                    model.CartItems = cart;

                    return View(model);
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "",
                        "Đặt hàng không thành công. Vui lòng kiểm tra lại thông tin."
                    );

                    model.CartItems = cart;

                    return View(model);
                }
            }
        }

        [HttpGet]
        public ActionResult OrderSuccess(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }

            string username = User.Identity.Name;

            var customer = db.Customers.FirstOrDefault(
                c => c.Username == username
            );

            if (customer == null)
                return HttpNotFound();

            // Chỉ cho khách xem đơn hàng thuộc về mình
            var order = db.Orders
                .Include("OrderDetails.Product")
                .FirstOrDefault(o =>
                    o.OrderID == id.Value &&
                    o.CustomerID == customer.CustomerID
                );

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // Giải phóng database
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