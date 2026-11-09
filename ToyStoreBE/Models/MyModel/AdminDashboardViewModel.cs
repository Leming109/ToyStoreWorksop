using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ToyStoreBE.Models.MyModel
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }

        public int TotalCategories { get; set; }

        public int TotalCustomers { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalOrderValue { get; set; }

        public List<RecentOrderViewModel> RecentOrders { get; set; }
    }

    public class RecentOrderViewModel
    {
        public int OrderID { get; set; }

        public string CustomerName { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; }
    }
}