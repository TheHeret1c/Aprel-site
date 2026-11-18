using CheZa_Core.Data;
using Microsoft.AspNetCore.Mvc;
using CheZa_Core.Models;
using CheZa_Core.Extensions;
using CheZa_Core.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CheZa_Core.Controllers
{
    public class ProfileController : Controller
    {

        private readonly IIikoApiServiceModel _iikoApiServiceModel;

        public ProfileController(IIikoApiServiceModel iikoApiServiceModel)
        {
            _iikoApiServiceModel = iikoApiServiceModel;
        }

        [Authorize]
        public IActionResult Profile()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Получение имени пользователя
                string userName = User.Identity.Name;

                // Установка имени пользователя в ViewBag
                ViewBag.userName = userName;
            }
            else
            {
                ViewBag.userName = null; // Или любое другое значение по умолчанию
                RedirectToAction("Index", "Home");
            }
            User user;
            List<Order> orders = new List<Order>();
            List<Address> addresses = new List<Address>();
            List<string> favourites = new List<string>();
            using (var context = DbContextFactory.CreateDbContext())
            {
                //user = context.User.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
                user = context.User.FirstOrDefault(u => u.Email == User.Identity.Name);
                Console.WriteLine("Phone: " + User.Identity.Name);
                Customer customer = context.Customer.FirstOrDefault(c => c.UserId == user.UserId);

                orders = context.Order
                            .Where(o => o.CustomerId == context.Customer.FirstOrDefault(c => c.UserId == user.UserId).CustomerId)
                            .Include(o => o.OrderStatus) // Загружаем OrderStatus
                            .Include(o => o.OrderItem)  // Загружаем OrderItems
                                .ThenInclude(oi => oi.OrderItemModifier) // Загружаем OrderItemModifiers
                            .ToList();

                HttpContext.Session.SetObjectAsJson("Orders", orders);

                addresses = context.Address.Where(a => a.CustomerId == context.Customer.FirstOrDefault(c => c.UserId == user.UserId).CustomerId).ToList();

                favourites = context.Favourite
                                                .Where(f => f.CustomerId == customer.CustomerId)
                                                .Select(f => f.ProductID)
                                                .ToList();
                Console.WriteLine("User: " + user);
            }

            int addressId = HttpContext.Session.GetObjectFromJson<Address>("selectAddress") != null ? HttpContext.Session.GetObjectFromJson<Address>("selectAddress").AddressId : 0;

            

            ProfileViewModel vm = new ProfileViewModel
            {
                user = user,
                orders = orders,
                addresses = addresses,
                selectAddress = addressId,
                favourites = favourites,
                menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu")
            };

            return View("Profile", vm);
        }

        public IActionResult GetEditProfile()
        {
            // ToDo... Сделать проверку авторизации
            EditProfileViewModel editProfileViewModel = new EditProfileViewModel();
            using (var context = DbContextFactory.CreateDbContext())
            {
                //var user = context.User.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
                var user = context.User.FirstOrDefault(u => u.Email == User.Identity.Name);
                Console.WriteLine("Get edit user: " + user + ". Phone: " + user.PhoneNumber);
                editProfileViewModel.user = user;
            }
            return PartialView("_EditProfileModal", editProfileViewModel);
        }

        public IActionResult SaveProfile([FromBody] EditProfileViewResponse model)
        {
            if (model == null)
            {
                Console.WriteLine("Received null model");
                return Json(new { success = false, message = "Model is null" });
            }

            Console.WriteLine("Model received: ");
            Console.WriteLine($"FirstName: {model.firstName}");
            Console.WriteLine($"LastName: {model.lastName}");
            Console.WriteLine($"DateBirthday: {model.dateBirthday}");
            Console.WriteLine($"PhoneNumber: {model.phoneNumber}");
            //Console.WriteLine($"Email: {model.email}");

            if (ModelState.IsValid)
            {
                using (var context = DbContextFactory.CreateDbContext())
                {
                    //var user = context.User.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name); // Замените на идентификатор текущего пользователя
                    var user = context.User.FirstOrDefault(u => u.Email == User.Identity.Name); // Замените на идентификатор текущего пользователя
                    if (user != null)
                    {
                        user.FirstName = !string.IsNullOrEmpty(model.firstName) ? model.firstName : user.FirstName;
                        user.LastName = !string.IsNullOrEmpty(model.lastName) ? model.lastName : user.LastName;

                        if (!string.IsNullOrEmpty(model.dateBirthday))
                        {
                            if (DateTime.TryParse(model.dateBirthday, out DateTime dateValue))
                            {
                                user.DateBirthday = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc); // Конвертируем в UTC
                            }
                        }

                        user.PhoneNumber = !string.IsNullOrEmpty(model.phoneNumber) ? model.phoneNumber : user.PhoneNumber;
                        //user.Email = !string.IsNullOrEmpty(model.email) ? model.email : user.Email;

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Save error: " + ex.Message);
                            return Json(new { success = false, message = "Error saving data" });
                        }

                        return Json(new { success = true });
                    }

                    return Json(new { success = false, message = "User not found" });
                }
            }

            return Json(new { success = false, message = "Model state is invalid" });
        }

        public async Task<IActionResult> GetOrder(int orderId)
        {           
            // Меню из сессии
            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");

            // Заказы из сессии
            var order = HttpContext.Session.GetObjectFromJson<List<Order>>("Orders").FirstOrDefault(o => o.OrderId == orderId);
            OrderViewModel model = new OrderViewModel(); // Создание представления для вывода данных
            model.orderId = orderId;
            model.totalAmount = order.TotalAmount; 

            List<ProductOrderViewModel> products = new List<ProductOrderViewModel>(); // Лист итоговых продуктов для представления

            List<OrderItem> orderItems = new List<OrderItem>(); // Лист продуктов заказа из бд
            using (var context = DbContextFactory.CreateDbContext())
            {
                orderItems = context.OrderItem.Where(oi => oi.OrderId == orderId).ToList(); // Выгрузка продуктов заказа

                foreach (var item in orderItems)
                {
                    List<OrderItemModifier> orderItemModifiers = new List<OrderItemModifier>(); // Лист модификаторов для получения их как продуктов
                    orderItemModifiers = context.OrderItemModifier.Where(oim => oim.OrderItemId == item.OrderItemId).ToList();
                    var productDetails = await _iikoApiServiceModel.GetProductWithModifiersToOrder(menu, item, orderItemModifiers); // Получение структурированного продукта с его айди и выбранными модификаторами

                    var productOrder = new ProductOrderViewModel // Итоговый продукт для представления
                    {
                        product = productDetails,
                        quantity = item.Quantity
                    };
                    products.Add(productOrder);
                }

                Review review = context.Review.FirstOrDefault(r => r.OrderId == orderId);

                if (review != null)
                {
                    model.haveReview = true;
                }
                else
                {
                    model.haveReview = false;
                }
            }
            model.products = products;

            return PartialView("_OrderModal", model);
        }

        [Authorize]
        public IActionResult AddAddress(int addressId)
        {
            AddressViewModel addressViewModel = new AddressViewModel();
            if (addressId == 0)
            {
                Console.WriteLine("Address equal 0");
                addressViewModel.isEdit = false;
                return PartialView("_AddressAddModal", addressViewModel);
            }

            Address address = new Address();
            using (var context = DbContextFactory.CreateDbContext())
            {
                address = context.Address
                       .Include(a => a.City) // Явная загрузка сущности City
                       .FirstOrDefault(a => a.AddressId == addressId);
            }
            addressViewModel.addressId = addressId;
            addressViewModel.city = address.City.CityName;
            addressViewModel.streetAddress = address.StreetAddress;
            addressViewModel.house = address.House;
            addressViewModel.entrance = address.Entrance;
            addressViewModel.floor = address.Floor;
            addressViewModel.apartment = address.Apartment;
            addressViewModel.comment = address.Comment;
            addressViewModel.isEdit = true;
            return PartialView("_AddressAddModal", addressViewModel);
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteAddress([FromBody] AddressViewModel address)
        {
            using (var context = DbContextFactory.CreateDbContext())
            {
                var deleteAddress = context.Address.FirstOrDefault(a => a.AddressId == address.addressId);
                if (deleteAddress != null)
                {
                    context.Address.Remove(deleteAddress);
                } else
                {
                    return Json(new { success = false, message = "Адрес не найден" });
                }

                try
                {
                    context.SaveChanges();
                    Console.WriteLine("Success delete address!");
                } catch (Exception ex)
                {
                    Console.WriteLine("Delete unsuccess. Error: " + ex.Message);
                    Console.WriteLine("Inner exception: " + ex.InnerException.Message);
                }
            }

            return Json(new { success = true, message = "Адрес успешно удалён!" });
        }

        [Authorize]
        [HttpPost]
        public IActionResult SetAddress([FromBody] AddressViewModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Неверные данные" });
            }

            int cityId = 0;
            using (var context = DbContextFactory.CreateDbContext())
            {
                var cities = context.City.ToList();
                if (cities != null)
                {
                    var city = cities.FirstOrDefault(c => c.CityName != null && c.CityName.Contains(model.city));
                    if (city == null)
                    {
                        return Json(new { success = false, message = "Город не найден" });
                    }
                    cityId = city.CityId;
                }
                else
                {
                    return Json(new { success = false, message = "Не удалось загрузить список городов" });
                }
            }

            Address address;
            using (var context = DbContextFactory.CreateDbContext())
            {
                if (model.isEdit)
                {
                    address = context.Address.FirstOrDefault(a => a.AddressId == model.addressId);
                    if (address == null)
                    {
                        return Json(new { success = false, message = "Адрес не найден" });
                    }
                }
                else
                {
                    address = new Address();
                    context.Address.Add(address);
                }

                //User user = context.User.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
                User user = context.User.FirstOrDefault(u => u.Email == User.Identity.Name);
                Customer customer = context.Customer.FirstOrDefault(u => u.UserId == user.UserId);

                // Обновляем или добавляем данные адреса
                address.CustomerId = customer.CustomerId;
                address.StreetAddress = model.streetAddress;
                address.House = model.house;
                address.CityId = cityId;
                address.Entrance = model.entrance;
                address.Floor = model.floor;
                address.Apartment = model.apartment;
                address.Comment = model.comment;

                try
                {
                    context.SaveChanges();
                    return Json(new { success = true, message = model.isEdit ? "Адрес успешно обновлён!" : "Адрес успешно добавлен!" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error " + (model.isEdit ? "update" : "add") + " address: " + ex.ToString());
                    return Json(new { success = false, message = "Ошибка " + (model.isEdit ? "обновления" : "добавления") + " адреса!" });
                }
            }
        }

        [Authorize]
        public IActionResult GetAddresses()
        {
            List<Address> addresses = new List<Address>();
            using (var context = DbContextFactory.CreateDbContext())
            {
                //User user = context.User.FirstOrDefault(u => u.PhoneNumber == User.Identity!.Name)!;
                User user = context.User.FirstOrDefault(u => u.Email == User.Identity!.Name)!;
                Customer customer = context.Customer.FirstOrDefault(c => c.UserId == user.UserId)!;

                addresses = context.Address.Where(a => a.CustomerId == customer.CustomerId).
                    Include(a => a.City).ToList();
            }


            return PartialView("_SelectAddress", addresses);
        }

        [Authorize]
        public IActionResult SetSelectAddress([FromBody] int addressId)
        {
            Console.WriteLine("Address id: " + addressId);
            if (addressId == 0)
            {
                Console.WriteLine("Samovyvoz");
                HttpContext.Session.SetString("deliveryType", "самовывоз");
                HttpContext.Session.SetString("selectCity", "Отрадное");
                HttpContext.Session.SetObjectAsJson("selectAddress", new Address());
                ViewBag.AddressId = addressId;
                return Json(new { success = true, message = "Выбран самовывоз" });
            }
            else
            {
                Console.WriteLine("Delivery");
                using (var context = DbContextFactory.CreateDbContext())
                {
                    Address address = context.Address.Include(a => a.City)
                        .FirstOrDefault(a => a.AddressId == addressId);
                    HttpContext.Session.SetString("selectCity", address.City.CityName);
                    HttpContext.Session.SetObjectAsJson("selectAddress", address);
                    ViewBag.AddressId = addressId;
                }
                HttpContext.Session.SetString("deliveryType", "доставка");
                return Json(new { success = true, message = "Адрес успешно выбран!" });
            }
        }

        public IActionResult GetReviewModal(int orderId)
        {
            if (orderId == 0)
            {
                return PartialView("_AddReviewModal", 0);
            }

            return PartialView("_AddReviewModal", orderId);
        }

        [Authorize]
        [HttpPost]
        public IActionResult AddReview([FromBody] AddReviewResponseModel review)
        {
            using (var context = DbContextFactory.CreateDbContext())
            {
                User user = context.User.FirstOrDefault(u => u.Email == User.Identity.Name);
                Customer customer = context.Customer.FirstOrDefault(c => c.UserId == user.UserId);

                Review newReview = new Review
                {
                    CustomerId = customer.CustomerId,
                    OrderId = review.orderId != 0 ? review.orderId : null,
                    Rating = review.rating,
                    ReviewText = review.textReview,
                    ReviewDate = DateTime.Now.ToUniversalTime(),
                };

                context.Review.Add(newReview);

                //try
                //{
                //    context.SaveChanges();
                //    return Json(new { success = true, message = "Отзыв успешно добавлен!" });
                //} catch (Exception ex)
                //{
                //    Console.WriteLine("Ошибка добавления отзыва. Ошибка: " + ex.Message + "\nВнутреннее исключение: " + ex.InnerException);
                //    return Json(new { success = false, message = "Ошибка добавления отзыва. Ошибка: " + ex.Message + "\nВнутреннее исключение: " + ex.InnerException });
                //}

                try
                {
                    context.SaveChanges();
                    return Json(new { success = true, message = "Отзыв успешно добавлен!" });
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException;
                    while (innerException != null)
                    {
                        Console.WriteLine("Inner exception message: " + innerException.Message);
                        innerException = innerException.InnerException;
                    }

                    Console.WriteLine("Ошибка добавления отзыва. Ошибка: " + ex.Message);
                    return Json(new { success = false, message = "Ошибка добавления отзыва. Ошибка: " + ex.Message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка добавления отзыва. Ошибка: " + ex.Message);
                    return Json(new { success = false, message = "Ошибка добавления отзыва. Ошибка: " + ex.Message });
                }
            }
        }

        private async Task<ProductDetailsViewModel> GetProductById(string id)
        {
            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");

            var viewModel = await _iikoApiServiceModel.GetProductWithDetailsById(menu, id);

            if (viewModel == null)
            {
                return new ProductDetailsViewModel();
            }
            return viewModel;
        }
    }
}
