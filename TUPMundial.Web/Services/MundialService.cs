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

            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return new List<Partido>();

            var json = await resp.Content.ReadAsStringAsync();
            var paginado = JsonSerializer.Deserialize<PaginadoRespuesta>(json, _json);
            return paginado?.Datos ?? new List<Partido>();
        }

        public List<Partido> ObtenerPartidos(string? grupo = null)
            => ObtenerPartidosAsync(grupo).GetAwaiter().GetResult();

        public async Task<Partido?> ObtenerPartidoPorIdAsync(string id)
        {
            var resp = await _http.GetAsync($"{BASE}/partido/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Partido>(json, _json);
        }

        public Partido? ObtenerPartidoPorId(int id)
            => ObtenerPartidoPorIdAsync(id.ToString()).GetAwaiter().GetResult();

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

        // Trae los tickets del usuario y los enriquece con los datos
        // del partido correspondiente (equipos, fecha, estadio, grupo),
        // ya que la API solo guarda PartidoId / NumeroPartido, no esos datos.
        public List<Ticket> ObtenerTicketsUsuario(string email)
        {
            try
            {
                var resp = _http.GetAsync($"{BASE}/ticket?email={Uri.EscapeDataString(email)}").GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) return new List<Ticket>();

                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var tickets = JsonSerializer.Deserialize<List<Ticket>>(json, _json) ?? new List<Ticket>();

                if (tickets.Count == 0) return tickets;

                var partidos = ObtenerPartidos();

                foreach (var t in tickets)
                {
                    var partido = partidos.FirstOrDefault(p => p.NumeroPartido == t.NumeroPartido);
                    if (partido == null) continue;

                    t.Equipo1 = partido.Equipo1;
                    t.Equipo2 = partido.Equipo2;
                    t.Fecha   = partido.Fecha;
                    t.Estadio = partido.Estadio;
                    t.Grupo   = partido.Grupo;

                    // Nota: la API no persiste la cantidad de entradas comprada,
                    // solo el precio total. Por eso se asume 1 como valor por
                    // defecto. Si necesitás que la cantidad real se guarde y
                    // se muestre correctamente, hay que agregar esa propiedad
                    // en el modelo Ticket del lado de la API.
                    if (t.Cantidad <= 0) t.Cantidad = 1;
                }

                return tickets;
            }
            catch { return new List<Ticket>(); }
        }

        // Devuelve true si la compra se registró correctamente.
        // Si falla (por ejemplo, sector inválido según la API), error
        // contiene el detalle para mostrarlo al usuario en vez de fallar en silencio.
        public bool ComprarTicket(string email, int partidoId, string sector, int cantidad, decimal precioUnit, out string? error)
        {
            error = null;
            // Importante: partidoId acá es el NumeroPartido (1-104), no el _id de Mongo.
            // Por eso se busca en la lista completa filtrando por NumeroPartido,
            // en vez de usar ObtenerPartidoPorId (que busca por _id de Mongo).
            var partido = ObtenerPartidos().FirstOrDefault(p => p.NumeroPartido == partidoId);
            if (partido == null)
            {
                error = "No se encontró el partido seleccionado.";
                return false;
            }

            try
            {
                var body = JsonSerializer.Serialize(new
                {
                    partidoId      = partido.Id,
                    numeroPartido  = partidoId,
                    nombreUsuario  = email.Split('@')[0],
                    emailComprador = email,
                    sector,
                    precio         = precioUnit * cantidad,
                    fechaCompra    = DateTime.UtcNow
                });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = _http.PostAsync($"{BASE}/ticket", content).GetAwaiter().GetResult();

                if (!resp.IsSuccessStatusCode)
                {
                    var respBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    error = $"La API rechazó la compra ({(int)resp.StatusCode}): {respBody}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"Error de conexión con la API: {ex.Message}";
                return false;
            }
        }

        // ── Clases auxiliares para deserializar ───────────────────
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
