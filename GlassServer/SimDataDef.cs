using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GlassServer
{
    public enum DEFINITION
    {
        Invalid = 0
    };

    public enum REQUEST
    {
        Invalid = 0
    };


    public class SimDataDef
    {
        public DEFINITION eDef = DEFINITION.Invalid;
        public REQUEST eRequest = REQUEST.Invalid;

        public string name;
        public double value = 0;
        public string units;

        public bool registered = false;
        public bool pending = false;
        public bool readOnly = false;
     
        public SimDataDef(string _name, string _units)
        {
            name = _name;
            units = _units;
        }
    }
}
