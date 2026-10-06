namespace Backend.Servicios
{
    public class GenerarAutorizacion
    {
        private static int ultimoCorrelativo=0;
        private static string fechaactualstr=string.Empty;
        public static string obtenerSiguienteCodigo(DateTime fechasolicitud)
        {
            string fechaFormato=fechasolicitud.ToString("yyyyMMdd");
            if(fechaactualstr != fechaFormato)
            {
                fechaFormato=fechaFormato;
                ultimoCorrelativo=1;
            }
            else
            {
                ultimoCorrelativo++;

            }
            return $"{fechaFormato}{ultimoCorrelativo:D8}";
        }
    }
}