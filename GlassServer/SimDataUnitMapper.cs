using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace GlassServer
{
    
    /// <summary>
    /// Provides a quick mapping between SIMVAR names and their most relevant units.
    /// </summary>
    public class SimDataUnitMapper
    {
        private static Dictionary<string, string> kvUnitDict = new Dictionary<string, string>();

        static SimDataUnitMapper()
        {

            var unit = "number";
            SetUnits("NUMBER OF ENGINES", unit);

            unit = "feet";
            SetUnits("INDICATED ALTITUDE", unit);

            unit = "percent";
            SetUnits("LIGHT TAXI", unit);
            SetUnits("LIGHT NAV", unit);
            SetUnits("LIGHT BEACON", unit);
            SetUnits("LIGHT STROBE", unit);
            SetUnits("LIGHT LANDING", unit);
            SetUnits("BRAKE PARKING POSITION", unit);
            SetUnits("GEAR POSITION", unit);
            SetUnits("FLAPS HANDLE PERCENT", unit);
            SetUnits("ELEVATOR TRIM PCT", unit);
            SetUnits("RUDDER TRIM PCT", unit);
            SetUnits("ALERION TRIM PCT", unit);


            unit = "knots";
            SetUnits("AIRSPEED INDICATED", unit);
            SetUnits("AIRSPEED TRUE", unit);
            SetUnits("AMBIENT WIND VELOCITY", unit);

            unit = "degrees";
            SetUnits("AMBIENT WIND DIRECTION", unit);
            SetUnits("PLANE BANK DEGREES", unit);
            SetUnits("PLANE PITCH DEGREES", unit);
            SetUnits("HEADING INDICATOR", unit);

            unit = "fpm";
            SetUnits("VERTICAL SPEED", unit);

            unit = "gallons";
            SetUnits("FUEL TOTAL QUANTITY", unit);
            SetUnits("FUEL TOTAL CAPACITY", unit);

            unit = "censius";
            SetUnits("AMBIENT TEMPERATURE", unit);


            // MISC
            SetUnits("PLANE LATITUDE", "degrees latitude");
            SetUnits("PLANE LONGITUDE", "degrees longitude");
            SetUnits("G FORCE", "G Force");


        }

        public static string? FindUnits(string sName)
        {
            string units = "";
            if(kvUnitDict.TryGetValue(sName, out units))
            {
                return units;
            }

            return null;
        }

        private static void SetUnits(string sName, string sUnits)
        {
            kvUnitDict.Add(sName, sUnits);
        }
    }
}
