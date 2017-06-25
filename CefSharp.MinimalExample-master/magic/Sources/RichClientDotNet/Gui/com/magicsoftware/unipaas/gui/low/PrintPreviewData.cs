using System;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Contains information required for Print Preview methods
   /// </summary>
   internal class PrintPreviewData
   {
      internal bool boolVal;
      internal int extendedStyle;
      internal int style;
      internal int number1;
      internal int number2;
      internal int number3;
      internal int number4;
      internal Int64 Int64Val;
      internal IntPtr intPtr1;
      internal IntPtr intPtr2;
      internal IntPtr intPtr3;
      internal IntPtr intPtr4;
      internal Form callerWindow;
      internal String className;
      internal String windowName;

      internal PrintPreviewData()
      {
      }
   }
}
