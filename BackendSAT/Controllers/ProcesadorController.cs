
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using BackendSAT.Models;
using BackendSAT.Services;
using System.Runtime.Versioning;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Linq;

namespace BackendSAT.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProcesarController: ControllerBase
    {
        [HttpPost("Procesar")]
        public IActionResult Procesar ([FromBody] JsonElement cuerpoPeticion)
        {
            try
            {
                string jsonString=cuerpoPeticion.GetRawText();
                if(jsonString.Contains("nombre") && jsonString.Contains("NIT"))
                {
                    var contribuyentes = JsonSerializer.Deserialize<List<ContribuyenteRequest>>(jsonString);
                    int nuevos=0;
                    int actualizados=0;
                    int invalidos=0;

                    foreach(var item in contribuyentes)
                      {
                        string nitLimpio = item.NIT?.Trim().ToUpper();
                        if(Contribuyente.ValidarNit(nitLimpio))
   
                       {
                         if(BaseDatosLocal.RTU.ContainsKey(nitLimpio))
                            {
                              BaseDatosLocal.RTU[nitLimpio].Nombre = item.nombre;
                              actualizados++;
                            }
                        else
                        {
                            BaseDatosLocal.RTU.Add(nitLimpio, new Contribuyente(nitLimpio, item.nombre));
                          nuevos++;
                        }
                       }
                      else
                       {
                        invalidos++;
                       }
                        }
                    var respuestaRtu=new
                    {
                        CONTRIBUYENTES_NUEVOS = nuevos,
                        CONTRIBUYENTES_ACTUALIZADOS = actualizados,
                        NITS_INVALIDOS = invalidos
                    };
                    return Ok(respuestaRtu);
                }
                else if(jsonString.Contains("TIEMPO") || jsonString.Contains("REFERENCIA"))
                {
                    var solicitudes = JsonSerializer.Deserialize<List<DteRequest>>(jsonString);
                    int aprobadas=0;
                    int rechazadasEmisorInexistente=0;
                    int rechazadasReceptorInexistente=0;
                    var detalleAprobaciones = new List<object>();
                    var referenciasVistas = new HashSet<string>();

                    foreach (var req in solicitudes)
                    {
                        bool emisorValido = Contribuyente.ValidarNit(req.NIT_EMISOR);
                        bool receptorValido = Contribuyente.ValidarNit(req.NIT_RECEPTOR);
                        bool emisorExiste = BaseDatosLocal.RTU.ContainsKey(req.NIT_EMISOR);
                        bool receptorExiste = BaseDatosLocal.RTU.ContainsKey(req.NIT_RECEPTOR);

                        if(!emisorValido || !emisorExiste)
                        {
                            rechazadasEmisorInexistente++;
                            continue;
                        }
                        if(!receptorValido || !receptorExiste)
                        {
                            rechazadasReceptorInexistente++;
                            continue;
                        }
                        if(string.IsNullOrEmpty(req.REFERENCIA) || referenciasVistas.Contains(req.REFERENCIA))
                        {
                            continue;
                        }
                        referenciasVistas.Add(req.REFERENCIA);

                        DateTime fechaEmision = DateTime.Now;
                        try
                        {
                            string fechaLimpia = req.TIEMPO;
                            if (fechaLimpia.Contains(","))
                            {
                                fechaLimpia = fechaLimpia.Split(',')[1].Trim();
                            }
                            fechaLimpia = fechaLimpia.Replace("hrs.","").Trim();
                            DateTime.TryParseExact(fechaLimpia, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaEmision);
                        }
                        catch (System.Exception)
                        {
                            fechaEmision = DateTime.Now;
                        }

                        DTE dte = new DTE
                        {
                            Tiempo = req.TIEMPO,
                            Referencia = req.REFERENCIA,
                            NitEmisor = req.NIT_EMISOR,
                            NitReceptor = req.NIT_RECEPTOR,
                            Valor = req.VALOR  
                        };
                        dte.CalcularImpuestos();
                        
                        // CORREGIDO: Se corrigió el error tipográfico en el nombre del método
                        int correlativo = BaseDatosLocal.ObtenerSiguienteCorrelativo(fechaEmision);
                        dte.GenerarCodigoAutorizacion(correlativo, fechaEmision);
                        BaseDatosLocal.FacturasAutorizadas.Add(dte);
                        aprobadas++;

                        detalleAprobaciones.Add(new
                        {
                            referencia = dte.Referencia,
                            codigoAprobacion = long.Parse(dte.CodigoAutorizacion)  
                        });
                    } 

                    var resultadoFinal = new
                    {
                        // CORREGIDO: Se ajustó el nombre de la propiedad a la estructura exacta de salida requerida
                        resultadoAutorizaciones = new
                        {
                            cantidadAutorizacionesAprobadas = aprobadas,
                            cantidadAutorizacionesRechazadasPorNitEmisorInexistente = rechazadasEmisorInexistente,
                            cantidadAutorizacionesRechazadasPorNitReceptorInexistente = rechazadasReceptorInexistente,
                            detalleAprobaciones = detalleAprobaciones
                        }
                    };
                    return Ok(resultadoFinal);
                }
                return BadRequest(new { error = "Estructura de JSON no reconocida" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Inicializar")]
        public IActionResult Inicializar()
        {
            BaseDatosLocal.ResetearTodo();
            return Ok(new { mensaje = "Sistema reseteado correctamente al estado inicial." });
        }

        [HttpGet("ConsultaDatos")]
        public IActionResult ConsultarDatos([FromQuery] string nit, [FromQuery] string tipo, [FromQuery] string fechaInicio, [FromQuery] string fechaFin)
        {
            DateTime inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var facturasFiltradas = BaseDatosLocal.FacturasAutorizadas.Where(f => {
                return true; 
            }).ToList();

            return Ok(facturasFiltradas);
        }

        [HttpGet("ResumenIva")]
        public IActionResult ResumenIva([FromQuery] string fecha)
        {
            var movimientosNit = new Dictionary<string, (decimal ivaEmitido, decimal ivaRecibido)>();

            foreach (var factura in BaseDatosLocal.FacturasAutorizadas)
            {
                if (factura.Tiempo.Contains(fecha))
                {
                    if (!movimientosNit.ContainsKey(factura.NitEmisor))
                        movimientosNit[factura.NitEmisor] = (0, 0);
                    
                    var currentEmisor = movimientosNit[factura.NitEmisor];
                    movimientosNit[factura.NitEmisor] = (currentEmisor.ivaEmitido + factura.Iva, currentEmisor.ivaRecibido);

                    if (!movimientosNit.ContainsKey(factura.NitReceptor))
                        movimientosNit[factura.NitReceptor] = (0, 0);
                    
                    var currentReceptor = movimientosNit[factura.NitReceptor];
                    movimientosNit[factura.NitReceptor] = (currentReceptor.ivaEmitido, currentReceptor.ivaRecibido + factura.Iva);
                }
            }

            var resultado = movimientosNit.Select(kv => new {
                NIT = kv.Key,
                IvaEmitido = kv.Value.ivaEmitido,
                IvaRecibido = kv.Value.ivaRecibido
            });

            return Ok(resultado);
        }

        [HttpGet("ResumenRango")]
        public IActionResult ResumenRango([FromQuery] string fechaInicio, [FromQuery] string fechaFin, [FromQuery] bool sinIva)
        {
            var resumenPorFecha = new Dictionary<string, decimal>();

            foreach (var factura in BaseDatosLocal.FacturasAutorizadas)
            {
                decimal monto = sinIva ? factura.Valor : factura.Total;
            }

            return Ok(resumenPorFecha);
        }

        [HttpGet("Grafica")]
        public IActionResult ObtenerDatosGrafica([FromQuery] string tipoResumen)
        {
            return Ok(new { mensaje = "Datos preparados para la gráfica" });
        }
       [HttpGet("ConsultaDatos")]
       public IActionResult ConsultarDatos([FromQuery] string nit, [FromQuery] string tipo, [FromQuery] string fechaInicio, [FromQuery] string fechaFin)
         {
         try
        {
        DateTime inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        DateTime fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(1).AddSeconds(-1); // Cubre todo el día final

        var facturasFiltradas = BaseDatosLocal.FacturasAutorizadas.Where(f => {
           
            string fechaStr = f.Tiempo;
            if (fechaStr.Contains(","))
            {
                fechaStr = fechaStr.Split(',')[1].Trim();
            }
            fechaStr = fechaStr.Split(' ')[0]; 

            if (!DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaFactura))
            {
                return false;
            }

       
            if (fechaFactura < inicio || fechaFactura > fin)
                return false;

            
            if (!string.IsNullOrEmpty(nit))
            {
                string tipoFiltro = tipo?.ToLower() ?? "todos";
                if (tipoFiltro == "emisor" && f.NitEmisor != nit) return false;
                if (tipoFiltro == "receptor" && f.NitReceptor != nit) return false;
                if (tipoFiltro != "emisor" && tipoFiltro != "receptor" && f.NitEmisor != nit && f.NitReceptor != nit) return false;
            }

            return true;
        }).ToList();

        return Ok(facturasFiltradas);
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = "Error en los parámetros de fecha. Use el formato dd/MM/yyyy. Detalle: " + ex.Message });
    }
}


        [HttpGet("ResumenRango")]
        public IActionResult ResumenTango([FromQuery] string fechaInicio, [FromQuery] string fechaFin, [FromQuery] bool sinIva)
        {
            try
            {
                DateTime inicio=DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime fin=DateTime.ParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var resumenPorFecha=new Dictionary<string, decimal>();
                foreach(var factura in BaseDatosLocal.FacturasAutorizadas)
                {
                    string fechaStr=factura.Tiempo;
                    if(fechaStr.Contains(","))
                    {
                        fechaStr=fechaStr.Split(',')[1].Trim();
                    }
                    fechaStr=fechaStr.Split(' ')[0];

                    if(DateTime.TryParseExact(fechaStr, "dd/MM/yyyy",CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaFactura))
                    {
                        if(fechaFactura>=inicio && fechaFactura<=fin)
                        {
                            decimal monto=sinIva ? factura.Valor:factura.Total;
                            if(!resumenPorFecha.ContainsKey(fechaStr))
                            {
                                resumenPorFecha[fechaStr]=0;
                            }
                            resumenPorFecha[fechaStr]+=monto;
                        }
                    }

                }
                var resultadoOrdenado=resumenPorFecha.OrderBy(x=> DateTime.ParseExact(x.Key, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToDictionary(k=> k.Key, v=>v.Value);
                return DayOfWeek(resultadoOrdenado);
            }
            catch (Exception ex)
           {
             return BadRequest(new { error = ex.Message });
              }
        }
    
    }
    
    


    public class ContribuyenteRequest
    {
        public string NIT { get; set; }
        public string nombre { get; set; }
    }

    public class DteRequest
    {
        public string TIEMPO { get; set; }
        public string REFERENCIA { get; set; }
        [JsonPropertyName("NIT EMISOR")]
        public string NIT_EMISOR { get; set; }
        [JsonPropertyName("NIT RECEPTOR")]
        public string NIT_RECEPTOR { get; set; }
        public decimal VALOR { get; set; }
    }

}