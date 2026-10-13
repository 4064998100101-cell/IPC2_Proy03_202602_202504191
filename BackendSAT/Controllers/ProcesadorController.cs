using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using BackendSAT.Models;
using BackendSAT.Services;
using System.Runtime.Versioning;
using System.Globalization;
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
                    var contribuyentes =JsonSerializer.Deserialize<List<ContribuyenteRequest>>(jsonString);
                    int nuevos=0;
                    int actualizados=0;
                    int invalidos=0;

                    foreach(var item in contribuyentes)
                    {
                        if(Contribuyente.ValidarNit(item.NIT))
                        {
                            if(BaseDatosLocal.RTU.ContainsKey(item.NIT))
                            {
                                BaseDatosLocal.RTU[item.NIT].Nombre=item.nombre;
                                actualizados++;
                            }
                            else
                            {
                                BaseDatosLocal.RTU.Add(item.NIT, new Contribuyente(item.NIT,item.nombre));
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
                            Contribuyentesnuevos=nuevos,
                            contributentesactualizados=actualizados,
                            nitsInvalidos=invalidos
                        };
                        return Ok(respuestaRtu);

                    
                }
                else if(jsonString.Contains("TIEMPO") || jsonString.Contains("REFERENCIA"))
                    {
                       var solicitudes=JsonSerializer.Deserialize<List<DteRequest>>(jsonString);
                       int aprobadas=0;
                       int rechazadasEmisorInexistente=0;
                       int rechadasReceptorInexistente=0;
                       var detalleAprobaciones=new List<object>();
                       var referenciasVistas=new HashSet<string>();
                       foreach (var req in solicitudes)
                    {
                        bool emisorValido=Contribuyente.ValidarNit(req.NIT_EMISOR);
                        bool receptorValido=Contribuyente.ValidarNit(req.NIT_RECEPTOR);
                        bool emisorExiste=BaseDatosLocal.RTU.ContainsKey(req.NIT_EMISOR);
                        bool receptorExiste=BaseDatosLocal.RTU.ContainsKey(req.NIT_RECEPTOR);

                        if(!emisorValido || !emisorExiste)
                        {
                            rechazadasEmisorInexistente++;
                            continue;
                        }
                        if(!receptorValido || !receptorExiste)
                        {
                            rechadasReceptorInexistente++;
                            continue;
                        }
                        if(string.IsNullOrEmpty(req.REFERENCIA)|| referenciasVistas.Contains(req.REFERENCIA))
                        {
                            continue;
                        }
                        referenciasVistas.Add(req.REFERENCIA);

                        DateTime fechaEmision=DateTime.Now;
                        try
                        {
                            string fechaLimpia=req.TIEMPO;
                            if (fechaLimpia.Contains(","))
                            {
                                fechaLimpia=fechaLimpia.Split(',')[1].Trim();
                            }
                            fechaLimpia=fechaLimpia.Replace("hrs.","").Trim();
                            DateTime.TryParseExact(fechaLimpia,"dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaEmision);

                        }
                        catch (System.Exception)
                        {
                            
                            fechaEmision=DateTime.Now;
                        }
                        DTE dte=new DTE
                        {
                            Tiempo = req.TIEMPO,
                            Referencia = req.REFERENCIA,
                            NitEmisor = req.NIT_EMISOR,
                            NitReceptor = req.NIT_RECEPTOR,
                            Valor = req.VALOR  
                        };
                        dte.CalcularImpuestos();
                        int correlativo= BaseDatosLocal.ObtenerSiguienteCorrelatino(fechaEmision);
                        dte.GenerarCodigoAutorizacion(correlativo, fechaEmision);
                        BaseDatosLocal.FacturasAutorizadas.Add(dte);
                        aprobadas++;

                        detalleAprobaciones.Add(new
                        {
                          referencia=dte.Referencia,
                          codigoAprobacion=long.Parse(dte.CodigoAutorizacion)  
                        }
                        );

                    } 

                    var resultadoFinal=new
                    {
                        resutladoAutorizaciones=new
                        {
                            cantidadAutorizacionesAprobadas = aprobadas,
                            cantidadAutorizacionesRechazadasPorNitEmisorInexistente = rechazadasEmisorInexistente,
                            cantidadAutorizacionesRechazadasPorNitReceptorInexistente =  rechadasReceptorInexistente,
                            detalleAprobaciones = detalleAprobaciones
                        }
                    };
                    return Ok(resultadoFinal);
                    
                    }
                    return BadRequest(new {error="Estructura de JSon no reconocida"});
            }
            catch (Exception ex)
            {
                
                return BadRequest(new {error =ex.Message});
            }
        }
        [HttpPost("Inicializar")]
        public IActionResult Inicializar()
        {
            BaseDatosLocal.ResetearTodo();
            return Ok(new { mensaje = "Sistema reseteado correctamente al estado inicial." });
        }

    
[HttpGet("ConsultaDatos")]
       [HttpGet("ConsultaDatos")]
        public IActionResult ConsultarDatos([FromQuery] string nit, [FromQuery] string tipo, [FromQuery] string fechaInicio, [FromQuery] string fechaFin)
        {
            // tipo: "emitidas" o "recibidas"
            DateTime inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var facturasFiltradas = BaseDatosLocal.FacturasAutorizadas.Where(f => {
                // Lógica de filtrado por fechas y NIT
                return true; 
            }).ToList();

            // Retornar la respuesta en formato JSON mediante Ok()
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
        public string NIT_EMISOR { get; set; }
        public string NIT_RECEPTOR { get; set; }
        public decimal VALOR { get; set; }
    }
   
    
}