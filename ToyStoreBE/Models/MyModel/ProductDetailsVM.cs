using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ToyStoreBE.Models.MyModel
{
    public class ProductDetailsVM
    {
        public Product Product { get; set; }

        public string CategoryName { get; set; }

        public List<Product> RelatedProducts { get; set; }
    }
}