using CheZa_Core.Models;
using CheZa_Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using CheZa_Core.Models.Entities;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace CheZa_Core.Controllers
{
    public class VerifySMSCodeRequest
    {
        public string Code { get; set; }
    }

    public class VerifyEmailCodeRequest
    {
        public string Code { get; set; }
    }

    public class AuthPhoneNumberRequest
    {
        public string PhoneNumber { get; set; }
    }

    public class AuthEmailRequest
    {
        public string Email { get; set; }
    }

    public class AuthController : Controller
    {
        private readonly EmailService _emailService;

        public AuthController(EmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetAuth()
        {
            return PartialView("_AuthModal");
        }

        public IActionResult GetAuthSMS(string phone)
        {
            string decodedPhone = WebUtility.UrlDecode(phone);
            ViewData["Phone"] = decodedPhone;
            HttpContext.Session.SetString("phoneNumber", decodedPhone);
            return PartialView("_AuthSMSModal");
        }

        public IActionResult GetAuthEmail(string email)
        {
            string decodedEmail = WebUtility.UrlDecode(email);
            ViewData["Email"] = decodedEmail;
            HttpContext.Session.SetString("email", decodedEmail);
            return PartialView("_AuthSMSModal");
        }

        [HttpPost]
        public async Task<IActionResult> VerifySMSCode([FromBody] VerifySMSCodeRequest request)
        {
            var expectedCode = HttpContext.Session.GetInt32("expectedCode");
            var phoneNumber = HttpContext.Session.GetString("phoneNumber");

            if (expectedCode == Convert.ToInt32(request.Code))
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                using (var context = DbContextFactory.CreateDbContext())
                {
                    Console.WriteLine("Try get user");
                    User user = context.User.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

                    if (user == null)
                    {
                        Console.WriteLine("Create user");
                        user = new User();
                        user.PhoneNumber = phoneNumber;
                        user.RoleId = 1;

                        context.User.Add(user);

                        try
                        {
                            context.SaveChanges();
                            Console.WriteLine("Save success");
                        } catch (Exception ex)
                        {
                            Console.WriteLine("Save unsuccess. Message: " + ex.Message);
                        }

                        Customer customer = new Customer();
                        //user = context.User.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
                        customer.UserId = user.UserId;
                        context.Customer.Add(customer);

                        try
                        {
                            context.SaveChanges();
                            Console.WriteLine("Save success");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Save unsuccess. Message: " + ex.Message);
                        }

                        HttpContext.Session.SetObjectAsJson("currentUser", user);
                    }
                    else
                    {
                        Console.WriteLine("User exists");
                        HttpContext.Session.SetObjectAsJson("currentUser", user);
                        Console.WriteLine("User: " + HttpContext.Session.GetObjectFromJson<User>("currentUser"));
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, phoneNumber)
                };

                var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) // устанавливаем время жизни в 1 час
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity), authProperties);

                return Json(new { success = true });
            }

            ModelState.AddModelError(string.Empty, "Неверный код");
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeRequest request)
        {
            var expectedCode = HttpContext.Session.GetString("expectedCode");
            var email = HttpContext.Session.GetString("email");

            if (expectedCode == request.Code)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                using (var context = DbContextFactory.CreateDbContext())
                {
                    Console.WriteLine("Try get user");
                    User user = context.User.FirstOrDefault(u => u.Email == email);

                    if (user == null)
                    {
                        Console.WriteLine("Create user");
                        user = new User();
                        user.Email = email;
                        user.RoleId = 1;

                        context.User.Add(user);

                        try
                        {
                            context.SaveChanges();
                            Console.WriteLine("Success create user");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Save unsuccess. Message: " + ex.Message);
                            Console.WriteLine("Inner exception: " + ex.InnerException.Message);
                        }
                        Console.WriteLine("Create customer");
                        Customer customer = new Customer();
                        //user = context.User.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
                        customer.UserId = user.UserId;
                        context.Customer.Add(customer);

                        try
                        {
                            context.SaveChanges();
                            Console.WriteLine("Success create customer");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Save unsuccess. Message: " + ex.Message);
                            Console.WriteLine("Inner exception: " + ex.InnerException.Message);
                        }

                        HttpContext.Session.SetObjectAsJson("currentUser", user);
                    }
                    else
                    {
                        Console.WriteLine("User exists");
                        HttpContext.Session.SetObjectAsJson("currentUser", user);
                        Console.WriteLine("User: " + HttpContext.Session.GetObjectFromJson<User>("currentUser"));
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email)
                };

                var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) // устанавливаем время жизни в 1 час
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity), authProperties);

                return Json(new { success = true });
            }

            ModelState.AddModelError(string.Empty, "Неверный код");
            return Json(new { success = false });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken] // Защита от CSRF
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Json(new { redirectToUrl = Url.Action("Index", "Home") }); // Возвращаем URL для перенаправления
        }

        //[HttpPost]
        //public IActionResult GenerateSmsCode([FromBody] AuthPhoneNumberRequest phoneNumber)
        //{
        //    var expectedCode = new Random().Next(0000, 9999);
        //    HttpContext.Session.SetInt32("expectedCode", expectedCode);
        //    //HttpContext.Session.SetString("phoneNumber", phoneNumber.PhoneNumber);
        //    Console.WriteLine("Success generate code. Code: " + expectedCode);
        //    return Json(new { success = "Happy happy happy!" });
        //}

        //[HttpPost]
        //public IActionResult GenerateSmsCode([FromBody] AuthPhoneNumberRequest phoneNumber)
        //{
        //    var code = GenerateRandomCode(4);
        //    HttpContext.Session.SetString("expectedCode", code);
        //    //HttpContext.Session.SetString("phoneNumber", phoneNumber.PhoneNumber);
        //    Console.WriteLine("Success generate code. Code: " + code);
        //    return Json(new { success = "Happy happy happy!" });
        //}

        [HttpPost]
        public async Task<IActionResult> GenerateEmailCode([FromBody] AuthEmailRequest email)
        {
            var code = GenerateRandomCode(4);
            Console.WriteLine("Email: " +  email.Email);

            try
            {
                await _emailService.SendEmailAsync(email.Email, "Подтверждение кода", $"Ваш код подтверждения: {code}");
                HttpContext.Session.SetString("expectedCode", code);
                Console.WriteLine("Success generate code. Code: " + code);
                return Json(new { success = "Happy happy happy!" });
            } catch (Exception  ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
