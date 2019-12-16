using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ftdicomm
{
    class Sensor // полное описание одного датчика.
    {
        public byte Address { get; private set; }
        public float P_SI { get; set; }
        public float T_SI { get; set; }

        public ushort P_ADC { get; set; }
        public ushort T_ADC { get; set; }

        public Sensor() { }
        
        public Sensor(byte address)
        {
            this.Address = address;
        }

        public override string ToString()
        {
            return $"address: {this.Address} P si {P_SI} T si {T_SI}\n";
        }

    }
}
