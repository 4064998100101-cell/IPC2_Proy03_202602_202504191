using System;
using System.Dynamic;
namespace BackendSAT.Models
{
    public class DTE
    {
        public string Tiempo {get; set;}
        public string Referencia{get; set;}
        public string NitEmisor{get; set;}
        public string NitReceptor{get; set;}
        public decimal Valor{get; set;}
        public decimal Iva{get; set;}
        public decimal Total{get; set;}
        public string CodigoAutorizacion{get; set;}
        public void CalcularImpuestos()
        {
            Iva=Math.Round(Valor*0.12m,2);
            Total=Math.Round(Valor+Iva,2);
        }
        public void GenerarCodigoAutorizacion(int correlativoDia, DateTime fechaemision)
        {
            string yyyy=fechaemision.ToString("yyyy");
            string mm=fechaemision.ToString("MM");
            string dd=fechaemision.ToString("dd");
            string secuencial=correlativoDia.ToString("D8");
            
            CodigoAutorizacion=$"{yyy}{mm}{dd}{secuencial}";
        }
    }
}