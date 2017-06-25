using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices; // DllImport
using com.magicsoftware.unipaas;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.util
{
   internal class Vis2Log
   {
      /// <summary> Vis_2_Log ()
      /// </summary>
      [DllImport(ConstInterface.V2L_DLL, CallingConvention = CallingConvention.Winapi)]
      internal static extern void Vis_2_Log([MarshalAs(UnmanagedType.LPStr)] StringBuilder Dst, [MarshalAs(UnmanagedType.U2)] UInt16 DstLen, [MarshalAs(UnmanagedType.LPStr)] StringBuilder Src, int SrcLen,
        [MarshalAs(UnmanagedType.Bool)] bool bLTR, [MarshalAs(UnmanagedType.Bool)] bool bDosIn,
        [MarshalAs(UnmanagedType.Bool)] bool bDosOut, [MarshalAs(UnmanagedType.Bool)] bool bWithCtrls);

      /// <summary> Log_2_Vis ()
      /// </summary>
      [DllImport(ConstInterface.V2L_DLL, CallingConvention = CallingConvention.Winapi)]
      internal static extern void Log_2_Vis([MarshalAs(UnmanagedType.LPStr)] StringBuilder Dst, [MarshalAs(UnmanagedType.LPStr)] StringBuilder Src, int SrcLen, 
         [MarshalAs(UnmanagedType.Bool)] bool bLTR, [MarshalAs(UnmanagedType.Bool)] bool bDosIn, [MarshalAs(UnmanagedType.Bool)] bool bDosOut);

   }

}
