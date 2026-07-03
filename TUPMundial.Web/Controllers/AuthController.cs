using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TUPMundial.Web.Services;

namespace TUPMundial.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly MundialService _service;
        public AuthController(MundialService service) => _service = service;

        // ── Helpers de validación ──────────────────────────────────
        private static bool EmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (email.Length > 100) return false;
            // Solo caracteres ASCII válidos para email, sin emojis ni caracteres raros
            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
                return false;
            return true;
        }

        private static bool NombreValido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;
            if (nombre.Length < 2 || nombre.Length > 50) return false;
            // Solo letras (incluyendo acentos), espacios y guiones
            if (!Regex.IsMatch(nombre, @"^[\p{L}\s\-']+$")) return false;
            return true;
        }

        private static string? ValidarPassword(string password, string? confirmar = null)
        {
            if (string.IsNullOrEmpty(password))
                return "La contraseña no puede estar vacía";
            if (password.Length < 8)
                return "La contraseña debe tener al menos 8 caracteres";
            if (password.Length > 100)
                return "La contraseña no puede superar los 100 caracteres";
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return "La contraseña debe tener al menos una mayúscula";
            if (!Regex.IsMatch(password, @"[0-9]"))
                return "La contraseña debe tener al menos un número";
            if (confirmar != null && password != confirmar)
                return "Las contraseñas no coinciden";
            return null;
        }

        // ── Login ──────────────────────────────────────────────────
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            email    = (email ?? "").Trim();
            password = password ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            { ViewBag.Error = "Completá todos los campos"; return View(); }

            if (!EmailValido(email))
            { ViewBag.Error = "El email no es válido (no se permiten emojis ni caracteres especiales)"; return View(); }

            if (password.Length > 100)
            { ViewBag.Error = "La contraseña supera el límite de caracteres"; return View(); }

            if (_service.Login(email, password, out var usuario))
            {
                HttpContext.Session.SetString("UsuarioEmail",  usuario!.Email);
                HttpContext.Session.SetString("UsuarioNombre", usuario!.Nombre);
                return RedirectToAction("Index", "Partidos");
            }

            ViewBag.Error = "Email o contraseña incorrectos";
            ViewBag.Email = email; // para no perder lo escrito
            return View();
        }

        // ── Registro ───────────────────────────────────────────────
        public IActionResult Registro() => View();

        [HttpPost]
        public IActionResult Registro(string nombre, string email, string password, string password2)
        {
            nombre    = (nombre    ?? "").Trim();
            email     = (email     ?? "").Trim();
            password  =  password  ?? "";
            password2 =  password2 ?? "";

            // Guardar valores para no perderlos si hay error
            ViewBag.Nombre = nombre;
            ViewBag.Email  = email;

            // Validar nombre
            if (!NombreValido(nombre))
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    ViewBag.ErrorNombre = "El nombre es obligatorio";
                else if (nombre.Length < 2)
                    ViewBag.ErrorNombre = "El nombre debe tener al menos 2 caracteres";
                else if (nombre.Length > 50)
                    ViewBag.ErrorNombre = "El nombre no puede superar los 50 caracteres";
                else
                    ViewBag.ErrorNombre = "El nombre solo puede contener letras y espacios";
            }

            // Validar email
            if (!EmailValido(email))
            {
                if (string.IsNullOrWhiteSpace(email))
                    ViewBag.ErrorEmail = "El email es obligatorio";
                else if (email.Length > 100)
                    ViewBag.ErrorEmail = "El email no puede superar los 100 caracteres";
                else
                    ViewBag.ErrorEmail = "Email inválido (solo letras, números y @. Sin emojis)";
            }

            // Validar contraseña
            var errorPass = ValidarPassword(password, password2);
            if (errorPass != null) ViewBag.ErrorPassword = errorPass;

            // Si hay algún error, volver al formulario
            if (ViewBag.ErrorNombre != null || ViewBag.ErrorEmail != null || ViewBag.ErrorPassword != null)
                return View();

            // Intentar registrar
            var errorRegistro = _service.Registro(nombre, email, password);
            if (errorRegistro != null)
            {
                ViewBag.ErrorEmail = errorRegistro;
                return View();
            }

            _service.Login(email, password, out var usuario);
            HttpContext.Session.SetString("UsuarioEmail",  usuario!.Email);
            HttpContext.Session.SetString("UsuarioNombre", usuario!.Nombre);
            return RedirectToAction("Index", "Partidos");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}