using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Logging
{
   /// <summary>
   /// Defines the methods for writing messages to a logging device, as needed by
   /// the Logger class.
   /// </summary>
   internal interface ILogWriter
   {
      /// <summary>
      /// Writes the message as is to the logging device.
      /// </summary>
      /// <param name="message">The message to be logged</param>
      void Write(string message);

      /// <summary>
      /// Writes the message as is to the logging device and adds a new line
      /// at the end of the message.
      /// </summary>
      /// <param name="message">The message to be logged</param>
      void WriteLine(string message);

      /// <summary>
      /// Ensures the content accumulated by the implementing class or sub-components is
      /// indeed written to the logging device.
      /// </summary>
      void Flush();
   }
}
