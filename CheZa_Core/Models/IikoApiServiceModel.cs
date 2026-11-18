using CheZa_Core.Models.Entities;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CheZa_Core.Models
{
    public interface IIikoApiServiceModel
    {
        Task<string> GetToken();
        Task RetrieveToken();
        Task GetOrganization();
        Task GetTerminalGroup();
        Task GetMenu();
        Task GetTableId();
        Task<MenuResponseModel> GetMainMenu();
        Task<ProductDetailsViewModel> GetProductById(string id);
        Task<ProductDetailsViewModel> GetProductWithDetailsById(MenuResponseModel menu, string id);
        Task<ProductDetailsViewModel> GetProductWithModifiersToOrder(MenuResponseModel menu, OrderItem orderProduct, List<OrderItemModifier> orderModifiers);
        Task CreateOrder(CartViewModel cart, User user, string address, string comment, string deliveryType);
        Task CreateDelivery(CartViewModel cart, User user, DeliveryPoint deliveryPoint, string deliveryType, string orderComment);
    }

    public class IikoApiServiceModel : IIikoApiServiceModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIikoSessionService _iikoSessionService;
        private readonly string _apiKey;
        private string _apiToken;
        private DateTime _tokenExpirationTime;
        private string _organizationId;
        private string _terminalGroupId;
        private string _tableId;
        private string _externalMenuId;
        private MenuResponseModel _menu;

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            // Указываем кодировку UTF-8
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            // Другие параметры настройки по вашему усмотрению
        };

        public IikoApiServiceModel(IHttpClientFactory httpClientFactory, IIikoSessionService iikoSessionService)
        {
            _httpClientFactory = httpClientFactory;
            _iikoSessionService = iikoSessionService;
        }

        public delegate Task ApiCallDelegate();

        private async Task<T> HandleResponse<T>(HttpResponseMessage response, ApiCallDelegate retryFunction) where T : class
        {
            string responseBody = await response.Content.ReadAsStringAsync();

            try
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        return JsonSerializer.Deserialize<T>(responseBody);
                        
                    case System.Net.HttpStatusCode.BadRequest:
                        var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);

                        await GetToken();
                        await retryFunction();
                        
                        break;
                    default:
                        Console.WriteLine("Unexpected error");
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                
                return null;
            }
        }

        public async Task<string> GetToken()
        {
            if (_apiToken == null || DateTime.Now >= _tokenExpirationTime) {
                await RetrieveToken();
            }
            return _apiToken;
        }

        public async Task RetrieveToken()
        {
            var requestData = new { apiLogin = _apiKey };
            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            using var client = _httpClientFactory.CreateClient();

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/access_token", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var responseData = JsonSerializer.Deserialize<AuthorizationResponse>(responseBody);
                    
                    Console.WriteLine("Successful");

                    _apiToken = responseData.token;
                    _tokenExpirationTime = DateTime.Now.AddHours(1);
                    //_iikoSessionService.SaveAccessToken(_apiToken, _tokenExpirationTime);
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Connection unsuccessful. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Connection unsuccessful. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);
                    break;
                default:
                    Console.WriteLine("Unexpected error");
                    break;
            }
        }

        public async Task GetOrganization()
        {
            //var token = _iikoSessionService.GetAccessToken();

            //if (DateTime.Now >= token.ExpirationTime)
            //{
            //    await GetToken();
            //}
            Console.WriteLine("Try get organization");

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);

            var requestData = new
            {
                organizationIds = new string[] { }, // Можно указать конкретные идентификаторы организаций или оставить пустым для получения всех
                returnAdditionalInfo = true,
                includeDisabled = false,
                returnExternalData = new string[] { } //{ "externalDataKey1", "externalDataKey2" }
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8 , "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/organizations", content);

            var responseData = await HandleResponse<OrganizationResponse>(response, GetOrganization);

            Console.WriteLine("Response Data organization: " + responseData);
            if (responseData != null)
            {
                Console.WriteLine("Successful get organization");
                Console.WriteLine("\n Response: \n" + JsonSerializer.Serialize(responseData));
                Console.WriteLine("Organization id: " + responseData.organizations[0].id + " name: " + responseData.organizations[0].name);
                _organizationId = responseData.organizations[0].id;
                Console.WriteLine("Organization: " +  _organizationId);
                //_iikoSessionService.SaveOrganizationId(_organizationId);
            }
        }

        public async Task GetMenu()
        {
            using var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-ru.iiko.services/api/2/menu");
            request.Headers.Add("Authorization", "Bearer " + _apiToken);

            HttpResponseMessage response = await client.SendAsync(request);
            
            string responseBody = await response.Content.ReadAsStringAsync();

            try
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        var responseData = JsonSerializer.Deserialize<ExternalMenuResponse>(responseBody);
                        Console.WriteLine("Successful get menu");
                        //Console.WriteLine("Id menu: " + responseData.externalMenus[0].id);
                        Console.WriteLine("Count menus: " + responseData.priceCategories.Count);
                        Console.WriteLine("Response: \n" + responseBody);
                        //_externalMenuId = responseData.externalMenus[0].id;
                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);
                        break;
                    default:
                        Console.WriteLine("Unexpected error");
                        break;
                }
            }
            catch (Exception ex) { Console.WriteLine("Error get menu: " + ex.Message); }
        }

        public async Task GetTerminalGroup()
        {
            //var token = _iikoSessionService.GetAccessToken();

            //if (DateTime.Now >= token.ExpirationTime)
            //{
            //    await GetToken();
            //}

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);

            var requestData = new
            {
                organizationIds = new string[] { _organizationId }
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/terminal_groups", content);

            string responseBody = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var responseData = JsonSerializer.Deserialize<TerminalGroupResponse>(responseBody);
                    Console.WriteLine("Successful get terminal");

                    _terminalGroupId = responseData.terminalGroups[0].items[0].id;
                    Console.WriteLine("Terminal: " + _terminalGroupId);
                    //_iikoSessionService.SaveTerminalGroupId(_terminalGroupId);
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);
                    break;
                default:
                    Console.WriteLine("Unexpected error");
                    break;
            }
        }

        public async Task GetTableId()
        {
            Console.WriteLine("Try get table id");
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);

            var requestData = new
            {
                terminalGroupIds = new string[] { _terminalGroupId },
                returnSchema = false
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine("Send get table");
            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/reserve/available_restaurant_sections", content);

            Console.WriteLine("Get response table");
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response body table: " + responseBody);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var responseData = JsonSerializer.Deserialize<RestarauntSectionsResponse>(responseBody);
                    Console.WriteLine("Successful get table");
                    Console.WriteLine("Response data: " + responseData);

                    Table table = responseData.restaurantSections
                        .SelectMany(section => section.tables)
                        .FirstOrDefault(table => table.number == 20);
                    _tableId = table.id;
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);
                    break;
                default:
                    Console.WriteLine("Unexpected error");
                    break;
            }
        }

        public async Task GetMenuItems()
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);

            var requestData = new
            {
                externalMenuId = _externalMenuId,
                organizationIds = new string[] { _organizationId }
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/2/menu/by_id", content);

            string responseBody = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var responseData = JsonSerializer.Deserialize<RetrieveExternalMenuResponse>(responseBody);
                    Console.WriteLine("Successful");

                    Console.WriteLine("Response: \n" + responseBody);

                    //ToDo...
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad.correlationId + "\n error description: " + responseDataBad.errorDescription + "\n error: " + responseDataBad.error);
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                    Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn.correlationId + "\n error description: " + responseDataUn.errorDescription + "\n error: " + responseDataUn.error);
                    break;
                default:
                    Console.WriteLine("Unexpected error");
                    break;
            }
        }

        public async Task CreateOrder(CartViewModel cart, User user, string address, string comment, string deliveryType)
        {
            Console.WriteLine("Try send order to iiko");
            //var token = _iikoSessionService.GetAccessToken();

            //if (DateTime.Now >= token.ExpirationTime)
            //{
            //    await GetToken();
            //}

            List<CreateOrderItem> items = new List<CreateOrderItem>();
            

            foreach (var item in cart.Items)
            {
                CreateOrderItem orderItem = new CreateOrderItem();
                List<CreateOrderItemModifier> modifiers = new List<CreateOrderItemModifier>();

                orderItem.productId = item.product.id;
                Console.WriteLine("Product id: " +  orderItem.productId + ". Price: " + item.product.sizePrices[0].price.currentPrice);
                
                foreach (var modifier in item.modifiers)
                {
                    CreateOrderItemModifier orderItemModifier = new CreateOrderItemModifier
                    {
                        productId = modifier.id,
                        amount = 1,
                        price = modifier.sizePrices[0].price.currentPrice
                    };

                    Console.WriteLine("Modifier id: " + orderItemModifier.productId + ". Price: " + orderItemModifier.price);

                    modifiers.Add(orderItemModifier);
                }

                orderItem.price = item.product.sizePrices[0].price.currentPrice;
                orderItem.amount = item.quantity;
                orderItem.comment = item.comment;
                orderItem.modifiers = modifiers;
                items.Add(orderItem);
            }

            items[items.Count - 1].comment += "\nКомм к заказу:\n" + comment + "\n" + deliveryType;

            var externalData = new[]
            {
                new
                {
                    key = "generalInfo",
                    value = "Телефон: " + user.PhoneNumber + ". Адрес: " + address,
                    isPublic = false
                }
            };

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestData = new
            {
                organizationId = _organizationId,
                terminalGroupId = _terminalGroupId,
                order = new
                {
                    tableIds = new string[] { _tableId },
                    items = items//,
                    //externalData = externalData
                }
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            Console.WriteLine("Request Data: " + jsonRequestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/order/create", content);

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response Body: " + responseBody);


            Console.WriteLine("Send order to iiko. Read response");
            try
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        Console.WriteLine("Successful");

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad?.correlationId + "\n error description: " + responseDataBad?.errorDescription + "\n error: " + responseDataBad?.error);
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn?.correlationId + "\n error description: " + responseDataUn?.errorDescription + "\n error: " + responseDataUn?.error);
                        break;
                    default:
                        Console.WriteLine("Unexpected error");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error create order: " + ex.Message);
            }
        }

        public async Task<MenuResponseModel> GetMainMenu()
        {
            Console.WriteLine("Get menu");
            var token = _iikoSessionService.GetAccessToken();

            if (DateTime.Now >= token.ExpirationTime)
            {
                await GetToken();
            }

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);

            var requestData = new
            {
                organizationId = _organizationId
            };

            var jsonRequestData = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/nomenclature", content);

            string responseBody = await response.Content.ReadAsStringAsync();

            try
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        var responseData = JsonSerializer.Deserialize<MainMenuResponse>(responseBody);
                        Console.WriteLine("Successful");

                        List<string> categories = new List<string>();
                        List<Product> products = new List<Product>();

                        if (responseData == null)
                            return new MenuResponseModel();

                        foreach (Product product in responseData.products)
                        {
                            if (!categories.Contains(product.seoKeywords))
                            {
                                if (product.seoKeywords != null)
                                {
                                    categories.Add(product.seoKeywords);
                                    Console.WriteLine("Add category: " + product.seoKeywords + "  product id: " + product.id);
                                }
                                
                            }
                            
                            products.Add(product);
                        }

                        Console.WriteLine("Successful get menu");
                        _menu = new MenuResponseModel(products, categories);
                        return _menu;
                    case System.Net.HttpStatusCode.BadRequest:
                        var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad?.correlationId + "\n error description: " + responseDataBad?.errorDescription + "\n error: " + responseDataBad?.error);
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn?.correlationId + "\n error description: " + responseDataUn?.errorDescription + "\n error: " + responseDataUn?.error);
                        break;
                    default:
                        Console.WriteLine("Unexpected error");
                        break;
                }

                return new MenuResponseModel();
            } catch (Exception ex)
            {
                Console.WriteLine("Error get main menu: " + ex.Message);
                return new MenuResponseModel();
            }
        }

        public async Task<ProductDetailsViewModel> GetProductById(string id)
        {
            if (_menu == null)
            {
                await GetMainMenu();
            }

            var product = _menu._products.FirstOrDefault(p => p.id == id);

            var modifiers = _menu._products
                .Where(p => product.modifiers.Any(modifier => modifier.id == p.id))
                .ToList();

            var modifiersToDelete = new List<Product>();
            foreach (Product modifier in modifiers)
            {
                if (modifier.name.Contains("добавка"))
                {
                    modifiersToDelete.Add(modifier);
                }
            }

            modifiers.RemoveAll(modifier => modifiersToDelete.Contains(modifier));

            var viewModel = new ProductDetailsViewModel
            {
                _product = product,
                _modifiers = modifiers
            };
            return viewModel;
        }

        public async Task<ProductDetailsViewModel> GetProductWithDetailsById(MenuResponseModel menu, string id)
        {
            var product = menu._products.FirstOrDefault(p => p.id == id);

            var modifiers = menu._products
                .Where(p => product.modifiers.Any(modifier => modifier.id == p.id))
                .ToList();

            var modifiersToDelete = new List<Product>();
            foreach (Product modifier in modifiers)
            {
                if (modifier.name.Contains("добавка"))
                {
                    modifiersToDelete.Add(modifier);
                }
            }

            modifiers.RemoveAll(modifier => modifiersToDelete.Contains(modifier));

            var viewModel = new ProductDetailsViewModel
            {
                _product = product,
                _modifiers = modifiers
            };
            return viewModel;
        }

        public async Task<ProductDetailsViewModel> GetProductWithModifiersToOrder(MenuResponseModel menu, OrderItem orderProduct, List<OrderItemModifier> orderModifiers)
        {
            var product = menu._products.FirstOrDefault(p => p.id == orderProduct.ProductId);

            List<Product> modifiers = new List<Product>();
            foreach (var orderModifier in orderModifiers)
            {
                modifiers.Add(menu._products.FirstOrDefault(p => p.id == orderModifier.ModifierId));
            }

            var viewModel = new ProductDetailsViewModel
            {
                _product = product,
                _modifiers = modifiers
            };

            return viewModel;
        }

        //ToDo...
        // Тестовая попытка перейти на Delivery
        // Попробовать нормально обработать ошибку от сервера (Request 400 и т.д.)

        public async Task CreateDelivery(CartViewModel cart, User user, DeliveryPoint deliveryPoint, string deliveryType, string orderComment)
        {
            Console.WriteLine("Try send order to iiko");

            List<CreateOrderItem> items = new List<CreateOrderItem>();

            // Добавление товаров в заказ
            foreach (var item in cart.Items)
            {
                CreateOrderItem orderItem = new CreateOrderItem();
                List<CreateOrderItemModifier> modifiers = new List<CreateOrderItemModifier>();

                orderItem.productId = item.product.id;
                Console.WriteLine("Product id: " + orderItem.productId + ". Price: " + item.product.sizePrices[0].price.currentPrice);

                foreach (var modifier in item.modifiers)
                {
                    CreateOrderItemModifier orderItemModifier = new CreateOrderItemModifier
                    {
                        productId = modifier.id,
                        amount = 1,
                        price = modifier.sizePrices[0].price.currentPrice
                    };

                    Console.WriteLine("Modifier id: " + orderItemModifier.productId + ". Price: " + orderItemModifier.price);

                    modifiers.Add(orderItemModifier);
                }

                orderItem.price = item.product.sizePrices[0].price.currentPrice;
                orderItem.amount = item.quantity;
                orderItem.comment = item.comment;
                orderItem.modifiers = modifiers;
                items.Add(orderItem);
            }

            // Создание заказа
            DeliveryCustomer deliveryCustomer = new DeliveryCustomer(user.FirstName + " " + user.LastName);
            DeliveryGuests deliveryGuests = new DeliveryGuests(1);
            Console.WriteLine("Address comment: " + deliveryPoint.comment + "\nOrder comment: " + orderComment);
            DeliveryOrder deliveryOrder = new DeliveryOrder(user.PhoneNumber, deliveryType, deliveryPoint, orderComment, deliveryCustomer, deliveryGuests, items);
            var requestData = new
            {
                organizationId = _organizationId,
                order = deliveryOrder
            };

            // Создание клиента и установка заголовка авторизации
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Сериализация запроса
            var jsonRequestData = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            Console.WriteLine("Request Data: " + jsonRequestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            // Отправка запроса
            HttpResponseMessage response = await client.PostAsync("https://api-ru.iiko.services/api/1/deliveries/create", content);

            // Чтение запроса
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response Body: " + responseBody);

            // Читаем ответ от сервера
            Console.WriteLine("Send order to iiko. Read response");
            try
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        Console.WriteLine("Successful");

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        var responseDataBad = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Bad request. \n Corelation id: " + responseDataBad?.correlationId + "\n error description: " + responseDataBad?.errorDescription + "\n error: " + responseDataBad?.error);
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        var responseDataUn = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        Console.WriteLine("Unauthorized. \n Corelation id: " + responseDataUn?.correlationId + "\n error description: " + responseDataUn?.errorDescription + "\n error: " + responseDataUn?.error);
                        break;
                    default:
                        Console.WriteLine("Unexpected error");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error create order: " + ex.Message);
            }
        }

    }

    public class ErrorResponse
    {
        public string correlationId { get; set; }
        public string errorDescription { get; set; }
        public string error { get; set; }
    }

    // Модель для организации
    public class OrganizationResponse
    {
        public string correlationId { get; set; }
        public List<Organization> organizations { get; set;}
    }

    public class Organization
    {
        public string responseType { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public List<ExternalData> externalData { get; set; }
    }

    public class ExternalData
    {
        public string key { get; set; }
        public string value { get; set; }
        public bool isPublic { get; set; }
    }

    // Модель для меню
    public class ExternalMenuResponse
    {
        public string correlationId { get; set;}
        public List<ExternalMenu> externalMenus { get; set;}
        public List<PriceCategorie> priceCategories { get; set;}
    }

    public class ExternalMenu
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class PriceCategorie
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    // Модель для терминальных групп
    public class TerminalGroupResponse
    {
        public string correlationId { get; set; }
        public List<TerminalGroup> terminalGroups { get; set; }
        public List<TerminalGroupInSleep> terminalGroupsInSleep { get; set; }
    }

    public class TerminalGroup
    {
        public string organizationId { get; set; }
        public List<Item> items { get; set; }
    }

    public class TerminalGroupInSleep
    {
        public string organizationId { get; set; }
        public List<Item> items { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string organizationId { get; set; }
        public string name { get; set; }
        public string timeZone { get; set; }
        public List<ExternalData> externalData { get; set; }
    }

    // Модель для секций зала
    public class RestarauntSectionsResponse
    {
        public string correlationId { get; set; }
        public List<RestarauntSection> restaurantSections { get; set; }
        public long revision { get; set; }
    }

    public class RestarauntSection
    {
        public string id { get; set; }
        public string terminalGroupId { get; set; }
        public string name { get; set; }
        public List<Table> tables { get; set; }
    }

    public class Table
    {
        public string id { get; set; }
        public int number { get; set; }
        public string name { get; set; }
        public int seatingCapacity { get; set; }
        public long revision { get; set; }
        public bool isDeleted { get; set; }
        public string posId { get; set; }
    }

    // Модель для авторизации
    public class AuthorizationResponse
    {
        public string corellationId { get; set; }
        public string token { get; set; }
    }

    // Модель для позиций меню
    public class RetrieveExternalMenuResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }

        public List<ItemCategory> itemCategories { get; set; }
    }

    public class ItemCategory
    {
        public List<MenuItem> items { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string buttonImageUrl { get; set; }
        public string headerImageUrl { get; set; }
    }

    public class MenuItem
    {
        public List<MenuItemSize> itemSizes { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<AllergenGroup> allergenGroups { get; set; }
        public string itemId { get; set; }
        public string modifierSchemaId { get; set; }
        public List<TaxCategory> taxCategory { get; set; }
        public string orderItemType { get; set; }
    }

    public class AllergenGroup
    {
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }

    public class TaxCategory
    {
        public string id { get; set; }
        public string name { get; set; }
        public float percentage { get; set; }
    }

    public class MenuItemSize
    {
        public List<Price> prices { get; set; }
        public List<ItemModifierGroup> itemModifierGroups { get; set; }
        public string sku { get; set; }
        public string sizeCode { get; set; }
        public string sizeName { get; set; }
        public bool isDefault { get; set; }
        public float portionWeightGrams { get; set; }
        public string sizeId { get; set; }
        public object nutritionPerHundredGrams { get; set; }
        public string buttonImageUrl { get; set; }
        public string[] buttonImageCroppedUrl { get; set; }
    }

    public class Price
    {
        public string organizationId { get; set; }
        public float price { get; set; }
    }

    public class ItemModifierGroup
    {
        public List<ModifierItem> items { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Restriction restrictions { get; set; }
        public bool canBeDivided { get; set; }
        public string itemGroupId { get; set; }
        public bool childModifiersHaveMinMaxRestrictions { get; set; }
        public string sku { get; set; }
    }

    public class ModifierItem
    {
        public List<Price> prices { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string buttonImage { get; set; }
        public Restriction restrictions { get; set; }
        public List<AllergenGroup> allergenGroups { get; set; }
        public object nutritionPerHundredGrams { get; set; }
        public float portionWeightGrams { get; set; }
        public List<Tag> tags { get; set; }
        public string itemId { get; set; }
    }

    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Restriction
    {
        public int minQuantity { get; set; }
        public int maxQuantity { get; set; }
        public int freeQuantity { get; set; }
        public int byDefault { get; set; }
    }

    // Модель основного меню

    public class MainMenuResponse
    {
        public string correlationId { get; set; }
        public List<Group> groups { get; set; }
        public List<ProductCategory> productCategories { get; set; }
        public List<Product> products { get; set; }
        public List<MainMenuSize> sizes { get; set; }
        public long revision { get; set; }
    }

    public class Group
    {
        public string[] imageLinks { get; set; }
        public string parentGroup { get; set; }
        public int order { get; set; }
        public bool isIncludedInMenu { get; set; }
        public bool isGroupModifier { get; set; }
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string additionalInfo { get; set; }
        public string[] tags { get; set; }
        public bool isDeleted { get; set; }
        public string seoDescription { get; set; }
        public string seoText { get; set; }
        public string seoKeywords { get; set; }
        public string seoTitle { get; set; }
    }

    public class ProductCategory
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool isDeleted { get; set; }
    }

    public class Product
    {
        public double fatAmount { get; set; }
        public double proteinsAmount { get; set; }
        public double carbohydratesAmount { get; set; }
        public double energyAmount { get; set; }
        public double fatFullAmount { get; set; }
        public double proteinsFullAmount { get; set; }
        public double carbohydratesFullAmount { get; set; }
        public double energyFullAmount { get; set; }
        public double weight { get; set; }
        public string groupId { get; set; }
        public string productCategoryId { get; set; }
        public string type { get; set; }
        public string orderItemType { get; set; }
        public string modifierSchemaId { get; set; }
        public string modifierSchemaName { get; set; }
        public bool splittable { get; set; }
        public string measureUnit { get; set; }
        public List<SizePrice> sizePrices { get; set; } // Переименовано здесь
        public List<Modifier> modifiers { get; set; }
        public List<GroupModifier> groupModifiers { get; set; }
        public string[] imageLinks { get; set; }
        public bool doNotPrintInCheque { get; set; }
        public string parentGroup { get; set; }
        public int order { get; set; }
        public string fullNameEnglish { get; set; }
        public bool useBalanceForSell { get; set; }
        public bool canSetOpenPrice { get; set; }
        public string paymentSubject { get; set; }
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string additionalInfo { get; set; }
        public string[] tags { get; set; }
        public bool isDeleted { get; set; }
        public string seoDescription { get; set; }
        public string seoText { get; set; }
        public string seoKeywords { get; set; }
        public string seoTitle { get; set; }
    }

    public class SizePrice
    {
        public string sizeId { get; set; }
        public MainMenuPrice price { get; set; }
    }

    public class MainMenuPrice
    {
        public double currentPrice { get; set; }
        public bool isIncludedInMenu { get; set; }
        public double? nextPrice { get; set; }
        public bool nextIncludedInMenu { get; set; }
        public string nextDatePrice { get; set; }
    }

    public class Modifier
    {
        public string id { get; set; }
        public int defaultAmount { get; set; }
        public int minAmount { get; set; }
        public int maxAmount { get; set; }
        public bool required { get; set; }
        public bool hideIfDefaultAmount { get; set; }
        public bool splittable { get; set; }
        public int freeOfChargeAmount { get; set; }
    }

    public class GroupModifier
    {
        public string id { get; set; }
        public int minAmount { get; set; }
        public int maxAmount { get; set; }
        public bool required { get; set; }
        public bool childModifiersHaveMinMaxRestrictions { get; set; }
        public List<Modifier> childModifiers { get; set; }
        public bool hideIfDefaultAmount { get; set; }
        public int defaultAmount { get; set; }
        public bool splittable { get; set; }
        public int freeOfChargeAmount { get; set; }
    }

    public class MainMenuSize
    {
        public string id { get; set; }
        public string name { get; set; }
        public int priority { get; set; }
        public bool isDefault { get; set; }
    }

    public class CreateOrderItem
    {
        public string productId { get; set; }
        public List<CreateOrderItemModifier> modifiers { get; set; } = new List<CreateOrderItemModifier>();
        public double price { get; set; }
        public string type { get; set; } = "Product";
        public double amount { get; set; }
        public string comment { get; set; }
    }

    public class CreateOrderItemModifier
    {
        public string productId { get; set; }
        public double amount { get; set; }
        public double price { get; set; }
    }

    public class CreateOrderResponse
    {
        public string correlationId { get; set; }
        public OrderInfo orderInfo { get; set; }
    }

    public class OrderInfo
    {
        public string id { get; set; }
        public string posId { get; set; }
        public string externalNumber { get; set; }
        public string organizationId { get; set; }
        public Int64 timestamp { get; set; }
        public string creationStatus { get; set; }
        public ErrorInfo errorInfo { get; set; }
        public CreatedOrder order { get; set; }
    }

    public class ErrorInfo
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class CreatedOrder
    {
        public List<string> tableIds { get; set; }
        public CustomerGuest customer {  get; set; }
        public string phone { get; set; }
        public string status { get; set; }
        public string whenCreated { get; set; } 
    }

    public class CustomerGuest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string comment { get; set; }
        public string gender { get; set; }
        public bool inBlacklist { get; set; }
        public string blacklistReason { get; set; }
        public string birthdate { get; set; }
        public string type { get; set; }
    }

    //ToDo...
    // Тестовая попытка перейти на Delivery

    //public class DeliveryCreate
    //{
    //    public string organizationId { get; set; }
    //    public Order order {  get; set; }
    //}

    public class DeliveryOrder
    {
        public string phone { get; set; }
        public string orderServiceType { get; set; } // DeliveryByCourier or DeliveryByClient
        public DeliveryPoint deliveryPoint { get; set; }
        public string comment { get; set; }
        public DeliveryCustomer customer { get; set; }
        public DeliveryGuests guests { get; set; }
        public List<CreateOrderItem> items { get; set; }

        public DeliveryOrder(string phone, string orderServiceType, DeliveryPoint deliveryPoint, string comment,
            DeliveryCustomer deliveryCustomer, DeliveryGuests deliveryGuests, List<CreateOrderItem> createOrderItems)
        {
            this.phone = phone;
            this.orderServiceType = orderServiceType;
            this.deliveryPoint = deliveryPoint;
            this.comment = comment;
            this.customer = deliveryCustomer;
            this.guests = deliveryGuests;
            this.items = createOrderItems;
        }

    }

    public class DeliveryCustomer
    {
        public string name { get; set; }
        public string type { get; set; } = "one-time";

        public DeliveryCustomer(string customerName)
        {
            name = customerName;
        }
    }

    public class DeliveryGuests
    {
        public int count { get; set; }

        public DeliveryGuests(int countGuests)
        {
            count = countGuests;
        }
    }

    public class DeliveryPoint
    {
        public DeliveryAddress address { get; set; }
        public string comment { get; set; }

        public DeliveryPoint(DeliveryAddress deliveryAddress, string comment)
        {
            this.address = deliveryAddress;
            this.comment = comment;
        }
    }

    public class DeliveryAddress
    {
        public DeliveryStreet street { get; set; }
        public string house { get; set; }
        public string doorphone { get; set; } // ограничение в 10 символов

        public DeliveryAddress(string street, string city, string house, string doorphone)
        {
            this.street = new DeliveryStreet(street, city);
            this.house = house;
            this.doorphone = doorphone;
        }
    }

    //public class DeliveryAddress
    //{
    //    public string line1 {  get; set; }
    //    public string doorphone { get; set; } // ограничение в 10 символов
    //    public string type { get; set; } = "city";

    //    public DeliveryAddress(string line1, string doorphone)
    //    {
    //        this.line1 = line1;
    //        this.doorphone = doorphone;
    //    }
    //}

    public class DeliveryStreet
    {
        public string name { get; set; } //Сделать ограничение в 60 символов!
        public string city { get; set; }

        public DeliveryStreet(string name, string city)
        {
            this.name = name;
            this.city = city;
        }
    }
}
