using CheZa_Core.Models;
using CheZa_Core.Models.Entities;
using CheZa_Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CheZa_Core.Controllers
{
    public class CartController : Controller
    {
        private readonly IIikoApiServiceModel _iikoApiServiceModel;

        public CartController(IIikoApiServiceModel iikoApiServiceModel)
        {
            _iikoApiServiceModel = iikoApiServiceModel;
        }

        // Получение корзины
        public async Task<IActionResult> GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart") ?? new CartViewModel();
            Console.WriteLine("Cart items count: " + cart.Items.Count);

            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");

            AllCartViewModel allCartViewModel = new AllCartViewModel(cart, menu);

            string city = HttpContext.Session.GetString("selectCity") ?? "Отрадное";

            double amount = GetAmountCart(cart);

            switch(city)
            {
                case "Отрадное":
                    if (amount < 700)
                        allCartViewModel.priceDelivery = 70;
                    break;
                case "Никольское":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
                case "Павлово":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
                case "СТ Союз-Чернобыль":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
                case "СНТ Огородник":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
                case "ТВЛПХ Деревня Львовские лужки":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
                case "садоводческий массив Ново-Анненское-Медное":
                    if (amount < 1500)
                        allCartViewModel.priceDelivery = 150;
                    break;
            }

            return PartialView("_CartModal", allCartViewModel);
        }

        // Добавление товара в корзину
        [HttpPost]
        public IActionResult AddToCart([FromBody] OrderItemModel orderItem)
        {
            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart") ?? new CartViewModel();

            var product = GetProductById(orderItem.ProductId);
            var modifiers = GetModifiersByIds(orderItem.SelectedModifiersId);
            Console.WriteLine("Try add to cart. Product id: " + orderItem.ProductId);

            cart.AddItem(product, modifiers, orderItem.Comment);

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Json(new { success = true, cartItemCount = cart.GetTotalItemCount(), cart = cart });
        }

        // Добавление количества
        [HttpPost]
        public IActionResult IncreaseQuantity([FromBody] OrderItemModel orderItem)
        {
            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart");
            List<Product> modifiers;

            var product = GetProductById(orderItem.ProductId);
            if (orderItem.SelectedModifiersId != null)
            {
                modifiers = GetModifiersByIds(orderItem.SelectedModifiersId);
            }
            else
            {
                modifiers = new List<Product>();
            }

            cart.IncreaseQuantity(product, modifiers);

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Json(new { success = true, cart = cart });
        }

        // Уменьшение количества
        [HttpPost]
        public IActionResult DecreaseQuantity([FromBody] OrderItemModel orderItem)
        {
            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart");
            Console.WriteLine("Cart: " + cart);
            List<Product> modifiers;

            var product = GetProductById(orderItem.ProductId);
            if (orderItem.SelectedModifiersId != null)
            {
                modifiers = GetModifiersByIds(orderItem.SelectedModifiersId);
            }
            else
            {
                modifiers = new List<Product>();
            }

            cart.DecreaseQuantity(product, modifiers);

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Json(new { success = true, cart = cart });
        }

        // Подтверждение заказа
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder([FromBody] ConfirmOrder confirmOrder)
        {
            var phoneNumber = User.Identity.Name;


            var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("Cart");

            Order order = new Order();
            using (var context = DbContextFactory.CreateDbContext())
            {
                //var user = context.User.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
                var user = context.User.FirstOrDefault(u => u.Email == phoneNumber);
                if (user == null || string.IsNullOrEmpty(user.PhoneNumber))
                {
                    return Json(new { success = false, message = "Авторизуйтесь" });
                }



                // Создание заказа
                //Console.WriteLine("User: " + HttpContext.Session.GetObjectFromJson<User>("currentUser").ToString());
                order.CustomerId = context.Customer.FirstOrDefault(c => c.UserId == user.UserId).CustomerId;
                order.OrderDate = DateTime.UtcNow;
                order.StatusId = 3;
                order.PaymentTypeId = 1;

                double totalAmount = 0;
                foreach (var item in cart.Items)
                {
                    double sum = 0;
                    sum += item.product.sizePrices[0].price.currentPrice;
                    foreach (var modifier in item.modifiers)
                    {
                        sum += modifier.sizePrices[0].price.currentPrice;
                    }
                    totalAmount += sum * item.quantity;
                }
                order.TotalAmount = totalAmount;
                order.AddressId = 1;

                context.Order.Add(order);

                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    // Поймайте внутреннее исключение и выведите его
                    var innerException = ex.InnerException;
                    while (innerException != null)
                    {
                        Console.WriteLine(innerException.Message);
                        innerException = innerException.InnerException;
                    }
                    return Json(new { success = false, message = "Error saving changes" });
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine(ex.Message);
                    return Json(new { success = false, message = "An unexpected error occurred" });
                }

                // Получение созданного заказа из бд
                //order = context.Order.Last();

                // Добавление товаров в заказ
                OrderItemModifier orderItemModifier;
                foreach (var item in cart.Items)
                {
                    OrderItem orderItem = new OrderItem();
                    orderItem.OrderId = order.OrderId;
                    orderItem.ProductId = item.product.id;
                    orderItem.Quantity = item.quantity;
                    orderItem.Comment = item.comment;

                    context.OrderItem.Add(orderItem);
                    try
                    {
                        context.SaveChanges();
                    }
                    catch (DbUpdateException ex)
                    {
                        // Поймайте внутреннее исключение и выведите его
                        var innerException = ex.InnerException;
                        while (innerException != null)
                        {
                            Console.WriteLine(innerException.Message);
                            innerException = innerException.InnerException;
                        }
                        return Json(new { success = false, message = "Error saving changes item" });
                    }
                    catch (Exception ex)
                    {
                        // Обработка других исключений
                        Console.WriteLine(ex.Message);
                        return Json(new { success = false, message = "An unexpected error occurred" });
                    }

                    DetachEntity(context, orderItem);

                    foreach (var modifier in item.modifiers)
                    {
                        orderItemModifier = new OrderItemModifier(); // Создаем один экземпляр OrderItemModifier
                        orderItemModifier.OrderItemId = orderItem.OrderItemId;
                        orderItemModifier.ModifierId = modifier.id;

                        Console.WriteLine("Modifier. OrderItemId: " + orderItemModifier.OrderItemId + ". ModifierId: " + orderItemModifier.ModifierId);

                        context.OrderItemModifier.Add(orderItemModifier);

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (DbUpdateException ex)
                        {
                            // Поймайте внутреннее исключение и выведите его
                            var innerException = ex.InnerException;
                            while (innerException != null)
                            {
                                Console.WriteLine(innerException.Message);
                                innerException = innerException.InnerException;
                            }
                            return Json(new { success = false, message = "Error saving changes add modifier" });
                        }
                        catch (Exception ex)
                        {
                            // Обработка других исключений
                            Console.WriteLine(ex.Message);
                            return Json(new { success = false, message = "An unexpected error occurred" });
                        }

                        DetachEntity(context, orderItemModifier); // Отсоединяем экземпляр после сохранения
                    }

                    DetachEntity(context, orderItem); // Отсоединяем экземпляр OrderItem после сохранения и добавления OrderItemModifier
                }

                Address address = HttpContext.Session.GetObjectFromJson<Address>("selectAddress") ?? new Address();
                string city = HttpContext.Session.GetString("selectCity") ?? "Отрадное";

                //DeliveryAddress deliveryAddress = new DeliveryAddress(address.StreetAddress, city, address.House, address.Apartment.ToString());
                DeliveryAddress deliveryAddress = new DeliveryAddress("Дружбы", "Отрадное", "2", "2");
                DeliveryPoint deliveryPoint = new DeliveryPoint(deliveryAddress, address.Comment);

                string fullAddress = city + ", " + address.StreetAddress + ", " + address.House;

                if (address.Entrance != null)
                {
                    fullAddress += ", подезд " + address.Entrance;
                }
                if (address.Apartment != null)
                {
                    fullAddress += ", кв. " + address.Apartment;
                }

                Console.WriteLine("Full address: " + fullAddress);
                Console.WriteLine("Send order to iiko");

                await _iikoApiServiceModel.GetToken();
                await _iikoApiServiceModel.GetOrganization();
                await _iikoApiServiceModel.GetTerminalGroup();
                await _iikoApiServiceModel.GetTableId();

                string deliveryType = HttpContext.Session.GetString("deliveryType") ?? "самовывоз";

                //await _iikoApiServiceModel.CreateOrder(cart, user, fullAddress, confirmOrder.orderComment, deliveryType);

                //ToDo...
                // Проверить работоспособность
                Console.WriteLine("Order comment in cart: " + confirmOrder.orderComment);
                await _iikoApiServiceModel.CreateDelivery(cart, user, deliveryPoint, "DeliveryByCourier", confirmOrder.orderComment);
            }

            Console.WriteLine("Successfully add order");
            cart = new CartViewModel();
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Json(new { success = true, message = "Successfully add order" });
        }

        private void DetachEntity(DbContext context, object entity)
        {
            var entry = context.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        // Получить продукт по id
        private Product GetProductById(string productId)
        {
            // Реализация получения продукта по его идентификатору
            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");
            foreach (Product product in menu._products)
            {
                if (product.id == productId)
                {
                    return product;
                }
            }
            return new Product();
        }

        // Получить модификаторы по id
        private List<Product> GetModifiersByIds(List<string> modifierIds)
        {
            // Реализация получения модификаторов по их идентификаторам

            var menu = HttpContext.Session.GetObjectFromJson<MenuResponseModel>("menu");

            if (menu == null || menu._products == null)
            {
                return new List<Product>();
            }

            var modifiers = menu._products
                        .Where(product => modifierIds.Contains(product.id))
                        .ToList();

            // удаление дублирующих модификаторов
            var modifiersToDelete = new List<Product>();
            foreach (Product modifier in modifiers)
            {
                if (modifier.name.Contains("добавка"))
                {
                    modifiersToDelete.Add(modifier);
                }
            }

            modifiers.RemoveAll(modifier => modifiersToDelete.Contains(modifier));
            return modifiers;
        }

        private double GetAmountCart(CartViewModel cart)
        {
            double totalAmount = 0;
            foreach (var item in cart.Items)
            {
                double amount = 0;
                amount += item.product.sizePrices[0].price.currentPrice;

                foreach (var modifier in item.modifiers)
                {
                    amount += modifier.sizePrices[0].price.currentPrice;
                }
                amount *= item.quantity;
                totalAmount += amount;
            }

            return totalAmount;
        }
    }

    public class ConfirmOrder
    {
        public string orderComment { get; set; }
    }
}
