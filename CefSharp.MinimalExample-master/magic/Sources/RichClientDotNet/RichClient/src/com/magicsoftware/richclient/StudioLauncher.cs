using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient
{
   /// <summary> Rich client launcher. This class is used when RC program is run from Development (i.e F7).
   /// On pressing F7 we spawn the rich client process. RTE then acts as server and starts listening 
   /// on a port while this class connects to RTE and receives either execution properties or Terminate request.
   /// If execution properties are received Launcher will start ClientManager otherwise Launcher will silently terminate</summary>
   /// <author>  rajendrap</author>
   internal class StudioLauncher
   {
      /// <summary>
      /// 
      /// </summary>
      private class Response
      {
         internal static readonly Response OK = new Response("0");
         internal static readonly Response FAILED = new Response("1");

         /// <param name="val"></param>
         internal Response(String val)
         {
            Val = val;
         }

         internal String Val { get; private set; }
      }

      private const int CHUNCK_SIZE = 1024;
      private const String RC_LAUNCHER_OK = "<StudioLauncher>OK</StudioLauncher>";

      private readonly int _bindPort; // Port on which Server is listening
      private readonly string _serverUrl;
      private StreamWriter _logWriter; // StudioLauncher log

      /// <summary> Creates new socket and try to connect to server</summary>
      /// <returns> InputStream of the socket</returns>
      internal StudioLauncher(int port, string serverUrl)
      {
         _bindPort = port;
         _serverUrl = serverUrl;
      }

      /// <summary> main execution function of StudioLauncher.</summary>
      internal void Execute()
      {
         InitLogWriter();

         try
         {
            System.Console.Out.WriteLine("StudioLauncher : Started");

            //Let the RTE know that StudioLauncher is started and it can continue to send execution props.
            SendRCIsOk();

            //connect to the RTE
            System.Net.Sockets.NetworkStream stream = null;
            System.Net.Sockets.TcpClient rcSock = new System.Net.Sockets.TcpClient("127.0.0.1", _bindPort);
            if (rcSock != null)
            {
               stream = rcSock.GetStream();
               rcSock = null;
            }

            //If connected read the execution props file.
            if (stream != null)
            {
               //#201341: send fictive HTTP request for automatically detecting settings when the client is pre-spawned.
               //This saves much time during F7 execution.
               ClientManager.PreSpawnHTTP(_serverUrl);
               String receivedStr = Read(stream);
               if (receivedStr != null)
               {
                  try
                  {
                     Write(stream, Response.OK);

                     // Start executing the client - load execution properties, initialize the session, execute the requested program, ..</summary>
                     //-------------------------------------------
                     // Split('|'): The property 'LocalID' is used by the client to direct execution to the current runtime-engine.
                     // This property shouldn't be saved to the execution properties file, since it's only valid for the current execution.
                     // When the execution properties file is used from RC mobile, for example, or later from Visual Studio, 
                     //    this property might block execution in case the runtime-engine was restarted to another port.
                     ClientManager.StartExecution(receivedStr.Split('|'));
                  }
                  catch (Exception ex)
                  {
                     WriteErrorToLog(ex.Message);
                     Write(stream, Response.FAILED);
                  }
               }
               else
               {
                  Debug.Assert(false);
                  String errorMsg = "No data received from Server";
                  WriteErrorToLog(errorMsg);
                  throw new Exception(errorMsg);
               }
            }
            else
               WriteErrorToLog("Failed to read NetworkStream");
         }
         catch (Exception ex)
         {
            WriteErrorToLog(ex.Message);
            throw;
         }
      }

      /// <summary> Helper function to read from inputStream</summary>
      /// <param name="in">InputStream </param>
      /// <returns> String containing received buffer.</returns>
      private String Read(System.Net.Sockets.NetworkStream stream)
      {
         String receivedStr = null;

         byte[] buff = new byte[CHUNCK_SIZE];
         int len = stream.Read(buff, 0, buff.Length);
         if (len > 0)
            receivedStr = Encoding.Default.GetString(buff, 0, len);

         return receivedStr;
      }

      /// <param name="out"></param>
      /// <param name="string"></param>
      /// <throws>  IOException  </throws>
      private void Write(System.Net.Sockets.NetworkStream stream, Response response)
      {
         byte[] msg = System.Text.Encoding.ASCII.GetBytes(response.Val);
         stream.Write(msg, 0, msg.Length);
      }

      /// <summary> Write <StudioLauncher>OK</RClauncher> on error stream. On receiving this RTE will
      /// stop reading from new error stream and will continue. (to send execution props)</summary>
      private void SendRCIsOk()
      {
         System.Console.Error.WriteLine(RC_LAUNCHER_OK);
         WriteToLog(RC_LAUNCHER_OK);
      }

      /// <summary>Initialize StudioLauncher error log --> %TEMP%\RCLauncherError.log</summary>
      private void InitLogWriter()
      {
         String logTarget = OSEnvironment.get("TEMP") + @"\RCLauncherError.log";

         try
         {
            _logWriter = new StreamWriter(new FileStream(logTarget, FileMode.Append, FileAccess.Write, FileShare.Read));
         }
         catch (IOException)
         {
            // if the log file is in use, insert the current process ID into the name: *.pid.suffix
            logTarget = logTarget.Insert(logTarget.LastIndexOf('.'), "." + System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            try
            {
               _logWriter = new StreamWriter(new FileStream(logTarget, FileMode.Append, FileAccess.Write, FileShare.Read));
            }
            catch { }
         }
         catch { }
         if (_logWriter != null)
            _logWriter.AutoFlush = true;
      }

      /// <summary>Write message to log file</summary>
      private void WriteToLog(String msg)
      {
         msg = string.Format("{0} {1}", DateTimeUtils.ToString(DateTime.Now, XMLConstants.ERROR_LOG_TIME_FORMAT), msg);
         _logWriter.WriteLine(msg);
      }

      /// <summary>Write error to log</summary>
      /// <param name="msg"></param>
      private void WriteErrorToLog(String msg)
      {
         WriteToLog("ERROR: " + msg);
      }

      /// <summary> main method of StudioLauncher.</summary>
      /// <param name="args"></param>
      [STAThread]

      // Main function of  RClauncher. Called from ClientManager.Main ().
      // It will call execute which will wait on given port till we get terminate or properties file.
      internal static void Init(String[] args)
      {
         Debug.Assert(args.Length == 3);
         try
         {
            if (args.Length > 0)
            {
               string serverUrl = args[1];
               int port = Int32.Parse(args[2]);
               new StudioLauncher(port, serverUrl).Execute();
            }
         }
         catch (Exception e)
         {
            Misc.WriteStackTrace(e, Console.Error);
         }
      }
   }
}
