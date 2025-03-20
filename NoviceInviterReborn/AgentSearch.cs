using System;
using System.Runtime.InteropServices;

namespace NoviceInviterReborn
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AgentSearch
    {
        // Online status fields
        [FieldOffset(0xB4)]
        public byte OnlineStatusLeft; // Should be set to 0

        [FieldOffset(0xB5)]
        public byte OnlineStatusRight; // 1 for sprouts only, 3 for sprouts + returner

        // Class search field
        [FieldOffset(0xB8)]
        public byte ClassSearchRow1; // 255 = all

        [FieldOffset(0xB9)]
        public byte ClassSearchRow2; // 255 = all

        [FieldOffset(0xBA)]
        public byte ClassSearchRow3; // 255 = all

        [FieldOffset(0xBB)]
        public byte ClassSearchRow4; // 255 = all

        [FieldOffset(0xBC)]
        public byte ClassSearchRow5; // 255 = all

        [FieldOffset(0xBD)]
        public byte ClassSearchRow6; // 3 = all

        // Level search fields
        [FieldOffset(0xC0)]
        public int MinLevel;

        [FieldOffset(0xC4)]
        public int MaxLevel;

        // Language and company fields
        [FieldOffset(0xC8)]
        public byte Language; // Should always be 15 for include all

        [FieldOffset(0xC9)]
        public byte Company; // Should always be 7 to include all

        // Area search fields - 11 areas with 5 bytes each
        [FieldOffset(0x113)]
        public byte Area1Byte1;
        [FieldOffset(0x114)]
        public byte Area1Byte2;
        [FieldOffset(0x115)]
        public byte Area1Byte3;
        [FieldOffset(0x116)]
        public byte Area1Byte4;
        [FieldOffset(0x117)]
        public byte Area1Byte5;

        [FieldOffset(0x118)]
        public byte Area2Byte1;
        [FieldOffset(0x119)]
        public byte Area2Byte2;
        [FieldOffset(0x11A)]
        public byte Area2Byte3;
        [FieldOffset(0x11B)]
        public byte Area2Byte4;
        [FieldOffset(0x11C)]
        public byte Area2Byte5;

        // Area 3
        [FieldOffset(0x11D)]
        public byte Area3Byte1;
        [FieldOffset(0x11E)]
        public byte Area3Byte2;
        [FieldOffset(0x11F)]
        public byte Area3Byte3;
        [FieldOffset(0x120)]
        public byte Area3Byte4;
        [FieldOffset(0x121)]
        public byte Area3Byte5;

        // Area 4
        [FieldOffset(0x122)]
        public byte Area4Byte1;
        [FieldOffset(0x123)]
        public byte Area4Byte2;
        [FieldOffset(0x124)]
        public byte Area4Byte3;
        [FieldOffset(0x125)]
        public byte Area4Byte4;
        [FieldOffset(0x126)]
        public byte Area4Byte5;

        // Area 5
        [FieldOffset(0x127)]
        public byte Area5Byte1;
        [FieldOffset(0x128)]
        public byte Area5Byte2;
        [FieldOffset(0x129)]
        public byte Area5Byte3;
        [FieldOffset(0x12A)]
        public byte Area5Byte4;
        [FieldOffset(0x12B)]
        public byte Area5Byte5;

        // Area 6
        [FieldOffset(0x12C)]
        public byte Area6Byte1;
        [FieldOffset(0x12D)]
        public byte Area6Byte2;
        [FieldOffset(0x12E)]
        public byte Area6Byte3;
        [FieldOffset(0x12F)]
        public byte Area6Byte4;
        [FieldOffset(0x130)]
        public byte Area6Byte5;

        // Area 7
        [FieldOffset(0x131)]
        public byte Area7Byte1;
        [FieldOffset(0x132)]
        public byte Area7Byte2;
        [FieldOffset(0x133)]
        public byte Area7Byte3;
        [FieldOffset(0x134)]
        public byte Area7Byte4;
        [FieldOffset(0x135)]
        public byte Area7Byte5;

        // Area 8
        [FieldOffset(0x136)]
        public byte Area8Byte1;
        [FieldOffset(0x137)]
        public byte Area8Byte2;
        [FieldOffset(0x138)]
        public byte Area8Byte3;
        [FieldOffset(0x139)]
        public byte Area8Byte4;
        [FieldOffset(0x13A)]
        public byte Area8Byte5;

        // Area 9
        [FieldOffset(0x13B)]
        public byte Area9Byte1;
        [FieldOffset(0x13C)]
        public byte Area9Byte2;
        [FieldOffset(0x13D)]
        public byte Area9Byte3;
        [FieldOffset(0x13E)]
        public byte Area9Byte4;
        [FieldOffset(0x13F)]
        public byte Area9Byte5;

        // Area 10
        [FieldOffset(0x140)]
        public byte Area10Byte1;
        [FieldOffset(0x141)]
        public byte Area10Byte2;
        [FieldOffset(0x142)]
        public byte Area10Byte3;
        [FieldOffset(0x143)]
        public byte Area10Byte4;
        [FieldOffset(0x144)]
        public byte Area10Byte5;

        // Area 11
        [FieldOffset(0x145)]
        public byte Area11Byte1;
        [FieldOffset(0x146)]
        public byte Area11Byte2;
        [FieldOffset(0x147)]
        public byte Area11Byte3;
        [FieldOffset(0x148)]
        public byte Area11Byte4;
        [FieldOffset(0x149)]
        public byte Area11Byte5;

        public struct BackupData
        {
            public byte OnlineStatusLeft;
            public byte OnlineStatusRight;
            public byte ClassSearch;
            public int MinLevel;
            public int MaxLevel;
            public byte Language;
            public byte Company;
            public byte[] AreaData;

            public BackupData()
            {
                OnlineStatusLeft = 0;
                OnlineStatusRight = 0;
                ClassSearch = 0;
                MinLevel = 0;
                MaxLevel = 0;
                Language = 0;
                Company = 0;
                AreaData = new byte[5 * 11]; // 5 bytes per area, 11 areas
            }
        }
    }
}