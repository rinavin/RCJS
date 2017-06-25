using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace com.magicsoftware.util.Logging
{
   /// <summary>
   /// Implementation of ILogWriter to supersede another log writer and accumulate messages 
   /// directed to it. Eventually, the using component can decide whether to flush the accumulated
   /// messages to the superseded log writer or to discard the accumulated messages.
   /// </summary>
   internal class LogAccumulator : ILogWriter, IDisposable
   {
      internal delegate void DisposalCallback();

      StringBuilder accumulatedMessages = new StringBuilder();
      DisposalCallback disposalCallback;

      public ILogWriter SupersededWriter { get; private set; }

      public LogAccumulator(ILogWriter supersededWriter, DisposalCallback disposalCallback)
      {
         SupersededWriter = supersededWriter;
         this.disposalCallback = disposalCallback;
      }

      public void Write(string message)
      {
         accumulatedMessages.Append(message);
      }

      public void WriteLine(string message)
      {
#if PocketPC
         accumulatedMessages.Append(message + "\r\n");
#else
         accumulatedMessages.AppendLine(message);
#endif
      }

      public void Flush()
      {
         if (accumulatedMessages.Length > 0)
            SupersededWriter.Write(accumulatedMessages.ToString());
         Discard();
      }

      public void Dispose()
      {
         Debug.Assert(accumulatedMessages.Length == 0, "Messages must be discarded or flushed before invoking Dispose.");
         if (disposalCallback != null)
            disposalCallback();
         disposalCallback = null;
      }

      public void Discard()
      {
         accumulatedMessages = new StringBuilder();
      }
   }
}
