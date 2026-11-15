
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ToyStoreBE.Models;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // =========================================
        // GET: Admin/Orders
        // =========================================
        public ActionResult Index()
        {
            var orders = db.Orders
                .Include("Customer")
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderID)
                .ToList();

            return View(orders);
        }

        // =========================================
        // GET: Admin/Orders/Details/5
        // =========================================
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderID == id.Value);

            if (order == null)
            {
                return HttpNotFound();
            }

            string currentStatus = string.IsNullOrWhiteSpace(order.OrderStatus)
                ? "Chờ xác nhận"
                : order.OrderStatus.Trim();

            ViewBag.NextStatuses = GetNextStatuses(currentStatus);

            return View(order);
        }

        // =========================================
        // POST: Admin/Orders/UpdateStatus
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string orderStatus)
        {
            var order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            string currentStatus = string.IsNullOrWhiteSpace(order.OrderStatus)
                ? "Chờ xác nhận"
                : order.OrderStatus.Trim();

            string nextStatus = (orderStatus ?? "").Trim();

            // Lấy các trạng thái được phép chuyển tiếp
            var allowedStatuses = GetNextStatuses(currentStatus);

            bool isAllowed = allowedStatuses.Any(
                s => s.Value == nextStatus
            );

            if (!isAllowed)
            {
                TempData["Error"] =
                    "Không thể chuyển trạng thái đơn hàng theo lựa chọn này.";

                return RedirectToAction(
                    "Details",
                    new { id = id }
                );
            }

            // Cập nhật trạng thái
            order.OrderStatus = nextStatus;

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction(
                "Details",
                new { id = id }
            );
        }

        // =========================================
        // DANH SÁCH TRẠNG THÁI TIẾP THEO HỢP LỆ
        // =========================================
        private List<SelectListItem> GetNextStatuses(string currentStatus)
        {
            var statuses = new List<SelectListItem>();

            switch (currentStatus)
            {
                case "Chờ xác nhận":
                    statuses.Add(new SelectListItem
                    {
                        Text = "Đã xác nhận",
                        Value = "Đã xác nhận"
                    });

                    statuses.Add(new SelectListItem
                    {
                        Text = "Đã hủy",
                        Value = "Đã hủy"
                    });
                    break;

                case "Đã xác nhận":
                    statuses.Add(new SelectListItem
                    {
                        Text = "Đang chuẩn bị hàng",
                        Value = "Đang chuẩn bị hàng"
                    });

                    statuses.Add(new SelectListItem
                    {
                        Text = "Đã hủy",
                        Value = "Đã hủy"
                    });
                    break;

                case "Đang chuẩn bị hàng":
                    statuses.Add(new SelectListItem
                    {
                        Text = "Đang giao hàng",
                        Value = "Đang giao hàng"
                    });

                    statuses.Add(new SelectListItem
                    {
                        Text = "Đã hủy",
                        Value = "Đã hủy"
                    });
                    break;

                case "Đang giao hàng":
                    statuses.Add(new SelectListItem
                    {
                        Text = "Đã giao hàng",
                        Value = "Đã giao hàng"
                    });
                    break;
            }

            return statuses;
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
