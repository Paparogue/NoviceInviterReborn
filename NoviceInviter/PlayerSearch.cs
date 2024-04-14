using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NoviceInviter
{
    internal class PlayerSearch
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PlayerData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string PlayerName;
        }
    }
}
