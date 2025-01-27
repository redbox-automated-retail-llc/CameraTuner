using System.Runtime.InteropServices;

namespace Redbox.DirectShow.Interop
{
    internal static class Tools
    {
        public static IPin GetPin(IBaseFilter filter, PinDirection dir, int num)
        {
            IPin[] pins = new IPin[1];
            IEnumPins enumPins = (IEnumPins)null;
            if (filter.EnumPins(out enumPins) == 0)
            {
                try
                {
                    while (enumPins.Next(1, pins, out int _) == 0)
                    {
                        PinDirection pinDirection;
                        pins[0].QueryDirection(out pinDirection);
                        if (pinDirection == dir)
                        {
                            if (num == 0)
                                return pins[0];
                            --num;
                        }
                        Marshal.ReleaseComObject((object)pins[0]);
                        pins[0] = (IPin)null;
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject((object)enumPins);
                }
            }
            return (IPin)null;
        }

        public static IPin GetInPin(IBaseFilter filter, int num)
        {
            return Tools.GetPin(filter, PinDirection.Input, num);
        }

        public static IPin GetOutPin(IBaseFilter filter, int num)
        {
            return Tools.GetPin(filter, PinDirection.Output, num);
        }
    }
}
