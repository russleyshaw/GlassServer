using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlassServer.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GlassServer.Controllers
{
    [Route("api/simdata")]
    [ApiController]
    public class SimDataController : ControllerBase
    {
        // GET: api/simdata
        [HttpGet]
        [EnableCors("AllowAll")]
        public SimDataModel[] Get([FromQuery(Name = "name")] string[] saNames)
        {
            // Request all data at once and wait until a result.
            var results = saNames.Distinct().Select(sName => {
                var def = SimDataManager.GetDefinition(sName);

                if (def == null) return null;

                return new SimDataModel {
                    name = def.name,
                    value = def.value,
                    units = def.units
                };
            }).Where(def => def != null);

            return results.ToArray();
        }

        // POST: api/simdata
        [HttpPost]
        [EnableCors("AllowAll")]
        public async Task Post([FromBody] SimDataPostEntry[] entries)
        {
            await SimDataManager.Connect();

            foreach (var entry in entries)
            {
                SimDataManager.RequestDataSet(entry.name, entry.value);
            }
        }

        public class SimDataPostEntry
        {
            public string name { get; set; }
            public float value { get; set; }
        }
    }
}
