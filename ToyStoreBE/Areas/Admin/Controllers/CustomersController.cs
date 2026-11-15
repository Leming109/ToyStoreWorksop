
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using ToyStoreBE.Models;

namespace ToyStoreBE.Areas.Admin.Controllers
{
    public class CustomersController : AdminBaseController
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();

        // =========================================
        // GET: Admin/Customers
        // =========================================
        public ActionResult Index()
        {
            var customers = db.Customers
                .Include(c => c.User)
                .Where(c => c.User != null && c.User.UserRole == "C")
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View(customers);
        }

        // =========================================
        // GET: Admin/Customers/Details/5
        // =========================================
        // GET: Admin/Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy khách hàng và toàn bộ lịch sử mua hàng
            var customer = db.Customers
                .Include("User")
                .Include("Orders.OrderDetails.Product")
                .FirstOrDefault(c => c.CustomerID == id.Value);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
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
