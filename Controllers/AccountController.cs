using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Shop_web.Models.Db;
using Shop_web.Models.ViewModels;
using System.Net.Mail;
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
        public IActionResult RecoveryPassword()
        {
            
            return View();
        }
        [HttpPost]
        public IActionResult RecoveryPassword(RecoveryPasswordViewModel resetpassword)
        {
            if(!ModelState.IsValid) 
                {
                    return View();
                }
            Regex regex = new Regex((@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"));
            Match match = regex.Match(resetpassword.Email);
            if (!match.Success) 
                {
                ModelState.AddModelError("Email", "Emil is not vaid");
                return View(resetpassword);
                }
            var fuser=_context.Users.FirstOrDefault(x=>x.Email==resetpassword.Email.Trim());
            if (fuser==null) 
                {
                    ModelState.AddModelError("Email", "Email is not exist");
                return View(resetpassword);
                }
            fuser.RecoveryCode=new Random().Next(10000,100000);
            _context.Users.Update(fuser);
            _context.SaveChanges();
            MailMessage mail=new MailMessage();  
            SmtpClient smtp=new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("cabana3500@gmail.com");
            mail.To.Add(fuser.Email);
            mail.Subject = "Recovery password";
            mail.Body = "your recovery code:" + fuser.RecoveryCode;
            smtp.Port = 587;
            smtp.Credentials = new System.Net.NetworkCredential("cabana3500@gmail.com", "efqd bisv wuhq zczm");
            smtp.EnableSsl = true;  
            smtp.Send(mail);
            return Redirect("/Account/RessetPassword?email=" + fuser.Email);


        }
        public IActionResult ResetPassword(string email)
        {
            var resetPasswordModel = new ResetPasswordViewModel();
            resetPasswordModel.Email = email;
            return View(resetPasswordModel);
        }
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel resetPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPassword);
            }
            var fuser=_context.Users.FirstOrDefault(x=>x.Email==resetPassword.Email && x.RecoveryCode==resetPassword.RecoveryCode);
            if (fuser==null) 
                {
                   ModelState.AddModelError("RecoveryCode", "Email or recovery code is not valid");
                    return View(resetPassword);
                }
            fuser.Password=resetPassword.NewPassword;
            _context.Users.Update(fuser);
            _context.SaveChanges();

            return RedirectToAction("Login");

        }
    }
}
