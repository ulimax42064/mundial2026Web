namespace TUPMundial.Web.Models
{
    public class Partido
    {
        public string? Id { get; set; }
        public int NumeroPartido { get; set; }
        public string Equipo1 { get; set; } = "";
        public string Equipo2 { get; set; } = "";
        public string? Fecha { get; set; }
        public string? Grupo { get; set; }
        public string Estadio { get; set; } = "";
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public decimal Precio { get; set; }
        public string Flags1 { get; set; } = "";
        public string Flags2 { get; set; } = "";
        public bool Definido { get; set; } = true;
    }
}
