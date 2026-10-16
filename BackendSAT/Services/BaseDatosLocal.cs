using System;
using System.Collections.Generic;
using BackendSAT.Models;
namespace BackendSAT.Services
{
    public static class BaseDatosLocal
    {
        public static Dictionary<string,Contribuyente> RTU=new Dictionary<string, Contribuyente>();
        public static List<DTE> FacturasAutorizadas =new List<DTE>();
        public static List<object> LogErrores=new List<object>();

        private static string ultimoDiaCorrelativo=string.Empty;
        private static int contadordiario=0;

        public static int ObtenerSiguienteCorrelativo(DateTime fecha)
        {
            string diaActual=fecha.ToString("yyyyMMdd");
            if(ultimoDiaCorrelativo != diaActual)
            {
                ultimoDiaCorrelativo=diaActual;
                contadordiario=1;
            }
            else
            {
                contadordiario++;
            }
            return contadordiario;
        }
        public static void ResetearTodo()
        {
            RTU.Clear();
            FacturasAutorizadas.Clear();
            LogErrores.Clear();
            contadordiario=0;
            ultimoDiaCorrelativo=string.Empty;
        }

    }
}
