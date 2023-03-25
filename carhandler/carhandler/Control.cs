using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carhandler
{
    internal class Control
    {
        static string canif = "can0";
        RearViewCam rvcam;
        CanButton canbutton;
        Configuration config;

        public Control() {
            rvcam = new RearViewCam(0, false);
            canbutton = new CanButton(canif, this);
            config = new Configuration();
        }
        public void RearViewCamTrigger(bool pState)
        {
            rvcam.SetRearViewCam(pState);
        }

        //todo: events, use this for test...
        public void WheelButtonEvent(CanButton.SteeringWheelButton pWheelButton)
        {
            Console.WriteLine($"Got Wheelbutton: {pWheelButton}");
        }
        public void MFDButtonEvent(CanButton.MFDButton pMFDButton)
        {
            Console.WriteLine($"Got Mfdbutton: {pMFDButton}");
        }
        public void RotaryEvent(bool pDirection)
        {
            if (pDirection)
            {
                Console.WriteLine("Rotary Dir: +");
            }
            else
            {
                Console.WriteLine("Rotary Dir: -");
            }
        }
    }
}
