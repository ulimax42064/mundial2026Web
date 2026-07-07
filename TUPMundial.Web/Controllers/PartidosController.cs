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

        // GET /Partidos?grupo=Grupo A   ó   /Partidos?fase=32avos
        public IActionResult Index(string? grupo, string? fase)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");
            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.Grupos = _service.ObtenerGrupos();

            // El botón activo puede ser un grupo (Grupo A, Grupo B, ...) o una fase
            // eliminatoria (32avos, 16avos, Cuartos, Semifinal, Tercer Puesto, Final).
            ViewBag.GrupoSel = !string.IsNullOrEmpty(grupo) ? grupo
                              : !string.IsNullOrEmpty(fase)  ? fase
                              : "Todos";

            var partidos = _service.ObtenerPartidos(grupo, fase);
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
