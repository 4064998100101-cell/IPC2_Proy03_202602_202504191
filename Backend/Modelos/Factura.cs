using System.Text.Json.Serialization;

namespace Backend.Modelos
{
    public class ContribuyenteDto
    {
        public String NIT {get; set; }= string.Empty;
        public string Nombre{get; set;}=string.Empty;

    }
    public class Solicitud
    {
        public string tiempo {get; set;}=string.Empty;
        public string referencia{get; set;}=string.Empty;
        [JsonPropertyName("NIT EMISOR")]
        public string Nitemisor{get; set;}=string.Empty;
        [JsonPropertyName("NIT RECEPTOR")]
        public string Nitreceptor{get; set;}=string.Empty;

        public decimal valor {get; set;}
        public decimal Iva {get; set;}
        public decimal total {get; set;}

    }

    public class FacturaProcesada: Solicitud
    {
        public DateTime Fecha{get; set;}
        public string codigoAutorizacion{get; set;}
        public List<string> Errores {get; set;}=new();
        public bool esvalida => Errores.Count==0;
    }
}