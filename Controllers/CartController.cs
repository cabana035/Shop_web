using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Newtonsoft.Json;
using NuGet.Configuration;
using PayPal.Api;
using Shop_web.Models.Db;
using Shop_web.Models.ViewModels;
using System.Linq;
using System.Security.Claims;

namespace Shop_web.Controllers
{
    public class CartController : Controller
    {
        private OnlineShopContext _context;
        private IHttpContextAccessor _httpContextAccessor;
        IConfiguration _configuration;
        public CartController(OnlineShopContext onlineShopContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {

            _context = onlineShopContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            var result = GetProductsinCart();
            return View(result);
        }
        public IActionResult ClearCart()
        {
            Response.Cookies.Delete("Cart");
            return Redirect("/");
        }
        [HttpPost]
        public IActionResult UpdateCart([FromBody] CartViewModel request)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
            {
                return NotFound();
            }
            var cartItems = GetCartItems();
            var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);
            if (foundProductInCart == null)
            {
                var newCartItem = new CartViewModel() { };
                newCartItem.ProductId = request.ProductId;
                newCartItem.Count = request.Count;
                cartItems.Add(newCartItem);

            }
            else
            {
                if (request.Count > 0)
                {
                    foundProductInCart.Count = request.Count;
                }
                else
                {
                    cartItems.Remove(foundProductInCart);
                }
            }
            var json = JsonConvert.SerializeObject(cartItems);
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(7);
            Response.Cookies.Append("Cart", json, option);
            var result = cartItems.Sum(x => x.Count);
            return Ok(result);
        }
        public List<CartViewModel> GetCartItems()
        {
            List<CartViewModel> cartList = new List<CartViewModel>();
            var prevCartItemsString = Request.Cookies["Cart"];
            if (!string.IsNullOrEmpty(prevCartItemsString))
            {
                cartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemsString);
            }
            return cartList;
        }

        public List<ProductCartViewModel> GetProductsinCart()
        {
            var cartitems = GetCartItems();
            if (!cartitems.Any())
            {
                return null;
            }
            var cartItemProductIds = GetCartItems().Select(x => x.ProductId).ToList();
            //Load products into memory
            var products = _context.Products.Where(p => cartItemProductIds.Contains(p.Id)).ToList();

            //create the ProductCartviewmodel list
            List<ProductCartViewModel> result = new List<ProductCartViewModel>();
            foreach (var item in products)
            {
                var newItems = new ProductCartViewModel
                {
                    Id = item.Id,
                    ImageName = item.ImageName,
                    Price = item.Price - (item.Discount ?? 0),
                    Title = item.Title,
                    Count = cartitems.Single(x => x.ProductId == item.Id).Count,
                    RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartitems.Single(x => x.ProductId == item.Id).Count,
                };
                result.Add(newItems);
            }
            return result;

        }
        public IActionResult SmallCart()
        {
            var result = GetProductsinCart();
            return PartialView(result);

        }
        [Authorize]
        public IActionResult Checkout()
        {
            var order = new Models.Db.Order();
            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }
            ViewData["Products"] = GetProductsinCart();
            return View(order);

        }
        [Authorize]
        [HttpPost]
        public IActionResult ApplyCouponCode([FromForm] string couponCode)
        {
            var order = new Models.Db.Order();
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);
            if (coupon != null)
            {
                order.Couponcode = coupon.Code;
                order.CouponDiscount = coupon.Discount;
            }
            else
            {
                ViewData["Products"] = GetProductsinCart();
                TempData["message"] = "Coupon not exist";
                return View("Checkout", order);
            }
            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }
            ViewData["Products"] = GetProductsinCart();
            return View("Checkout", order);
        }
        /*
        [Authorize]
        [HttpPost]
         public IActionResult Checkout(Order order)
          {
              if (!ModelState.IsValid)
              {
                  ViewData["Products"] = GetProductsinCart() ;
                  return View(order) ;
              }
              //check and find coupon
              if (!string.IsNullOrEmpty(order.Couponcode))
              {
                  var coupon =_context.Coupons.FirstOrDefault(c=>c.Code==order.Couponcode);
                  if (coupon != null)
                  {
                      order.Couponcode = coupon.Code;
                      order.CouponDiscount = coupon.Discount;
                  }
                  else
                  {
                      TempData["message"] = "Coupon not exist";
                      ViewData["Products"]=GetProductsinCart();
                      return View(order);
                  }

              }

              var products = GetProductsinCart();
              order.Shipping=_context.Settings.First().Shipping;
              order.CreateDate = DateTime.Now;
              order.Subtotal = products.Sum(x => x.RowSumPrice);
              order.Total=(order.Subtotal + order.Shipping);
              order.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
              if (order.CouponDiscount != null)
              {
                  order.Total-=order.CouponDiscount.Value;
              }
              _context.Orders.Add(order);
              _context.SaveChanges();
              List<OrderDetail> orderDetails = new List<OrderDetail>();
              foreach (var item in products)
              {
                  OrderDetail orderDetailsItem = new OrderDetail()
                  {
                      Count = item.Count,
                      ProductId  =   item.Id,
                      ProductPrice=(decimal)item.Price,
                      ProductTitle=item.Title,
                      OrderId=order.Id,
                  };
                  orderDetails.Add(orderDetailsItem);


              }
              _context.OrderDetails.AddRange(orderDetails);
              _context.SaveChanges();
              return Redirect("Cart/RedirectToPayPal?orderId=" + order.Id);
          }
          public ActionResult RedirectToPayPal(int orderId)
          {
              var order = _context.Orders.Find(orderId);
              if (order == null)
              {
                  return View("PaymentFailed");
              }
              var orderDetails=_context.OrderDetails.Where(x=>x.OrderId == orderId).ToList();
              var clientId = _configuration.GetValue<string>("PayPal:Key");
              var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
              var mode = _configuration.GetValue<string>("PayPal:mode");
              var apiContext=PaypalConfifuration.getAPIContext(clientId, clientSecret, mode);
              try
              {
                  string baseURI = $"{Request.Scheme}://{Request.Host}/cart/PaypalReturn?";
                  var guid = Guid.NewGuid().ToString();
                  var payment = new Payment
                  {
                      intent = "Sale",
                      payee = new Payer { payment_method = "paypal" },
                      transactions = new List<Transaction>
                      {
                           new Transaction
                           {
                               description=$"Order{order.Id}",
                               invoice_number=Guid.NewGuid().ToString(),
                               amount=new Amount
                               {
                                   currency="USD",
                                   total=order.Total?.ToString("F"),
                                   //total="5.00"

                               },
                               item_list=new ItemList
                               {
                                   items=orderDetails.Select(p=>new Item
                                   {
                                       name=p.ProductTitle,
                                       currency="USD",
                                       price=p.ProductPrice.ToString("F"),
                                       quantity=p.Count.ToString(),
                                       sku=p.ProductId.ToString(),
                                   }).ToList(),
                               },

                           }

                  },
                      redirect_urls = new RedirectUrls
                      {
                          cancel_url = $"{baseURI}&Cancel=true",
                          return_url = $"{baseURI}orderId={order.Id}"
                      }
                  };

                  //add shipping price
                  payment.transactions[0].item_list.items.Add(new Item
                  {
                      name = "Shipping cost",
                      currency = "USD",
                      price = order.Shipping?.ToString("F"),
                      quantity = "1",
                      sku = "1"

                  });
                  var createdPayment = payment.Create(apiContext);
                  var approvalUrl = createdPayment.links.FirstOrDefault(l => l.rel.ToLower() == "approval_u");
                  _httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                  return Redirect(approvalUrl);
              }
              catch (Exception ex)
              {
                  return View("PaymentFailed");

              }
          }
        */


    }
}
