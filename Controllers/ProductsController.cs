using Microsoft.AspNetCore.Mvc;
using Shop_web.Models.Db;

namespace Shop_web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly OnlineShopContext _context;
        public ProductsController(OnlineShopContext onlineShopContext)
        {
            _context = onlineShopContext;
        }
        public IActionResult Index()
        {
            var products = _context.Products.OrderByDescending(x => x.Id).ToList();
            return View(products);
        }
    }
}
