using System.Windows.Forms;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.remote;
using System.Drawing;
using util.com.magicsoftware.util;
using System.Diagnostics;

namespace com.magicsoftware.richclient.http
{
   /// <summary>
   /// session statistics form + internal logging (InternalLogFile) and output to the activity monitor.
   /// the class' functionality is effective only if DisplayStatisticInformation=Y
   /// </summary>
   internal partial class SessionStatisticsForm : Form
   {
      internal SessionStatisticsForm()
      {
         InitializeComponent();

         if (CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Remote)
         {
            labelServerURL.Text = RemoteCommandsProcessor.GetInstance().ServerUrl;
            labelApplication.Text = ClientManager.Instance.getAppName();
            string prgDescription = ClientManager.Instance.getPrgDescription();
            if (string.IsNullOrEmpty(prgDescription))
               prgDescription = ClientManager.Instance.getPrgName();
            labelProgram.Text = prgDescription;
            labelInternalLogLevel.Text = ClientManager.Instance.getInternalLogLevel();
            labelInternalLogFile.Text = ClientManager.Instance.getInternalLogFile();

            labelRequests.Text = Statistics.GetRequestsCnt().ToString("N0");
            labelUploadedKB.Text = string.Format("{0} KB", Statistics.GetAccumulatedUploadedKB().ToString("N0"));
            labelDownloadedKB.Text = string.Format("{0} KB", Statistics.GetAccumulatedDownloadedKB().ToString("N0"));
            labelUploadedCompressionRatio.Text = string.Format("{0}", Statistics.GetUploadedCompressionRatio().ToString("N0"));
            labelDownloadedCompressionRatio.Text = string.Format("{0}", Statistics.GetDownloadedCompressionRatio().ToString("N0"));
            ulong sessionTime = Statistics.GetSessionTime();
            ulong accumulatedExternalTime = Statistics.GetAccumulatedExternalTime();
            ulong accumulatedMiddlewareTime = Statistics.GetAccumulatedMiddlewareTime();
            ulong accumulatedServerTime = Statistics.GetAccumulatedServerTime();

            SessionTimeLabel.Text = ToHHHMMSSms(sessionTime);
            ExternalTimeLabel.Text = ToHHHMMSSms(accumulatedExternalTime);
            ulong networkTime = accumulatedExternalTime - accumulatedMiddlewareTime - accumulatedServerTime;
            NetworkTimeLabel.Text = ToHHHMMSSms(networkTime);
            MiddlewareTimeLabel.Text = ToHHHMMSSms(accumulatedMiddlewareTime);
            ServerTimeLabel.Text = ToHHHMMSSms(accumulatedServerTime);
            ExternalPercentageLabel.Text = (accumulatedExternalTime * 100 / sessionTime).ToString();
            ulong middlewarePercentage = accumulatedMiddlewareTime * 100 / accumulatedExternalTime;
            ulong serverPercentage = accumulatedServerTime * 100 / accumulatedExternalTime;
            ulong networkPercentage = 100 - middlewarePercentage - serverPercentage;
            NetworkPercentageLabel.Text = networkPercentage.ToString();
            if (networkPercentage >= 50)
               NetworkPercentageLabel.ForeColor = Color.Red;
            MiddlewarePercentageLabel.Text = middlewarePercentage.ToString();
            ServerPercentageLabel.Text = serverPercentage.ToString();
         }
      }

      /// <summary>ms time --> HHH:MM:SS.ms</summary>
      /// <param name="msTime">time in milliseconds</param>
      /// <returns></returns>
      private static string ToHHHMMSSms(ulong msTime)
      {
         return (msTime / 3600000).ToString("D3") + ":" +
                (msTime % 3600000 / 60000).ToString("D2") + ":" +
                (msTime % 60000 / 1000).ToString("D2") + "." +
                (msTime % 1000).ToString("D3");
      }

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void SessionStatistics_KeyUp(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Escape)
            Dispose();
      }

      /// <summary>
      /// </summary>
      /// <param name="msg"></param>
      delegate void SessionStatisticsWriter(string msg);

      /// <param name="writeHeader">if true, the log header (client version, computer, etc...) will be written.</param>
      internal void WriteToInternalLog()
      {
         Debug.Assert(ClientManager.Instance.getDisplayStatisticInfo());

         InternalLogWriter(string.Empty);
         OutputStatistics(InternalLogWriter);
      }

      /// <summary>write requests count, session/network/Middleware/server times to the activity monitor of the server.</summary>
      internal void WriteToFlowMonitor(FlowMonitorQueue flowMonitor)
      {
         OutputStatistics(flowMonitor.addFlowVerifyInfo);
      }

      /// <summary>
      /// output all statistics into a given writer.
      /// </summary>
      /// <param name="writer"></param>
      private void OutputStatistics(SessionStatisticsWriter writer)
      {
         writer("-----------------------------------------------------------------------------");
         writer(string.Format("Server     : {0}", labelServerURL.Text));
         writer(string.Format("Application: {0}", labelApplication.Text));
         writer(string.Format("Program    : {0}", labelProgram.Text));
         writer(string.Format("Log Level  : {0}", labelInternalLogLevel.Text));
         writer("-----------------------------------------------------------------------------");
         writer(string.Format("Requests: {0}", labelRequests.Text));
         writer("-----------------------------------------------------------------------------");
         writer(string.Format("Session   : {0}", SessionTimeLabel.Text));
         writer(string.Format("External  : {0}   {1,3}% out of Session time", ExternalTimeLabel.Text, ExternalPercentageLabel.Text));
         writer(string.Format("Network   : {0}   {1,3}% out of External time", NetworkTimeLabel.Text, NetworkPercentageLabel.Text));
         writer(string.Format("Middleware: {0}   {1,3}% out of External time", MiddlewareTimeLabel.Text, MiddlewarePercentageLabel.Text));
         writer(string.Format("Server    : {0}   {1,3}% out of External time", ServerTimeLabel.Text, ServerPercentageLabel.Text));
         writer("-----------------------------------------------------------------------------");
         writer(string.Format("Uploaded         : {0}", labelUploadedKB.Text));
         writer(string.Format("Compression Ratio: {0}%", labelUploadedCompressionRatio.Text));
         writer(string.Format("Downloaded       : {0}", labelDownloadedKB.Text));
         writer(string.Format("Compression Ratio: {0}%", labelDownloadedCompressionRatio.Text));
         writer("-----------------------------------------------------------------------------");
      }

      /// <summary>
      /// an helper method to write into the client's logger, opening the log if not opened yet.
      /// </summary>
      /// <param name="value"></param>
      private void InternalLogWriter(string msg)
      {
         Logger.Instance.WriteToLog(msg, true);
      }
   }
}
