using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class AdminController : AdminBaseController
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // GET: Admin/Admin
        public ActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                // Tổng sản phẩm
                TotalProducts = db.Products.Count(),

                // Tổng danh mục
                TotalCategories = db.Categories.Count(),

                // Tổng khách hàng: chỉ lấy UserRole C
                TotalCustomers = db.Customers.Count(c =>
                    c.User != null &&
                    c.User.UserRole == "C"),

                // Tổng đơn hàng
                TotalOrders = db.Orders.Count(),

                // Tổng giá trị các đơn hàng
                TotalOrderValue = db.Orders
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,

                // 5 đơn hàng mới nhất
                RecentOrders = db.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderID = o.OrderID,

                        CustomerName = o.Customer.CustomerName,

                        OrderDate = o.OrderDate,

                        TotalAmount = o.TotalAmount,

                        PaymentStatus = o.PaymentStatus
                    })
                    .ToList()
            };

            return View(model);
        }


        // Dispose the database context when the controller is disposed
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