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
        public string? text;
        public string units;

        public bool registered = false;
        public bool pending = false;
        public bool readOnly;

        public Func<double, double> transformValue;
        public Func<double, string> transformText;

        public void SetValue(double _value)
        {
            if (transformValue != null)
            {
                _value = transformValue(_value);
            }

            string _text = null;
            if (transformText != null)
            {
                _text = transformText(_value);
            }

            value = _value;
            text = _text;
        }

    }

    public class SimDataDefBuilder
    {
        string m_name;
        string m_units;

        bool m_readOnly = false;

        Func<double, double> m_transformValue;
        Func<double, string> m_transformText;

        public SimDataDefBuilder Name(string _name)
        {
            var copy = Clone();
            copy.m_name = _name;
            return copy;
        }

        public SimDataDefBuilder Units(string _units)
        {
            var copy = Clone();
            copy.m_units = _units;
            return copy;
        }

        public SimDataDefBuilder ReadOnly()
        {
            var copy = Clone();
            copy.m_readOnly = true;
            return copy;
        }

        public SimDataDefBuilder Transform(Func<double, string> transformer)
        {
            var copy = Clone();
            copy.m_transformText = transformer;
            return copy;
        }

        public SimDataDefBuilder Transform(Func<double, double> transformer)
        {
            var copy = Clone();
            copy.m_transformValue = transformer;
            return copy;
        }

        public SimDataDefBuilder Enum(params (int, string)[] entries)
        {
            var copy = Clone();
            var dict = entries.ToDictionary(e => e.Item1, e => e.Item2);

            copy.m_transformText = (value) =>
            {
                string result;
                if (dict.TryGetValue((int)value, out result))
                {
                    return result;
                }

                return null;
            };

            return copy;
        }

        public SimDataDef Build()
        {
            if (m_name == null) throw new Exception("Name of SimDataDef must be given.");
            if (m_units == null) throw new Exception("Units of SimDataDef must be given.");

            return new SimDataDef
            {
                name = m_name,
                units = m_units,
                readOnly = m_readOnly,
                transformValue = (Func<double, double>)m_transformValue?.Clone(),
                transformText = (Func<double, string>)m_transformText?.Clone()
            };
        }

        private SimDataDefBuilder Clone()
        {
            return new SimDataDefBuilder
            {
                m_name = m_name,
                m_readOnly = m_readOnly,
                m_units = m_units,
                m_transformValue = (Func<double, double>)m_transformValue?.Clone(),
                m_transformText = (Func<double, string>)m_transformText?.Clone()
            };
        }
    }
}
