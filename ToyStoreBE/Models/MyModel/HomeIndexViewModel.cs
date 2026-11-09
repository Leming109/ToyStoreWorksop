    using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
namespace ToyStoreBE.Models.MyModel
{
    public class HomeIndexViewModel
    {
        public List<Category> Categories { get; set; }

        public List<Product> LatestProducts { get; set; }

        public List<Product> BestSellingProducts { get; set; }

        public List<HomeCategoryProductsViewModel> CategoryProducts { get; set; }

        public List<CollectionCardViewModel> Collections { get; set; }

        public List<Product> SearchResults { get; set; }

        public string SearchTerm { get; set; }

        public bool IsSearchMode { get; set; }
    }
    public class HomeCategoryProductsViewModel
    {
        public int CategoryID { get; set; }

        public string CategoryName { get; set; }

        public List<Product> LatestProducts { get; set; }

        public List<Product> BestSellingProducts { get; set; }
    }

    public class CollectionCardViewModel
    {
        public string Name { get; set; }

        public string SearchTerm { get; set; }

        public string ImagePath { get; set; }
    }
}