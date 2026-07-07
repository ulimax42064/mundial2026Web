using System.Text;
using System.Text.Json;
using TUPMundial.Web.Models;

namespace TUPMundial.Web.Services
{
    public class MundialService
    {
        private readonly HttpClient _http;
        private const string BASE = "http://localhost:5123/api";
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public MundialService(HttpClient http)
        {
            _http = http;
        }

        // ── Partidos ──────────────────────────────────────────────
        public async Task<List<Partido>> ObtenerPartidosAsync(string? grupo = null)
        {
            var url = $"{BASE}/partido?porPagina=200";
            if (!string.IsNullOrEmpty(grupo) && grupo != "Todos")
                url += $"&grupo={Uri.EscapeDataString(grupo)}";

            try
            {
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<Partido>();
                var json = await resp.Content.ReadAsStringAsync();
                var paginado = JsonSerializer.Deserialize<PaginadoRespuesta>(json, _json);
                return paginado?.Datos ?? new List<Partido>();
            }
            catch { return new List<Partido>(); }
        }

        public List<Partido> ObtenerPartidos(string? grupo = null)
            => ObtenerPartidosAsync(grupo).GetAwaiter().GetResult();

        public async Task<Partido?> ObtenerPartidoPorNumeroAsync(int numero)
        {
            try
            {
                var resp = await _http.GetAsync($"{BASE}/partido/numero/{numero}");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Partido>(json, _json);
            }
            catch { return null; }
        }

        public Partido? ObtenerPartidoPorNumero(int numero)
            => ObtenerPartidoPorNumeroAsync(numero).GetAwaiter().GetResult();

        // Mantener compatibilidad — busca por NumeroPartido
        public Partido? ObtenerPartidoPorId(int id)
            => ObtenerPartidoPorNumero(id);

        public List<string> ObtenerGrupos()
        {
            var partidos = ObtenerPartidos();
            return partidos
                .Where(p => !string.IsNullOrEmpty(p.Grupo))
                .Select(p => p.Grupo!)
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }

        // ── Auth ──────────────────────────────────────────────────
        public bool Login(string email, string password, out Usuario? usuario)
        {
            usuario = null;
            try
            {
                var body = JsonSerializer.Serialize(new { email, password });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = _http.PostAsync($"{BASE}/usuario/login", content).GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) return false;
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                usuario = JsonSerializer.Deserialize<Usuario>(json, _json);
                return usuario != null;
            }
            catch { return false; }
        }

        public string? Registro(string nombre, string email, string password)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { nombre, email, password });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = _http.PostAsync($"{BASE}/usuario/registro", content).GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode) return null;
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var error = JsonSerializer.Deserialize<ErrorRespuesta>(json, _json);
                return error?.Error ?? "Error al registrar usuario.";
            }
            catch { return "Error de conexión con la API."; }
        }

        // ── Tickets ───────────────────────────────────────────────
        public List<Ticket> ObtenerTicketsUsuario(string email)
        {
            try
            {
                var resp = _http.GetAsync($"{BASE}/ticket?email={Uri.EscapeDataString(email)}").GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) return new List<Ticket>();
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonSerializer.Deserialize<List<Ticket>>(json, _json) ?? new List<Ticket>();
            }
            catch { return new List<Ticket>(); }
        }

        public void ComprarTicket(string email, int numeroPartido, string sector, int cantidad, decimal precioUnit)
        {
            try
            {
                var partido = ObtenerPartidoPorNumero(numeroPartido);
                if (partido == null) return;

                var body = JsonSerializer.Serialize(new
                {
                    partidoId      = partido.Id,
                    numeroPartido  = partido.NumeroPartido,
                    nombreUsuario  = email.Split('@')[0],
                    emailComprador = email,
                    sector,
                    precio         = precioUnit * cantidad,
                    fechaCompra    = DateTime.UtcNow
                });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                _http.PostAsync($"{BASE}/ticket", content).GetAwaiter().GetResult();
            }
            catch { }
        }

        // ── Clases auxiliares ─────────────────────────────────────
        private class PaginadoRespuesta
        {
            public List<Partido> Datos { get; set; } = new();
        }

        private class ErrorRespuesta
        {
            public string? Error { get; set; }
        }
    }
}
