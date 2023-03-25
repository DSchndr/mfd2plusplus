//launch node-carplay and handle its crashes :)
//todo: IPC for buttons and 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carhandler
{
    internal class Carplay
    {
        Process carplay;
        public Carplay() { }
        public void LaunchCarplay()
        {//TODO: get this from config...
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "cd /home/pi/tmp/node-carplay && node .", };
            carplay = new Process() { StartInfo = startInfo, };
            carplay.Start();
        }
        public void KillCarplay()
        {
            carplay.Kill();
        }
    }
}
