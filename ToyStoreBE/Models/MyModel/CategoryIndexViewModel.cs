using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace ToyStoreBE.Models.ViewModel
{
    public class CategoryIndexViewModel
    {
        public List<CategoryRowViewModel> Categories { get; set; }

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCategories { get; set; }
        public int TotalProducts { get; set; }
    }

    public class CategoryRowViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }
}