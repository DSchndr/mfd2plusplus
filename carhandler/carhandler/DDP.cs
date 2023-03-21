//VAG Display Daten Protokoll (for playing tetris on your cluster...)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carhandler
{
    internal class DDP
    {
        VWTP20 vwtp;
        public DDP(string pInterface) {
            vwtp = new VWTP20(pInterface, 0x687, 0x686); //Addresses for Telematics module TP2.0 comms //TODO: callback (rx) function
            SimulateTelematics(); //Fake that thing being installed
            vwtp.InitChanParams(0x0F,0x8A,0x4A);
            SendDDPMagicPacket1(); //Add some magic so the cluster does *magic stuff*
            SendDDPMagicPacket2();
            //wait for auth
            DDPLogin("Test Menu", true, 0x55, 0x12);
        }

        private void SimulateTelematics()
        {
            vwtp.CreateCyclicMessage(0x5A7,new byte[] { 0x80,0x80, 0x00, 0x40, 0x00, 0x09, 0x00, 0x00 } , 300);
        }
        void SendDDPMagicPacket1()
        {
            vwtp.Send(new byte[] { 0x00, 0x55, 0x00, 0xFF }, true);
        }
        void SendDDPMagicPacket2()
        {
            vwtp.Send(new byte[] { 0x01, 0x12 }, true);
        }

        public int RXHandler()
        {
            return 0;
        }

        //# 01=normal clear, 0x02=invert, 0x03=add menu entry
        public void ClearArea(byte pID, int pType, byte StartingPointHorizontal, byte StartingPointVertical, byte NumLinesHorizontal, byte pNumLinesVertical) 
        {
            byte type;
            switch(pType)
            {
                default:
                case 1:
                    type = 0x01;
                    break;
                case 2:
                    type = 0x02;
                    break;
                case 3:
                    type = 0x03;
                    break;
            }
            vwtp.Send(new byte[] { 0x09, pID, 0x60, 0x09, type, StartingPointHorizontal, 0x00, StartingPointVertical, 0x00, NumLinesHorizontal, 0x00, pNumLinesVertical, 0x00, 0x08}, true);
        }

/* 0x02 - DDP Channel Login OPCODE

# 0x02 | 0xME | 0xID | 0xSS | 0xTT | 0xTT | 0xTT
# 0xME : 0x70 			= With Menu Entry
#        0x71 - 0x85	= Without Menu Entry (Lower = Higher Priority) 
# 0xID : Device ID
# 0xSS : 0x00 = All segments
#        0x10 = Middle Segment
#        0x20 = Upper Segment
#        0x30 = Lower Segment
#        0x40 = Upper + Middle Segment
#        0x50 = Lower + Middle Segment
# 0xTT : Ascii text of unknown length
*/
        public void DDPLogin(string pText, bool pMenuEntry, byte pID, byte pSegments)
        {
            vwtp.Send(new byte[] {0x02, (byte)(pMenuEntry ? 0x70 : 0x71), pSegments}.Concat(Encoding.ASCII.GetBytes(pText)).ToArray(), true);
        }
    }
}


/*
 * 
# Commands from Display:
# 20 ?? 				: All DDP Channels deleted for Participant
# 21 00 04 00 xx 00 yy 	: Display-Segmentsize (OP|Internal Segmentnumber|0x00|Width(h)|Width(l)|Height(h)|Height(l))
# 23 xx 01 00 			: Screen with ID xx available, rendering active, you can send data
# 23 xx 00 00 			: Screen with ID xx UNavailable, in case you send data, just keep sending :)
# 27 xx yy 				: Finished Rendering, you can send new data (yy=channel status) OR
# 23 xx 00 00 			: Data received, did not render since screen is not available

# 2A xx 				: (Screen with ID xx available) DDP Channel (xx) left by selecting next Menu entry
# 2B FF xx 				: Error with ID xx
#Fehlercodes:
#0x01: Unbekannter Opcode
#0x02: Nachricht unvollständig
#0x03: Unbekannte Kanalnummer
#0x04: Unbekanntes Segment
#0x05: Fehler in Displaydaten
#0x06: ?
#0x07: x Koordinate zu groß fuer Segment
#0x08: Y Koordinate zu groß fuer Segment
#0x09: Maximum an Kanälen (3) überschritten
#0x35 = DDP Teilnehmer abgemeldet (OP)




# Commands to Display:
# 00 xx 		 	: Delete all channels for xx being device id (ex. 0x55)
# 01 ss			 	: Get Segment size of seg ss
# 02 (Look at def)	: DDP Channel login
# 05 cc			 	: Disable Screen with ID / Logout channel (ex. turning off), have to reinit afterwards
# Response: 25 xx

# 06 cc mm 		 	: Change Menu mode cc = channel, mm = menumode
# 09 cc			 	: Opcode to send data for DDP channel
# 0A cc			 	: Jump to menu of channel? / Hide Screen with ID (ex. to exit from Settings entry)
# Response: 25 xx

# 0B cc ss		 	: Change Display Segment of Channel to SS (new seg)
# 0C cc 		 	: (force) Show Screen with ID / channel Priority, works only with Main entry
# Response: "23 xx 00 00, 23 xx 01 00", then you can send data 

# 0D 00 00 		 	: Delete all channels for DDP Participant
# 15 xx 20 01 st 	: DDP Status xx = ID, ss = 0x00 (OFF) / 0x01 (ON)

#----------------------------------------------------------------------------------------------------------------------------
# 0x09 - DDP Data
# 09 xx 		: Data array start. xx - ID
# --- 			: data
# 08 			: End of Array

# -Rectangle
# 60 ll aa xx 00 yy 00 ww 00 hh 00
# 60 = Command "draw rectangle"
# ll - Länge der Daten im Befehl nach diesem Byte immer 09
# aa - Attribute: 0 or 1 - delete, 2 - draw, 3 - draw Cursor. Cursor independant of h-w values and the same every time
# 00 - Spacer byte
# xx - X-Coord in Pixels. 0 ist die obere linke Ecke des Bildschirmbereichs, auf den gezeichnet werden soll.
# yy - Y-Coord
# ww - Width
# hh - Height
# !!! x-y must be inside of screen, h-w can be outside !!!

# -Text
# 61 ll aa ii xx 00 yy 00 tt .... tt
# 61 der Befehl ist
# ll - Länge der Daten im Befehl nach diesem Byte unter Berücksichtigung der Anzahl der Textzeichen
# aa - Fontattribute: (lowest Nibble) x0 - normal, x1 - bold 1, x2 - bold 2, x3 - chinese, x4 - smol , x5 - Graphic Glyph , (high Nibble) 0x - left direction, 1x - centered from X-Coordinate
# ii - Inversion attribute: 0 - normal, 2 - inverse
# xx - X-Coord, in Pixel. 0 ist die obere linke Ecke des Bildschirmbereichs, auf den gezeichnet werden soll.
# yy - Y-Coord
# tt - Text
# 00 - Spacer Byte
#-----------------------------------------------------------------------------------------------------------------------------

# 0x02 - DDP Channel Login OPCODE
# 0x02 | 0xME | 0xID | 0xSS | 0xTT | 0xTT | 0xTT
# 0xME : 0x70 			= With Menu Entry
#		 0x71 - 0x85	= Without Menu Entry (Lower = Higher Priority) 
# 0xID : Device ID
# 0xSS : 0x00 = All segments
#		 0x10 = Middle Segment
#		 0x20 = Upper Segment
#		 0x30 = Lower Segment
#		 0x40 = Upper + Middle Segment
#		 0x50 = Lower + Middle Segment
# 0xTT : Ascii text of unknown length

*/