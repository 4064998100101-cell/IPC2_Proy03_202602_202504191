namespace Backend.Servicios
{
    public static class ValidarNit
    {
        public static bool EsnitValido(string nit)
        {
            nit=nit.Trim().ToUpper();
            
            if(nit.Length<2)return false;
            
            string cuerpo=nit.Substring(0, nit.Length-1);
            char dvIngresado=nit[nit.Length-1];

            if(cuerpo.Length< 1 || cuerpo.Length > 20)
            {
                return false;
            }
            foreach (char c in cuerpo)
            {
                if(!char.IsDigit(c))
                {
                    return false;
                }
            }
            int suma =0;
            int posicion=2;
            for(int i=cuerpo.Length -1; i>=0; i--)
            {
                int digito= int.Parse(cuerpo[i].ToString());
                suma += digito*posicion;
                posicion++;
            }
            int modulo =suma%11;
            int resta =11-modulo;
            int resultadoMod=resta%11;

            char dvCalculado;
            if(resultadoMod==10)
            {
                dvCalculado='K';
            }
            else
            {
                dvCalculado=resultadoMod.ToString()[0];

            }
            return dvIngresado== dvCalculado;
        }
    }
}