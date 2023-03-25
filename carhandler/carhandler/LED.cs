//ambient lighting stuff
//todo: effects handled in task
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Unosquare.PiGpio.NativeEnums;
using Unosquare.PiGpio.NativeMethods;

namespace carhandler
{
    internal class LED
    {
        enum LedEffect
        {
            Static = 0,
            Rainbow = 1,
            PingPong = 2,

        }
        //todo: add led types
        UserGpio gr, gg, gb;
        public LED(SystemGpio pR, SystemGpio pG, SystemGpio pB) {
            UserGpio gr = (UserGpio)((int)pR);
            UserGpio gg = (UserGpio)((int)pG);
            UserGpio gb = (UserGpio)((int)pB);
            Setup.GpioInitialise();
            IO.GpioSetMode(pR, PinMode.Output);
            IO.GpioSetMode(pG, PinMode.Output);
            IO.GpioSetMode(pB, PinMode.Output); 
            Pwm.GpioPwm(gr, 0);
            Pwm.GpioPwm(gg, 0);
            Pwm.GpioPwm(gb, 0);
        }
        public void SetRGB(uint pDR, uint pDG, uint pDB)
        {
            Pwm.GpioPwm(gr, pDR);
            Pwm.GpioPwm(gg, pDG);
            Pwm.GpioPwm(gb, pDB);
        }
        
    }
}
