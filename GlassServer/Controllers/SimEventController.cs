using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace GlassServer.Controllers
{
    [Route("api/simevent")]
    [ApiController]
    public class SimEventController : ControllerBase
    {
        [HttpPost]
        [EnableCors("AllowAll")]
        public async Task Post([FromBody] PostEntry[] entries)
        {
            foreach (var entry in entries)
            {
                SimManager.SendEvent(entry.name, entry.value);
            }
        }

        public class PostEntry
        {
            public string name { get; set; }
            public uint value { get; set; }
        }
    }
    
}


