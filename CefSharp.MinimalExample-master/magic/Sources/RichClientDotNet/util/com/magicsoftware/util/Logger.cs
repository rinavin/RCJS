using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
#if !PocketPC
using System.Media;
#endif
using System.IO;
using System.Reflection;
using System.Threading;
using com.magicsoftware.util;
using com.magicsoftware.util.Logging;
using System.Net;
#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace util.com.magicsoftware.util
{
#if PocketPC
   public class StackTrace
   {
      // Dummy class.

      public StackTrace(int i, bool b) {}
   }
#endif

   /// <summary>
   /// Logger class will take care of client side logging . It will check for various log levels and accordingly will write messages in log file.
   /// </summary>
   public class Logger
   {
      /// <summary>
      /// Log levels enum
      /// </summary>
      public enum LogLevels
      {
         None = 0,
         Server = 1,
         ServerMessages = 2,
         Support = 3,
         Gui = 4,
         Development = 5,
         Basic = 6
      }

      public enum MessageDirection
      {
         MessageLeaving = 0,
         MessageEntering = 1
      }

      public LogLevels LogLevel { get; set; } // InternalLogLevel
      private ILogWriter _logWriter;
      private LogAccumulator _accumulator;

      /// <summary>
      /// While writing the error messages in the file play the beep.
      /// </summary>
      public bool ShouldBeep { get; set; }

      /// <summary>
      /// Constructor
      /// </summary>
      public Logger()
      {
         _logWriter = new ConsoleLogWriter();
         _accumulator = null;
      }

      private static Logger instance;
      public static Logger Instance
      {
         get
         {
            if (instance == null)
            {
               lock (typeof(Logger))
               {
                  if (instance == null)
                     instance = new Logger();
               }
            }
            return instance;
         }

         private set
         {
            instance = value;
         }
      }

      /// <summary>
      /// Initialize logger
      /// </summary>
      /// <param name="internalLogFileName"></param>
      /// <param name="logLevel"></param>
      /// <param name="internalLogSync"></param>
      public void Initialize(string internalLogFileName, LogLevels logLevel, string internalLogSync, bool shouldBeep)
      {
         try
         {
            LogSyncMode logSync = LogSyncMode.Session;
            this.LogLevel = logLevel;
            this.ShouldBeep = shouldBeep;

            String strLogSync = internalLogSync;
            if (!string.IsNullOrEmpty(strLogSync))
            {
               if (strLogSync.StartsWith("M", StringComparison.CurrentCultureIgnoreCase))
                  logSync = LogSyncMode.Message;
               else if (strLogSync.StartsWith("F", StringComparison.CurrentCultureIgnoreCase))
                  logSync = LogSyncMode.Flush;
            }

            _logWriter = new FileLogWriter(internalLogFileName, logSync, _logWriter);
         }
         catch (Exception e)
         {
            WriteDevToLog("ClientManager.init(): " + e.Message);
         }
      }


      /// <summary></summary>
      /// <param name="logLevel"></param>
      /// <returns></returns>
      public bool ShouldLog(LogLevels logLevel)
      {
         return (this.LogLevel == logLevel);
      }

      /// <summary>
      /// </summary>
      /// <returns>true if any log level other than None was set.</returns>
      public bool ShouldLog()
      {
         return (this.LogLevel != LogLevels.None);
      }

      /// <summary>
      /// </summary>
      /// <returns>true if server related messages should be written to the log.</returns>
      public bool ShouldLogServerRelatedMessages()
      {
         return ((ShouldLogExtendedServerRelatedMessages()
                 || Logger.Instance.ShouldLog(LogLevels.Server)) &&
                 LogLevel != LogLevels.Basic);
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      public bool ShouldLogExtendedServerRelatedMessages()
      {
         return ((Logger.Instance.ShouldLog(LogLevels.ServerMessages)
                 || Logger.Instance.ShouldLog(LogLevels.Support)
                 || Logger.Instance.ShouldLog(LogLevels.Development)) &&
                 LogLevel != LogLevels.Basic);
      }

      /// <summary></summary>
      /// <param name="msg"></param>
      /// <param name="openIfNecessary">open the log file if not opened yet</param>
      public void WriteToLog(String msg, bool openIfNecessary)
      {
         if (LogLevel != LogLevels.None || openIfNecessary)
         {
            msg = string.Format("{0} {1}{2}",
                                (LogLevel == LogLevels.Basic
                                    ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                                    : DateTimeUtils.ToString(DateTime.Now, XMLConstants.ERROR_LOG_TIME_FORMAT)),
                                (Misc.IsGuiThread() || Misc.IsWorkThread() // write thread id only for special threads, i.e. not the gui/main or work threads.
                                    ? ""
                                    : string.Format("Thread #{0}, ", Thread.CurrentThread.ManagedThreadId)),
                                msg);
            _logWriter.WriteLine(msg);
         }
      }

      /// <summary> 
      /// write a server access to the log
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteServerToLog(String msg)
      {
         if (ShouldLogServerRelatedMessages())
            WriteToLog(string.Format("Server, Thread={0}: ", Thread.CurrentThread.ManagedThreadId)  + msg, false);
      }

      /// <summary> 
      /// write a server access to the log, including the content
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteServerMessagesToLog(String msg)
      {
         if (ShouldLogExtendedServerRelatedMessages())
            WriteToLog(string.Format("Server#, Thread={0}: ", Thread.CurrentThread.ManagedThreadId) + msg, false);
      }

      /// <summary>Write a QC message to the log</summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteSupportToLog(String msg, bool skipLine)
      {
         if (LogLevel >= LogLevels.Support && LogLevel != LogLevels.Basic)
         {
            if (skipLine)
               WriteToLog("SUPPORT: " + msg, false);
            else
            {
               WriteToLog(
                  "SUPPORT: " + msg + OSEnvironment.EolSeq +
                  "-----------------------------------------------------------------------------------------------------------",
                  false);
            }
         }
      }

      /// <summary> 
      /// write a performance message to the log
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteGuiToLog(String msg)
      {
         if (LogLevel >= LogLevels.Gui && LogLevel != LogLevels.Basic)
            WriteToLog(msg, false);
      }

      /// <summary>
      /// write a developer message to the log
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteDevToLog(String msg)
      {
         if (LogLevel >= LogLevels.Development && LogLevel != LogLevels.Basic)
            WriteToLog("DEV: " + msg, false);
      }


      /// <summary>
      /// Writes a basic level entry to log
      /// </summary>
      /// <param name="messageDirection">message direction relative to the current module (RIA client). Can be either MessageEntering or MessageLeaving</param>
      /// <param name="statusCode">HTTP status code</param>
      /// <param name="contentLength">length of the http message</param>
      /// <param name="httpHeaders">HTTP headers</param>
      public void WriteBasicToLog(MessageDirection messageDirection, String contextID, long sessionCounter, String clientID,
                                  String serverID, long responseTime, HttpStatusCode statusCode, WebHeaderCollection httpHeaders, Int64 contentLength)
      {
         if (LogLevel == LogLevels.Basic)
         {
            String msg;
            String httpHeadersString = httpHeaders.ToString();
            httpHeadersString = httpHeadersString.Trim();
            httpHeadersString = httpHeadersString.Replace("\r\n", "|");
            msg = string.Format("RIA,{0}_{1},{2},{3},{4},{5},-,{6},{7},{8},{9},{10},{11}",
                                System.Diagnostics.Process.GetCurrentProcess().Id,
                                Thread.CurrentThread.ManagedThreadId,
                                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                                (messageDirection == MessageDirection.MessageLeaving ? "MSGL" : "MSGE"),
                                contextID, sessionCounter, clientID, serverID, 
                                (responseTime != 0 ? responseTime.ToString() : "-"),
                                (statusCode != HttpStatusCode.Unused ? ((int)statusCode).ToString() : "-"), 
                                httpHeadersString, contentLength);
            _logWriter.WriteLine(msg);
         }
      }

      /// <summary>
      /// Writes a request exception basic level entry to log
      /// </summary>
      /// <param name="contextID"></param>
      /// <param name="sessionCounter"></param>
      /// <param name="clientID"></param>
      /// <param name="serverID"></param>
      /// <param name="ex">the logged exception</param> 
      public void WriteBasicErrorToLog(String contextID, long sessionCounter, String clientID, String serverID, Exception ex)
      {
         Debug.Assert(LogLevel == LogLevels.Basic);

         String msg;
         msg = string.Format("RIA,{0}_{1},{2},{3},{4},{5},-,{6},{7},-,-,-,{8} {9}",
                             System.Diagnostics.Process.GetCurrentProcess().Id,
                             Thread.CurrentThread.ManagedThreadId,
                             DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                             "RES", contextID, sessionCounter, clientID, serverID, ex.GetType(), ex.Message);
         _logWriter.WriteLine(msg);
      }

      /// <summary> 
      /// Write an error to the log
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteErrorToLog(String msg)
      {
         WriteToLog("ERROR: " + msg, true);

         //Product #178, Topic #61: beep on the client side only upon user errors (e.g. duplicate record), otherwise only if started from the studio (F7)
         if (ShouldBeep)
            SoundBeep();
      }

      /// <summary> 
      /// Write an internal error to the log. Also prints stack trace along with the message
      /// </summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteExceptionToLog(String msg)
      {
         WriteToLog("ERROR: " + msg + OSEnvironment.getStackTrace(), true);

         //Product #178, Topic #61: beep on the client side only upon user errors (e.g. duplicate record), otherwise only if started from the studio (F7)
         if (ShouldBeep)
            SoundBeep();
      }

      public void WriteExceptionToLog(Exception ex)
      {
         WriteExceptionToLog(string.Format("{0} : {1}{2}{3}{4}",
                                       ex.GetType(), OSEnvironment.EolSeq,
                                       ex.StackTrace, OSEnvironment.EolSeq,
                                       ex.Message));
      }

      public void WriteExceptionToLog(Exception ex, string msg)
      {
         WriteExceptionToLog(string.Format("{0}, {1} : {2}{3}{4}{5}",
                                       ex.GetType(), msg, OSEnvironment.EolSeq,
                                       ex.StackTrace, OSEnvironment.EolSeq,
                                       ex.Message));
      }

      /// <summary> write a warning to the log</summary>
      /// <param name="msg">the message to write to the log</param>
      public void WriteWarningToLog(String msg)
      {
         if (LogLevel != LogLevels.Basic)
            WriteToLog("WARNING: " + msg, true);

         //Product #178, Topic #61: beep on the client side only upon user errors (e.g. duplicate record), otherwise only if started from the studio (F7)
         if (ShouldBeep)
            SoundBeep();
      }

      public void WriteWarningToLog(Exception ex)
      {
         WriteWarningToLog(ex.GetType() + " : " + OSEnvironment.EolSeq +
                           ex.StackTrace + OSEnvironment.EolSeq +
                           ex.Message);
      }

      public void WriteWarningToLog(Exception ex, string msg)
      {
         WriteWarningToLog(string.Format("{0}, {1} : {2}{3}{4}{5}",
                                         ex.GetType(), msg, OSEnvironment.EolSeq,
                                         ex.StackTrace, OSEnvironment.EolSeq,
                                         ex.Message));
      }

      /// <summary>
      /// Write the framesToPrint stack frames to the log. The resulting stack trace
      /// will begin from the calling method.
      /// </summary>
      /// <param name="framesToPrint">The number of frames to print, starting at the calling method.</param>
      public void WriteStackTrace(uint framesToPrint)
      {
         WriteStackTrace(framesToPrint, 2);
      }

      /// <summary>
      /// Write the framesToPrint stack frames to the log. The resulting stack trace
      /// will skip the first framesToSkip frames (counting includes this method as well).
      /// If the actual stack trace has more frames than printed, the printing will end
      /// with a message denoting that there are more stack frames down the road.
      /// </summary>
      /// <param name="framesToPrint">The number of frames to print, starting at the first unskipped frame.</param>
      public void WriteStackTrace(uint framesToPrint, int framesToSkip)
      {
#if !PocketPC
         StackTrace stackTrace = new StackTrace(framesToSkip, true);
         WriteStackTrace(stackTrace, framesToPrint, null);
#endif
      }

      public void WriteStackTrace(StackTrace stackTrace, uint framesToPrint, string traceTitle)
      {
#if !PocketPC
         if (traceTitle == null)
            traceTitle = "Stack trace:";
         StringBuilder formattedMessage = new StringBuilder(traceTitle + OSEnvironment.EolSeq);
         StackFrame[] frames = stackTrace.GetFrames();
         foreach (StackFrame frame in frames)
         {
            framesToPrint--;

            formattedMessage.AppendFormat("   + {0}.{1} - {2} ({3})", frame.GetMethod().DeclaringType.FullName, frame.GetMethod().Name, frame.GetFileName(), frame.GetFileLineNumber()).AppendLine();

            if (framesToPrint == 0)
            {
               // Add a message at the end, to denote that there are more stack frames
               // further down the list.
               formattedMessage.Append("\t... more stack frames ...\n");
               break;
            }
         }
         WriteToLog(formattedMessage.ToString(), true);
#endif
      }

      /// <summary>
      ///   beep
      /// </summary>
      internal void SoundBeep()
      {
#if !PocketPC
         SystemSounds.Beep.Play();
#else
         Beep.Play();
#endif
      }

      /// <summary>
      /// Flush the log writer.
      /// </summary>
      public void Flush()
      {
         _logWriter.Flush();
      }

      /// <summary>
      /// Accumulate messages instead of writing them to current log writer.
      /// </summary>
      /// <returns>Returns a IDisposable object that can be used in a 'using' clause to ensure
      /// the message accumulation is stopped.</returns>
      public IDisposable AccumulateMessages()
      {
         _accumulator = new LogAccumulator(_logWriter, StopMessageAccumulation);
         _logWriter = _accumulator;
         return _accumulator;
      }

      /// <summary>
      /// Stops the current message accumulation. If the current accumulation
      /// supersedes another accumulation, the superseded accumulation will be
      /// in effect.
      /// </summary>
      public void StopMessageAccumulation()
      {
         if (_accumulator == null)
            return;
         _logWriter = _accumulator.SupersededWriter;
         _accumulator = _logWriter as LogAccumulator;  // This may result with null. 
         // FOR PORTING: Use if (_logWriter is LogAccumulator)...
      }

      /// <summary>
      /// Flushes the accumulated messages to the superseded log writer.
      /// </summary>
      public void FlushAccumulatedMessages()
      {
         Debug.Assert(_accumulator != null);
         if (_accumulator != null)
            _accumulator.Flush();
      }

      /// <summary>
      /// Discards accumulated messages in the last created accumulator. Superseded
      /// accumulators are not necessarily affected (depends on current implementation
      /// of LogAccumulator).
      /// </summary>
      public void DiscardAccumulatedMessages()
      {
         Debug.Assert(_accumulator != null);
         if (_accumulator != null)
            _accumulator.Discard();
      }

      /// <summary>
      /// Returns true if accumulating messages
      /// </summary>
      public bool IsAccumulatingMessages()
      {
         return (_accumulator != null);
      }
   }
}
