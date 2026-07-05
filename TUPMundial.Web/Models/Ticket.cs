namespace TUPMundial.Web.Models
{
    public class Ticket
    {
        public string? Id { get; set; }
        public string UsuarioEmail { get; set; } = "";
        public string EmailComprador { get; set; } = "";
        public string PartidoId { get; set; } = "";
        public int NumeroPartido { get; set; }
        public string Equipo1 { get; set; } = "";
        public string Equipo2 { get; set; } = "";
        public string? Fecha { get; set; }
        public string Estadio { get; set; } = "";
        public string? Grupo { get; set; }
        public string Sector { get; set; } = "";
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnit { get; set; }
        public decimal Precio { get; set; }
        public decimal Total => Precio;
        public string NombreUsuario { get; set; } = "";
    }
}
