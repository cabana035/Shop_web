using Microsoft.AspNetCore.Mvc;
using Shop_web.Models.Db;
using System.Text.RegularExpressions;

namespace Shop_web.Controllers
{
    public class AccountController : Controller
    {
        private readonly OnlineShopContext _context;
        public AccountController(OnlineShopContext context)
        {
            _context = context;
        }

        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {
            user.RegisterDate = DateTime.Now;
            user.IsAdmin = false;
            user.Email=user.Email.Trim();
            user.Password=user.Password.Trim();
            user.FullName = user.FullName.Trim();
            user.RecoveryCode = 0;
            if (!ModelState.IsValid)
            {
                return View(user);
            }

                Regex regex = new Regex((@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"));
                Match match = regex.Match(user.Email);
                if (!match.Success)
                {
                    ModelState.AddModelError("Email", "Email is not valid");
                    return View(user);
                }
                var preUser = _context.Users.Any(x=>x.Email==user.Email);
            if ((preUser==true))
            {
                ModelState.AddModelError("Email", "Email exists");
                return View(user);
            }
            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("login");
           
        }
        public IActionResult Login()
        {
            return View();
        }
    }
}
