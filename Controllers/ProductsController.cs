using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult SearchProducts(string SearchText)
        {
            var products = _context.Products.Where(x =>
            EF.Functions.Like(x.Title, "%" + SearchText + "%") ||
            EF.Functions.Like(x.Tags, "%" + SearchText + "%") )
            .OrderBy(x=>x.Title).ToList();
            return View("Index",products);
        }
    }
}
