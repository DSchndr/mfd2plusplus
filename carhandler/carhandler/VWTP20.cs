//VAG Transport Protokoll 2.0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketCANSharp;
using SocketCANSharp.Network;
using SocketCANSharp.Network.BroadcastManagement;
using System.Threading.Tasks;
using System.Diagnostics;

namespace carhandler
{
    internal class VWTP20
    {
        private CanNetworkInterface if_can;
        private RawCanSocket rawCanSocket;
        public uint timeout = 0;
        private byte rx_seq = 0;
        private byte tx_seq = 0;
        private uint rx_addr = 0;
        private uint tx_addr = 0;
        private uint packetinterval;
        private uint timetoack;


        enum TPOp : byte //fucking c# forcing me to use an enum blyat
        {
            ACKmorepackets = 0x00,     // Waiting for ACK, more packets to follow (i.e. reached max block size value as specified above)
            ACKlastpacket = 0x10,     //Waiting for ACK, this is last packet   
            NOACKmorepackets = 0x20,    // Not waiting for ACK, more packets to follow
            NOACKlastpacket = 0x30,    //Not waiting for ACK, this is last packet
            ACKreadynextpacket = 0xB0,    // ACK, ready for next packet
            ACKnotreadynextpacket = 0x90    //ACK, not ready for next packet 
        }

        public VWTP20(string pInterface)
        {
            if_can = CanNetworkInterface.GetAllInterfaces(true).First(iface => iface.Name.Equals(pInterface));
            rawCanSocket = new RawCanSocket();
        }
        public VWTP20(string pInterface, uint prx_addr, uint ptx_addr )
        {
            if_can = CanNetworkInterface.GetAllInterfaces(true).First(iface => iface.Name.Equals(pInterface));
            rx_addr = prx_addr;
            tx_addr = ptx_addr;
            rawCanSocket = new RawCanSocket();
            rawCanSocket.Bind(if_can);

        }

        public int CreateCyclicMessage(uint pcanid, byte[] pData, int pInterval) //interval: in ms
        {
            if(if_can == null || pData.Length >= 8) { //fix null
                return -1; //todo exception
            }
            using (var bcmCanSocket = new BcmCanSocket())
            {
                bcmCanSocket.Connect(if_can);
                var canFrame = new CanFrame(pcanid, pData);
                var frames = new CanFrame[] { canFrame };
                var config = new BcmCyclicTxTaskConfiguration()
                {
                    Id = pcanid,
                    StartTimer = true,
                    SetInterval = true,
                    //InitialIntervalConfiguration = new BcmInitialIntervalConfiguration(10, new BcmTimeval(0, 5000)), // 10 messages at 5 ms
                    PostInitialInterval = new BcmTimeval(0, pInterval*1000), //*1000 since time is in us
                };
                return bcmCanSocket.CreateCyclicTransmissionTask(config, frames); //return bytes
            }
        }
        public int InitChanParams(byte pBlockSize, byte pTimeToACK, byte pPacketInterval)
        {
            timetoack = ExtractTimingParameter(pTimeToACK);
            packetinterval = ExtractTimingParameter(pPacketInterval);
            rawCanSocket.Write(new CanFrame(tx_addr, new byte[] { 0xA0, pBlockSize, pTimeToACK, 0xFF, pPacketInterval, 0xFF }));
            DebugPrint($"Initializing channel with timetoack: {timetoack} and packetinterval: {packetinterval}");
            WaitForAck(true);
            return 0; //todo: if timeout return -1
        }

