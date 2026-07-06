using Microsoft.AspNetCore.Mvc;
using TUPMundial.Web.Services;

namespace TUPMundial.Web.Controllers
{
    public class PartidosController : Controller
    {
        private readonly MundialService _service;
        public PartidosController(MundialService service) => _service = service;

        private bool EstaLogueado() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioEmail"));

        // GET /Partidos
        public IActionResult Index(string? grupo)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");
            ViewBag.Nombre   = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.Grupos   = _service.ObtenerGrupos();
            ViewBag.GrupoSel = grupo ?? "Todos";
            var partidos     = _service.ObtenerPartidos(grupo);
            return View(partidos);
        }

        // GET /Partidos/Detalle/5
        public IActionResult Detalle(int id)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");
            var partido = _service.ObtenerPartidos()
                .FirstOrDefault(p => p.NumeroPartido == id);
            if (partido == null) return RedirectToAction("Index");
            return View(partido);
        }
    }
}
