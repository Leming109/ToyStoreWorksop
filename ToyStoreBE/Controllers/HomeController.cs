using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ToyStoreBE.Models;
using ToyStoreBE.Models.MyModel;
using System.Data.Entity;

namespace ToyStoreBE.Controllers
{
    public class HomeController : Controller
    {
        private ToyStoreBEEntities db = new ToyStoreBEEntities();
        public ActionResult Index(string searchTerm, int? categoryId, string category)
        {
            var categories = db.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            // Nếu link truyền tên danh mục thì tìm CategoryID
            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = categories.FirstOrDefault(c =>
                    string.Equals(
                        c.CategoryName,
                        category.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (selectedCategory != null)
                {
                    categoryId = selectedCategory.CategoryID;
                }
                else if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = category;
                }
            }

            // Lấy sản phẩm và thông tin danh mục
            var allProducts = db.Products
                .Include(p => p.Category)
                .ToList();

            // 10 sản phẩm mới nhất
            var latestProducts = allProducts
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy thứ hạng sản phẩm bán chạy từ OrderDetail
            var salesRanking = db.OrderDetails
                .GroupBy(d => d.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    TotalQuantity = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ThenBy(x => x.ProductID)
                .ToList();

            // Ghép thứ hạng với thông tin sản phẩm
            var productDictionary = allProducts.ToDictionary(
                p => p.ProductID);

            var bestSellingProducts = salesRanking
                .Where(s => productDictionary.ContainsKey(s.ProductID))
                .Select(s => productDictionary[s.ProductID])
                .ToList();

            // Tạo danh sách sản phẩm riêng cho từng danh mục
            var categoryProducts = categories.Select(c =>
                new HomeCategoryProductsViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,

                    LatestProducts = allProducts
                        .Where(p => p.CategoryID == c.CategoryID)
                        .OrderByDescending(p => p.ProductID)
                        .Take(10)
                        .ToList(),

                    BestSellingProducts = bestSellingProducts
                        .Where(p => p.CategoryID == c.CategoryID)
                        .Take(10)
                        .ToList()
                })
                .ToList();

            // Danh sách bộ sưu tập
            var collections = new List<CollectionCardViewModel>
            {
                new CollectionCardViewModel
                {
                    Name = "Lamborghini",
                    SearchTerm = "Lamborghini",
                    ImagePath = "~/Content/images/collections/lamborghini.jpg"
                },
                new CollectionCardViewModel
                {
                    Name = "Mercedes-Benz",
                    SearchTerm = "Mercedes",
                    ImagePath = "~/Content/images/collections/mercedes.jpg"
                },
                new CollectionCardViewModel
                {
                    Name = "BlindBox",
                    SearchTerm = "Blind Box",
                    ImagePath = "~/Content/images/collections/blindbox.jpg"
                },
                new CollectionCardViewModel
                {
                    Name = "Xe cổ",
                    SearchTerm = "Xe cổ",
                    ImagePath = "~/Content/images/collections/xe-co.jpg"
                },
                new CollectionCardViewModel
                {
                    Name = "Ducati",
                    SearchTerm = "Ducati",
                    ImagePath = "~/Content/images/collections/ducati.jpg"
                },
                new CollectionCardViewModel
                {
                    Name = "Pop Mart",
                    SearchTerm = "Pop Mart",
                    ImagePath = "~/Content/images/collections/popmart.jpg"
                }
            };

            var model = new HomeIndexViewModel
            {
                Categories = categories,
                LatestProducts = latestProducts,
                BestSellingProducts = bestSellingProducts.Take(10).ToList(),
                CategoryProducts = categoryProducts,
                Collections = collections,
                SearchTerm = searchTerm,
                IsSearchMode =
                    !string.IsNullOrWhiteSpace(searchTerm) ||
                    categoryId.HasValue
            };

            // Nếu đang tìm kiếm hoặc chọn một danh mục
            if (model.IsSearchMode)
            {
                IEnumerable<Product> results = allProducts;

                if (categoryId.HasValue)
                {
                    results = results.Where(p =>
                        p.CategoryID == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string keyword = searchTerm.Trim();

                    results = results.Where(p =>
                        (!string.IsNullOrEmpty(p.ProductName) &&
                         p.ProductName.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >= 0)

                        ||

                        (!string.IsNullOrEmpty(p.ProductDecription) &&
                         p.ProductDecription.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >= 0)

                        ||

                        (p.Category != null &&
                         !string.IsNullOrEmpty(p.Category.CategoryName) &&
                         p.Category.CategoryName.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    );
                }

                model.SearchResults = results.ToList();
            }

            return View(model);
        }

        public ActionResult Search(string searchTerm)
        {
            return RedirectToAction("Index", new
            {
                searchTerm = searchTerm
            });
        }

        public ActionResult NewProduct()
        {
            var products = db.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductID)
                .ToList();

            return View(products);
        }

        public ActionResult BestSaleProduct()
        {
            var salesRanking = db.OrderDetails
                .GroupBy(d => d.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    TotalQuantity = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ThenBy(x => x.ProductID)
                .ToList();

            var ids = salesRanking
                .Select(x => x.ProductID)
                .ToList();

            var products = db.Products
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.ProductID))
                .ToList();

            var productDictionary = products.ToDictionary(
                p => p.ProductID);

            var bestSellingProducts = ids
                .Where(id => productDictionary.ContainsKey(id))
                .Select(id => productDictionary[id])
                .ToList();

            return View(bestSellingProducts);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            // Lấy sản phẩm hiện tại
            var product = db.Products.FirstOrDefault(
                p => p.ProductID == id.Value
            );

            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy tên danh mục
            string categoryName = db.Categories
                .Where(c => c.CategoryID == product.CategoryID)
                .Select(c => c.CategoryName)
                .FirstOrDefault();

            // Lấy các sản phẩm cùng danh mục, loại trừ sản phẩm đang xem
            var relatedCandidates = db.Products
                .Where(p =>
                    p.CategoryID == product.CategoryID &&
                    p.ProductID != product.ProductID)
                .ToList();

            // Xáo trộn ngẫu nhiên và chỉ lấy tối đa 10 sản phẩm
            var relatedProducts = relatedCandidates
                .OrderBy(p => Guid.NewGuid())
                .Take(10)
                .ToList();

            var model = new ProductDetailsVM
            {
                Product = product,
                CategoryName = categoryName,
                RelatedProducts = relatedProducts
            };

            return View(model);
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