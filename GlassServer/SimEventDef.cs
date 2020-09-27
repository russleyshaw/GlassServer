using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GlassServer
{
    public enum EVENT_DEFINITION
    {
        Invalid = 0
    };

    public enum EVENT_GROUP
    {
        Main
    }


    public class SimEventDef
    {
        public EVENT_DEFINITION eDef = EVENT_DEFINITION.Invalid;

        public string name;

        public bool registered = false;

    }

    public class SimEventDefBuilder
    {
        string m_sName;

        public SimEventDefBuilder Name(string _sName)
        {
            var copy = Clone();
            copy.m_sName = _sName;
            return copy;
        }

        public SimEventDefBuilder Clone()
        {
            return new SimEventDefBuilder
            {
                m_sName = m_sName
            };
        }

        public SimEventDef Build()
        {
            if (m_sName == null) throw new Exception("Event name must be defined.");

            return new SimEventDef
            {
                name = m_sName,
            };
        }
    }
}
