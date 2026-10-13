using System;
namespace BackendSAT.Models
{
    public class Contribuyente
    {
        public string Nit{get; set;}
        public string Nombre {get; set;}
        public Contribuyente(string nit, string nombre)
        {
            Nombre=nombre;
            Nit=nit;
        }
        public static bool ValidarNit(string nit)
        {
            if(string.IsNullOrWhiteSpace(nit))
            {
                return false;
            }
            nit=nit.Trim().ToUpper();
            if(nit.Length<2)
            {
                return false;
            }
            string cuerpo=nit.Substring(0,nit.Length-1);
            char dvIngresado=nit[nit.Length-1];
            int suma=0;
            int posicion=cuerpo.Length+1;
            foreach(char c in cuerpo)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
                int digito=int.Parse(c.ToString());
                suma +=digito*posicion;
                posicion--;
            }
            int modulo11=suma%11;
            int paso4=11-modulo11;
            int modulo11Paso4=paso4 % 11;
            char dvCalculado;
            if(modulo11Paso4 ==10)
            {
                dvCalculado ='K';
            }
            else
            {
                dvCalculado=spectroscopiChar(modulo11Paso4);
            }
            return dvIngresado==dvCalculado;

        }
        private static char spectroscopiChar(int val)
        {
            return (char)('0' + val);
        }
    }
}