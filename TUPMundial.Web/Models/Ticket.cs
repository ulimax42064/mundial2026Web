namespace TUPMundial.Web.Models
{
    public class Ticket
        {
                public int Id { get; set; }
                        public string UsuarioEmail { get; set; } = "";
                                public int PartidoId { get; set; }
                                        public string Equipo1 { get; set; } = "";
                                                public string Equipo2 { get; set; } = "";
                                                        public string Fecha { get; set; } = "";
                                                                public string Estadio { get; set; } = "";
                                                                        public string Grupo { get; set; } = "";
                                                                                public string Sector { get; set; } = "";
                                                                                        public int Cantidad { get; set; }
                                                                                                public decimal PrecioUnit { get; set; }
                                                                                                        public decimal Total { get; set; }
                                                                                                            }
                                                                                                            }