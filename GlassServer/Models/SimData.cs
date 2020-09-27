using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GlassServer.Models
{
    public class SimDataModel
    {
        public string name { get; set; }
        public string units { get; set; }
        public double? value { get; set; }
        public string? text { get; set; }
    }
}
