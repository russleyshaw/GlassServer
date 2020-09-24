using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlassServer.Models;
using Microsoft.AspNetCore.Mvc;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GlassServer.Controllers
{
    [Route("api/simdata")]
    [ApiController]
    public class SimDataController : ControllerBase
    {
        // GET: api/simdata
        // BODY (application/json): ["NAME 1", "NAME 2", ...]
        [HttpGet]
        public async Task<IEnumerable<SimDataModel>> Get([FromBody] string[] saNames)
        {
            // Request all data at once and wait until a result.
            var results = await Task.WhenAll(saNames.Distinct().Select(async sName => {
                return new SimDataModel {
                    name = sName,
                    value = await SimDataManager.RequestData(sName.Trim().ToUpper()),
                    units = SimDataUnitMapper.FindUnits(sName)
                };
            }));

            return results;
        }
    }
}
