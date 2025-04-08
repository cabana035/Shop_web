using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Shop_web.Models.Db;
using Shop_web.Models.ViewModels;
using System.Security.Claims;
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
            user.Email=user.Email?.Trim();
            user.Password=user.Password?.Trim();
            user.FullName = user.FullName?.Trim();
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

        [HttpPost]
        public IActionResult Login(LoginViewModel user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            var founduser=_context.Users.FirstOrDefault(x=>x.Email==user.Email.Trim()&&x.Password==user.Password.Trim());
            if (founduser == null)
            {
                ModelState.AddModelError("Email", "Email or password is not valid!");
                return View(user);
            }
            //create claims
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, founduser.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, founduser.FullName));
            claims.Add(new Claim(ClaimTypes.Email, founduser.Email));
            //-----------
            if (founduser.IsAdmin == true)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var identity=new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);
            var principal=new ClaimsPrincipal(identity);
            //sign in the user
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal);

            return Redirect("/");
        }
        public IActionResult logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
