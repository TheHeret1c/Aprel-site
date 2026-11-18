using CheZa_Core.Models;
using Microsoft.AspNetCore.Mvc;
using CheZa_Core.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CheZa_Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CheZa_Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IIikoApiServiceModel _iikoApiServiceModel;
        private string _name;

        public HomeController(ILogger<HomeController> logger, IIikoApiServiceModel iikoApiServiceModel)
        {
            _logger = logger;
            _iikoApiServiceModel = iikoApiServiceModel;
        }

        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart") ?? new CartViewModel();
            ViewBag.Cart = JsonConvert.SerializeObject(cart);

            List<Review> reviews = new List<Review>();
            var favouriteProductIds = new List<string>();
            var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
            var userName = "";

            using (var context = DbContextFactory.CreateDbContext())
            {
                reviews = context.Review.Include(c => c.Customer.User).ToList();
            }

            if (isAuthenticated)
            {
                userName = HttpContext.User.Identity.Name;
                Console.WriteLine("Authenticated");
                ViewBag.userName = userName;

                using (var context = DbContextFactory.CreateDbContext())
                {
                    //User user = context.User.FirstOrDefault(u => u.PhoneNumber == userName);
                    User user = context.User.FirstOrDefault(u => u.Email == userName);
                    Customer customer = context.Customer.FirstOrDefault(c => c.UserId == user.UserId);
                    favouriteProductIds = context.Favourite
                                                .Where(f => f.CustomerId == customer.CustomerId)
                                                .Select(f => f.ProductID)
                                                .ToList();
                }
            }

            //HttpContext.Session.SetString("apiToken", await _iikoApiServiceModel.GetToken());
            await _iikoApiServiceModel.GetToken();
            await _iikoApiServiceModel.GetOrganization();
            var menu = await _iikoApiServiceModel.GetMainMenu();

            HttpContext.Session.SetObjectAsJson("menu", menu);

            IndexViewModel indexViewModel = new IndexViewModel
            {
                menu = menu,
                cart = cart,
                userName = userName,
                favoriteProductIds = favouriteProductIds,
                reviews = reviews
            };

            return View(indexViewModel);
        }

        public async Task<IActionResult> MyAction()
        {
            await _iikoApiServiceModel.GetOrganization();
            TempData["Message"] = await _iikoApiServiceModel.GetMainMenu();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetails(string id)
        {

            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");

            var viewModel = await _iikoApiServiceModel.GetProductWithDetailsById(menu, id);

            Console.WriteLine("Find id: " + id + ". Find product id: " + viewModel._product.id);

            if (viewModel == null)
            {
                return NotFound();
            }

            if (viewModel._product.tags == null)
            {
                viewModel._product.tags = new string[0];
            }


            return PartialView("_ProductModal", viewModel);
        }

        [Authorize]
        [HttpPost]
        public IActionResult ToggleFavorite([FromBody] FavoriteRequest request)
        {
            using (var context = DbContextFactory.CreateDbContext())
            {
                var userPhoneNumber = User.Identity.Name;
                //User user = context.User.FirstOrDefault(u => u.PhoneNumber == userPhoneNumber);
                User user = context.User.FirstOrDefault(u => u.Email == userPhoneNumber);
                Customer customer = context.Customer.FirstOrDefault(c => c.UserId == user.UserId);
                var favorite = context.Favourite.FirstOrDefault(f => f.CustomerId == customer.CustomerId && f.ProductID == request.ProductId);

                if (favorite != null)
                {
                    context.Favourite.Remove(favorite);
                }
                else
                {
                    context.Favourite.Add(new Favourite
                    {
                        CustomerId = customer.CustomerId,
                        ProductID = request.ProductId,
                        DateAdd = DateTime.UtcNow
                    });
                }

                try
                {
                    context.SaveChanges();
                    return Json(new { success = true });
                } catch (Exception ex)
                {
                    Console.WriteLine("Error save favourite. Error: " + ex.Message);
                    return Json(new { success = false });
                }
            }
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            return PartialView("_CartModal");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Police()
        {
            return View();
        }
    }

    public class FavoriteRequest
    {
        public string ProductId { get; set; }
    }
}
