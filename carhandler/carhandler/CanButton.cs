//canbus button packet handling
using SocketCANSharp;
using SocketCANSharp.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carhandler
{
    internal class CanButton
    {
        enum SelectorPosition : byte
        {
            Forward = 0x00,
            Reverse = 0x02,
        }

        enum SteeringWheelButton : byte
        {
            None = 0,
            Up = 0x22,
            Down = 0x23,
            VolP = 0x06,
            VolM = 0x07,
            Menu = 0x0A,
            Phone = 0x1A,
            OK = 0x28,
            Mute = 0x2B
        }
        enum MFDButton : byte
        {
            B1 = 1,
            B2 = 2,
            B3 = 3,
            B4 = 4,
            B5 = 5,
            B6 = 6,
            B7 = 7,
            B8 = 8,
            B9 = 9,
            B10 = 0x0A,
            TrackRight = 0x0c,
            TrackLeft = 0x0b,
            Back = 0x0e,
            RotaryClick = 0x0D,
        }

        enum ButtonTiming
        {
            ShortPress = 0,
            LongPress = 1,
        }

        private CanNetworkInterface if_can;
        private RawCanSocket rawCanSocket;
        private Task CanMessageReceiverTask;
        public CanButton(string pInterface)
        {
            if_can = CanNetworkInterface.GetAllInterfaces(true).First(iface => iface.Name.Equals(pInterface));
            rawCanSocket = new RawCanSocket();
            CanMessageReceiverTask = Task.Run(() => CanMessageReceiver(rawCanSocket));
        }

        private void CanMessageReceiver(RawCanSocket prawCanSocket)
        {
            byte lastWheelButton = 0;
            Stopwatch lastWheelButtonStopwatch = new Stopwatch();
            byte lastMFDButton = 0;
            byte lastMFDRotaryPos= 0;
            Stopwatch lastMFDButtonStopwatch = new Stopwatch();
            while (true)
            {
                var readFrame = new CanFrame();
                int bytesRead = prawCanSocket.Read(out readFrame);
                if (bytesRead > 0)
                {
                    if (readFrame.CanId == 0x461) //MFD Buttons 
                    {
                        if (readFrame.Data[2] == 0x30) //In the normal Radio UI
                        {
                            continue; //Skip handling buttons
                        }
                        if (readFrame.Data[1] > lastMFDRotaryPos) //Rotary right
                        {

                        }
                        if (readFrame.Data[1] < lastMFDRotaryPos) //Rotary left
                        {

                        }
                        if (readFrame.Data[0] == 0 && lastMFDButton != 0) //Button Stop being pressed
                        {
                            //get sw val, reset, decide if long, send event
                            var time = lastMFDButtonStopwatch.ElapsedMilliseconds;
                            lastMFDButtonStopwatch.Stop();
                            lastMFDButtonStopwatch.Reset();
                            MFDButton b = (MFDButton)lastMFDButton;
                            lastMFDButton = 0;
                        }
                        if (readFrame.Data[0] == lastMFDButton) //Button is keep getting pressed
                        {

                        }
                        if ((readFrame.Data[0] != lastMFDButton) && (lastMFDButtonStopwatch.ElapsedMilliseconds == 0)) //Button was pressed
                        {
                            lastMFDButtonStopwatch.Start();
                        }
                    }

                    if (readFrame.CanId == 0x5c1) //Steering wheel buttons
                    {
                        if (readFrame.Data[0] == 0 && lastWheelButton != 0) //Button Stop being pressed
                        {
                            //get sw val, reset, decide if long, send event
                            var time = lastWheelButtonStopwatch.ElapsedMilliseconds;
                            lastWheelButtonStopwatch.Stop();
                            lastWheelButtonStopwatch.Reset();
                            SteeringWheelButton b = (SteeringWheelButton)lastWheelButton;
                            lastWheelButton = 0;
                        }
                        if (readFrame.Data[0] == lastWheelButton) //Button is keep getting pressed
                        {

                        }
                        if ((readFrame.Data[0] != lastWheelButton) && (lastWheelButtonStopwatch.ElapsedMilliseconds == 0)) //Button was pressed
                        {
                            lastWheelButtonStopwatch.Start();
                            lastWheelButton = readFrame.Data[0];
                        }
                    }
                    if (readFrame.CanId == 0x351)
                    {
                        SelectorPosition s = (SelectorPosition)readFrame.Data[0];
                    }
                }
            }

        }
    }
}
