using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace com.magicsoftware.util
{
   /// <summary>
   /// The class simulates the keyboard actions. 
   /// </summary>
   public class SimulateKeyboard
   {
      /// <summary>
      /// Simulate keyboard messages - make the system re-send the message as if the user typed it again.
      /// </summary>
      /// <param name="msg"></param>
      public static void Simulate(Message msg)
      {
         // build the "inputs" structures
         NativeWindowCommon.INPUT[] inputs = new NativeWindowCommon.INPUT[]
         {
            new NativeWindowCommon.INPUT
            {
               type = NativeWindowCommon.INPUT_KEYBOARD,
               u = new NativeWindowCommon.InputUnion
               {
                  ki = new NativeWindowCommon.KEYBDINPUT
                  {
                      wVk = (ushort)msg.WParam,
                      wScan = 0,
                      dwFlags = (msg.Msg == NativeWindowCommon.WM_KEYDOWN || msg.Msg == NativeWindowCommon.WM_SYSKEYDOWN) ? 0 : NativeWindowCommon.KEYEVENTF_KEYUP,
                      dwExtraInfo = NativeWindowCommon.GetMessageExtraInfo(),
                  }
              }
            }
         };

         // send the structure
         NativeWindowCommon.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeWindowCommon.INPUT)));
      }
   }
}
