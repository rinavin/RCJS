using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.util.Logging
{
   /// <summary>
   /// Writes log messages to a file.
   /// </summary>
   internal class FileLogWriter : ILogWriter
   {
#if PocketPC
      const string NewLine = "\r\n";
#else
      static readonly string NewLine;

      static FileLogWriter()
      {
         NewLine = System.Environment.NewLine;
      }
#endif

      private StreamWriter writer;
      private Boolean autoFlush;
      private Boolean closeAfterWrite;
      private String logTarget;
      private ILogWriter fallbackWriter;

      /// <summary>
      /// Instantiates a new file log writer.
      /// </summary>
      /// <param name="fileName">The name of the file to which messages will be written.</param>
      /// <param name="logSync">The log synchronization mode.</param>
      /// <param name="fallbackWriter">An ILogWriter that can be used in case the instantiated writer is
      /// unable to access the target file. This parameter may be null.</param>
      public FileLogWriter(string fileName, LogSyncMode logSync, ILogWriter fallbackWriter)
      {
         this.fallbackWriter = fallbackWriter;

         logTarget = fileName;

         if (logSync == LogSyncMode.Message)
            closeAfterWrite = true;
         else if (logSync != LogSyncMode.Session)
            autoFlush = true;
      }

      /// <summary> 
      /// Try to open the StreamWriter. If failed - try to change the log file name
      /// </summary>
      private void Open()
      {
         if (!String.IsNullOrEmpty(logTarget))
         {
            try
            {
               writer = new StreamWriter(new FileStream(logTarget, FileMode.Append, FileAccess.Write, FileShare.Read));
            }
            catch (DirectoryNotFoundException e)
            {
               // No such directory - fail
               logTarget = null;
               FallbackWrite("Exception while processing internal log file name. " + e.Message, NewLine);
            }
            catch (IOException)
            {
               // if the log file is in use, insert the current process ID into the name: *.pid.suffix
               logTarget = logTarget.Insert(logTarget.LastIndexOf('.'),
                                            "." + System.Diagnostics.Process.GetCurrentProcess().Id);
               try
               {
                  writer = new StreamWriter(new FileStream(logTarget, FileMode.Append, FileAccess.Write, FileShare.Read));
               }
               catch
               {
                  logTarget = null;
               }
            }
            if (writer != null)
               writer.AutoFlush = autoFlush;
         }
      }

      /// <summary> close the StreamWriter </summary>
      private void Close()
      {
         if (writer != null)
         {
            writer.Close();
            writer = null;
         }
      }

      /// <summary> 
      /// thread-safe write to the log file.
      /// </summary>
      /// <param name="msg"></param>
      public void Write(String msg)
      {
         lock (this)
         {
            if (!WriteToStream(msg))
               FallbackWrite(msg);
         }
      }

      /// <summary> 
      /// thread-safe write to the log file, adding a new line at the end of the message.
      /// </summary>
      /// <param name="msg"></param>
      public void WriteLine(String msg)
      {
         lock (this)
         {
            if (!WriteToStream(msg, NewLine))
               FallbackWrite(msg, NewLine);
         }
      }

      /// <summary>
      /// Tries writing the messages to the stream, opening and closing the stream as needed.
      /// </summary>
      /// <param name="msgs">The messages to be written to the stream.</param>
      /// <returns>The method returns <c>true</c> if it successfully writes the messages to the stream. Otherwise
      /// it returns <c>false</c></returns>
      private bool WriteToStream(params string[] msgs)
      {
         if (writer == null)
            Open();

         // Did open fail?
         if (writer == null)
            return false;

         foreach (var msg in msgs)
            writer.Write(msg);

         if (closeAfterWrite)
            Close();

         return true;

      }

      /// <summary>
      /// Writes the messages to the fallback writer, if one is set.
      /// </summary>
      /// <param name="msgs">The messages to be written.</param>
      private void FallbackWrite(params string[] msgs)
      {
         if (fallbackWriter != null)
         {
            foreach (var msg in msgs)
               fallbackWriter.Write(msg);
            fallbackWriter.Flush();
         }
      }

      /// <summary>
      /// Flush unwritten data to the file
      /// </summary>
      public void Flush()
      {
         if (writer != null)
            writer.Flush();
      }
   }
}
