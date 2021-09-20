using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.Protocol.McProtocol;

namespace SampleApp.PlcTools
{
    public static class KenyenceDeviceToMcConvert
    {
        private static string GetBitAddress(int address, int subAddress) => ((address * 0x10) + subAddress).ToString();

        //For R, MR, LR, CR, Keyence use "R0000 to R1999_15" form to indicate address.
        public static McProtocol.McBitDevice R(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"X{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice MR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"M{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice LR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"L{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice CR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"SM{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice B(int address) => new McProtocol.McBitDevice($"B{address}");

        public static McProtocol.McWordDevice CM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"S{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice DM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"D{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice EM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"D{address + 100000}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice FM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"R{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice ZF(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"ZR{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice W(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"W{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McWordDevice Tcurrent(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"TN{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McBitDevice Tcontact(int address)
        {
            return new McProtocol.McBitDevice($"TS{address}");
        }

        public static McProtocol.McWordDevice Ccurrent(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0)
        {
            return new McProtocol.McWordDevice($"CN{address}", upperLimit, lowerLimit, asDouble, asFloat, decimalPointPosition);
        }

        public static McProtocol.McBitDevice Ccontact(int address)
        {
            return new McProtocol.McBitDevice($"CS{address}");
        }
    }
}
