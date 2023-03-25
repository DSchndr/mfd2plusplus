//Used to trigger a rear view camera
//launch mpv and enable relay
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.PiGpio.NativeEnums;
using Unosquare.PiGpio.NativeMethods;


namespace carhandler
{
    internal class RearViewCam
    {
        bool Inverted;
        SystemGpio Relay;
        Process mpv;
        bool state = false; //Initial state
        public RearViewCam(SystemGpio pRelay, bool pInverted) {
            Relay = pRelay;
            Inverted = pInverted;
            IO.GpioWrite(pRelay, false);
            IO.GpioSetMode(pRelay, PinMode.Output);
        }

        public void SetRearViewCam(bool pState)
        {
            IO.GpioWrite(Relay, (pState^Inverted)); //xor with inverted
            if (state == pState )
            {
                Console.WriteLine("[RearViewCam]: Hej! RVC mpv already started :/");
                return;
            }

            if (state) launchmpv();
            else killmpv();
        }

        private void launchmpv()
        {//TODO: get this from config
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "mpv --no-cache --untimed --hwdec=mmal --no-correct-pts --video-latency-hacks=yes --profile=low-latency av://v4l2:/dev/video0", };
            mpv = new Process() { StartInfo = startInfo, };
            mpv.Start();
        }
        private void killmpv()
        {
            mpv.Kill();
        }
    }
}
