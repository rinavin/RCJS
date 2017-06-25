using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Logging
{
   /// <summary>
   /// Implements ILogWriter so that messages can be written to the
   /// console window.
   /// </summary>
   internal class ConsoleLogWriter : ILogWriter
   {
      public void Write(string message)
      {
         Console.Out.Write(message);
      }

      public void WriteLine(string message)
      {
         Console.Out.WriteLine(message);
      }

      public void Flush()
      {
         Console.Out.Flush();
      }
   }
}
