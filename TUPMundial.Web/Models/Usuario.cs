namespace TUPMundial.Web.Models
{
    public class Usuario
    {
        public string? Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string? Mensaje { get; set; }
    }
}
