using System;
using System.Diagnostics;

namespace RichClientPreBuild
{
   class Program
   {
      static void Main(string[] args)
      {
         try
         {
            Process[] processes = Process.GetProcessesByName("MgxpaRIA");

            foreach (var process in processes)
            {
               if (process.MainWindowTitle != "R & D Management System")
                  process.Kill();
            }
         }
         catch (Exception e)
         {
            Debug.WriteLine("Exception in " + Process.GetCurrentProcess().ProcessName);
            Debug.WriteLine(e);
         }
      }
   }
}