        public int Send(byte[] data, bool ack)
        {
            List<byte[]> packets = new List<byte[]>();
            if(data.Length > 7) {
                //Split packets
                for (int i = 0; i < data.Length; i+=7)
                {
                    int remaininglength = data.Length - i;
                    packets.Add(data.Skip(i).Take(remaininglength < 7 ? remaininglength : 7).ToArray());
                }
            }
            else
            {
                packets.Add(data);
            }
            DebugPrint($"Got {packets.Count()} VWTP packets to send");
            foreach (byte[] packet in packets)
            {
                if(packet == packets.Last()) //Last packet
                {
                    rawCanSocket.Write(new CanFrame(tx_addr, new byte[] { (byte)(ack ? (byte)TPOp.ACKlastpacket | TX_Inc() : (byte)TPOp.NOACKlastpacket | TX_Inc()) }.Concat(packet).ToArray()));
                    //TODO: Wait for ack
                    WaitForAck(false);
                    return 0;
                }
                rawCanSocket.Write(new CanFrame(tx_addr, new byte[] { (byte)(ack ? (byte)TPOp.ACKmorepackets | RX_Inc() : (byte)TPOp.NOACKmorepackets | RX_Inc()) }.Concat(packet).ToArray()));
            };
            return 0;
        }

        uint ExtractTimingParameter(byte time) //Extract timing param to int in ms
        {
            float result = 0;
            int scaler = time & 0b00111111; //Number to scale the units by  
            if ((time & 0b11000000) == 0) result = result + 0.1f; //0.1ms
            if ((time & 0b11000000) == 1) result = result + 1f; //1ms
            if ((time & 0b11000000) == 2) result = result + 100f; //10ms
            if ((time & 0b11000000) == 3) result = result + 100f; //100ms
            result = result * scaler;
            return (uint)Math.Round(result);

        }

        //TODO: Implement send 0xA8
        bool WaitForAck(bool ignorecontent)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //bool havetowait = false; //0x9 packet
            while (true)
            {
                if (sw.ElapsedMilliseconds > timetoack) {
                    DebugPrint("ACK Timeout");
                    return false; 
                }
                var readFrame = new CanFrame();
                int bytesRead = rawCanSocket.Read(out readFrame);
                if (bytesRead > 0)
                {
                    if(readFrame.CanId == rx_addr)
                    {
                        if (ignorecontent) return true;
                        //todo: add break (a4), disconnect (a8)
                        if (readFrame.Length == 1 && ((readFrame.Data[0] & 0xF0) == 0xB0)) //did we get "ACK, ready for next packet"?  
                        { //i think we have to check if its right but i dont care rn lol
                            int seq = readFrame.Data[0] & 0x0F;
                            tx_seq = (byte)seq;
                            DebugPrint($"Got ACK, next packet ({readFrame.Data[0]}), seq set to: {seq}");
                            return true;
                        }
                        if ((readFrame.Length > 1 && (((readFrame.Data[0] & 0xF0) == 0x10)) | ((readFrame.Data[0] & 0xF0) == 0x10))) //Did we get last packet?
                        {
                            int seq = readFrame.Data[0] & 0x0F;
                            rx_seq = (byte)seq;
                            RX_Inc(); //Response seq is +1
                            SendAck();
                            DebugPrint($"Got Last packet ({readFrame.Data[0]}), seq: {seq}, rx_seq: {rx_seq}");
                            return true;
                        }
                    }
                }
            }
        }

        void SendAck()
        {
            rawCanSocket.Write(new CanFrame(tx_addr, new byte[] { (byte)(rx_seq | 0xB0)}));
        }

        byte TX_Inc()
        {
            if (tx_seq == 0xF) tx_seq = 0;
            return tx_seq++;
        }
        byte RX_Inc()
        {
            if (rx_seq == 0xF) rx_seq = 0;
            return rx_seq++;
        }

        void DebugPrint(string pText)
        {
            Console.WriteLine($"[VWTP]: {pText}");
        }

        private static void ReceiveTask(RawCanSocket prawCanSocket)
        {
            while (true)
            {
                var readFrame = new CanFrame();
                int bytesRead = prawCanSocket.Read(out readFrame);
                if (bytesRead > 0)
                {

                }
            }
        }

    }
}
