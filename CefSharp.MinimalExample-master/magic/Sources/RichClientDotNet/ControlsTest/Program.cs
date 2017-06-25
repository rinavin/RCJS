using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ControlsTest
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
#if !PocketPC
      [STAThread]
#endif
      static void Main()
      {
#if !PocketPC
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
#endif
         Application.Run(new Form1());
      }
   }
}
