using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;       // <-- nuevo
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace ServiPuntosUyAdmin.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _apiBaseUrl;

        // 1) Inyecta IConfiguration
        public AccountController(IConfiguration config)
        {
            _apiBaseUrl = config["API_BASE_URL"] 
                        ?? throw new InvalidOperationException("define API_BASE_URL");
        }

        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Remove("AdminLogged");
            HttpContext.Session.Remove("jwt_token");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string userType,
            string tenantName = null)
        {
            // 2) Usa _apiBaseUrl directo, sin localhost
            using var http = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };

            // Login
            var loginPayload = new { email, password };
            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginPayload),
                Encoding.UTF8,
                "application/json"
            );
            var loginRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/login"
            ) { Content = loginContent };

            loginRequest.Headers.Add("X-User-Type", userType);
            if ((userType == "Tenant" || userType == "Branch")
                && !string.IsNullOrEmpty(tenantName))
            {
                loginRequest.Headers.Add("X-Tenant-Name", tenantName);
            }

            var loginResp = await http.SendAsync(loginRequest);
            if (!loginResp.IsSuccessStatusCode)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Extraigo token
            var loginJson = await loginResp.Content.ReadAsStringAsync();
            var loginObj  = JObject.Parse(loginJson);
            var token     = loginObj["data"]?["token"]?.Value<string>();
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Error = "No se recibió token del servidor.";
                return View();
            }

            // consumo de Auth/me
            var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/me");
            meRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            var meResp = await http.SendAsync(meRequest);
            if (!meResp.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo verificar el tipo de usuario.";
                return View();
            }

            var meJson = await meResp.Content.ReadAsStringAsync();
            var meObj  = JObject.Parse(meJson);
            var data   = meObj["data"];
            if (data == null)
            {
                ViewBag.Error = "Respuesta inesperada del servidor.";
                return View();
            }

            int  userTypeReal  = data["userType"]?.Value<int>()   ?? 0;
            int? tenantIdValue = data["tenantId"]?.Value<int?>();
            int? branchIdValue = data["branchId"]?.Value<int?>();
            string nameValue   = data["name"]?.Value<string>()   ?? "";

            // Guardo en sesión
            var session = HttpContext.Session;
            session.SetString("AdminLogged", "true");
            session.SetString("jwt_token",   token);
            session.SetString("user_type",
                userTypeReal == 1 ? "admin_central" :
                userTypeReal == 2 ? "admin_tenant" :
                                    "admin_branch"
            );

            if (userTypeReal == 2 && tenantIdValue.HasValue)
                session.SetString("tenant_id", tenantIdValue.Value.ToString());

            if (userTypeReal == 3 && branchIdValue.HasValue)
                session.SetString("branch_id", branchIdValue.Value.ToString());

            session.SetString("AdminName", nameValue);

            if (userTypeReal == 2 && tenantIdValue.HasValue)
                return RedirectToAction("Index", "Product", new { id = tenantIdValue.Value });

            if (userTypeReal == 3 && branchIdValue.HasValue)
                return RedirectToAction("Hours", "Station", new { id = branchIdValue.Value });

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Signup(
            string email,
            string password,
            string name,
            string userType)
        {
            using var http = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };

            var payload = new { email, password, name };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/signup")
            { Content = content };
            request.Headers.Add("X-User-Type", userType);

            var response = await http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "¡Usuario registrado correctamente! Ahora podés iniciar sesión.";
                return RedirectToAction("Login");
            }
            else
            {
                ViewBag.Error = "No se pudo registrar el usuario. " + responseBody;
                return View("Login");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminLogged");
            HttpContext.Session.Remove("jwt_token");
            HttpContext.Session.Remove("AdminName");
            return RedirectToAction("Login");
        }
    }
}
