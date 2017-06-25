using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.cache;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.http
{
   /// <summary>This class encapsulates recent network activities. 
   /// It holds a list of activities the no of elements can be controlled by maxActivities member.</summary>
   internal class RecentNetworkActivities
   {
      // Default max activities
      private const int DEFAULT_RECENT_ACTIVITIES = 5;
      private const int MAX_ACTIVITIES = DEFAULT_RECENT_ACTIVITIES;
      private String _dataUnit;

      // List of recent activities
      private List<RecentNetworkActivity> _recentActivities;
      private String _reqExecTimeLabel;
      private String _timeUnit;
      private String _tooltipHeader;

      /// <summary>get labels that are attached to each value in the tooltip</summary>
      internal void GetTranslateStrings()
      {
         _tooltipHeader = ClientManager.Instance.getMessageString(MsgInterface.STR_RC_RECENT_ACTIVITY_TOOLTIP_HDR);
         _reqExecTimeLabel = ClientManager.Instance.getMessageString(MsgInterface.STR_RC_RECENT_ACTIVITY_TIME_LBL);
         _dataUnit = ClientManager.Instance.getMessageString(MsgInterface.STR_RC_RECENT_ACTIVITY_DATA_UNIT);
         _timeUnit = ClientManager.Instance.getMessageString(MsgInterface.STR_RC_RECENT_ACTIVITY_TIME_UNIT);
      }

      /// <summary> append a new activity in the list to beginning of list. 
      /// If list is not created it will create the list and add the activity. 
      /// If list has reached max count <code>maxActivities</code> then last element is removed.</summary>
      /// <param name="seqID">sequential id of requests.</param>
      /// <param name="requestTime">Calendar containing when the request was executed.</param>
      /// <param name="elapsedTime">Time required for the request to executed</param>
      /// <param name="downloadSizeKB">Number of bytes that were downloaded.</param>
      /// <param name="url">the url that was accessed by the request.</param>
      internal void Append(ulong seqID, TimeSpan requestTime, ulong elapsedTime, ulong downloadSizeKB, string url)
      {
         // if the list not yet created then create it with max count i.e. maxActivities
         if (_recentActivities == null)
         {
            lock (this)
            {
               if (_recentActivities == null)
                  _recentActivities = new List<RecentNetworkActivity>(MAX_ACTIVITIES);
            }
         }

         lock (_recentActivities)
         {
            // if list has reached to it max limit then remove the last element
            if (_recentActivities.Count == MAX_ACTIVITIES)
            {
               _recentActivities.RemoveAt(MAX_ACTIVITIES - 1);
            }

            // Create new recent activity object
            var recentActivity = new RecentNetworkActivity(this, seqID, requestTime, elapsedTime, downloadSizeKB, url);

            // add to beginning of list.
            _recentActivities.Insert(0, recentActivity);
         }
      }

      /// <summary> Returns string that will be displayed as tool tip. The string contains header and information about all
      /// activities in the list. e.g. http://myserver.com Recent Network Activities At 10:02:24 800 ms. 27 KB At
      /// 10:02:10 3450 ms. 1220 KB At 10:01:10 450 ms 12 KB At 10:01:01 2300 ms 1000 KB At 10:00:01 234 ms 10 KB
      /// </summary>
      /// <returns>String containing tool tip.</returns>
      internal String ToTooltipString()
      {
         var strBuff = new StringBuilder();
         if (_recentActivities != null)
         {
            lock (_recentActivities)
            {
               // Add 2 header lines.
               strBuff.Append(GetToolTipHeader());
               strBuff.Append("\n");

               // get iterator for the list
               IEnumerator it = _recentActivities.GetEnumerator();

               // Loop in the list
               while (it.MoveNext())
               {
                  strBuff.Append("\n");
                  // Get the next element in the lost
                  var recentActivity = (RecentNetworkActivity) it.Current;

                  // add recent activity's string.
                  Debug.Assert(recentActivity != null, "recentActivity != null");
                  strBuff.Append(recentActivity.ToTooltipString());
               }
            }
         }

         return strBuff.ToString();
      }

      /// <returns>Returns tool tip header</returns>
      private String GetToolTipHeader()
      {
         var strBuff = new StringBuilder();
         String serverUrl = RemoteCommandsProcessor.GetInstance().ServerUrl;

         if (!String.IsNullOrEmpty(serverUrl))
         {
            // Get server URL
            try
            {
               Uri url = Misc.createURI(RemoteCommandsProcessor.GetInstance().ServerUrl);
               // Prepare first header line http://servername.com
               strBuff.Append(url.Scheme + "://" + url.Host);
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex.Message);
            }
         }

         // Build tooltip header.
         strBuff.Append("\n" + _tooltipHeader);

         return strBuff.ToString();
      }

      #region Nested type: RecentNetworkActivity

      /// <summary>This class holds information about one network activity. 
      /// Typically it holds the time when the request was executed the time took to execute the request 
      /// and data downloaded on client side.</summary>
      /// <author> rajendrap</author>
      private class RecentNetworkActivity
      {
         private readonly ulong _downloadSizeKb;
         private readonly ulong _elapsedTime;
         private readonly RecentNetworkActivities _enclosingInstance;
         private readonly ulong _seqId; // sequential id of the network activity
         private readonly String _url; // Requested URL
         private TimeSpan _requestTime;

         /// <summary> CTOR Initializes the data members.</summary>
         /// <param name="enclosingInstance">reference of container object</param>
         /// <param name="seqID">sequential id of requests.</param>
         /// <param name="requestTime">Calendar containing when the request was executed.</param>
         /// <param name="elapsedTime">Time taken to execute the request.</param>
         /// <param name="downloadSizeKB">Data downloaded on client side.</param>
         /// <param name="url">the url that was accessed by the request.</param>
         internal RecentNetworkActivity(RecentNetworkActivities enclosingInstance, ulong seqID, TimeSpan requestTime,
                                        ulong elapsedTime, ulong downloadSizeKB, string url)
         {
            _enclosingInstance = enclosingInstance;
            _seqId = seqID;
            _requestTime = requestTime;
            _elapsedTime = elapsedTime;
            _downloadSizeKb = downloadSizeKB;
            _url = url;
         }

         /// <summary> String representation of network activity used for showing in tooltip. The string will be built in
         /// form of At 'Request executed time in HH:MM:SS' 'Time taken in ms' 'data downloaded in KB' e.g. At
         /// 10:01:34 800ms 27KB</summary>
         /// <returns>String containing tooltip string</returns>
         internal String ToTooltipString()
         {
            const int MIN_COLUMN_WIDTH = 5;
            const int MAX_URL_SIZE = 80;

            var strBuff = new StringBuilder();

            // Build the string
            strBuff.Append("#");
            strBuff.Append(_seqId);
            strBuff.Append(AppendSpaces(MIN_COLUMN_WIDTH - Convert.ToString(_seqId).Length));
            strBuff.Append(" : ");
            strBuff.Append(_enclosingInstance._reqExecTimeLabel + " ");
            strBuff.Append(GetFormattedRequestExecutedTime());
            strBuff.Append("   ");
            strBuff.Append(AppendSpaces(MIN_COLUMN_WIDTH - Convert.ToString(_elapsedTime).Length));
            strBuff.Append(Convert.ToString(_elapsedTime));
            strBuff.Append(" " + _enclosingInstance._timeUnit + "   ");
            strBuff.Append(AppendSpaces(MIN_COLUMN_WIDTH - Convert.ToString(_downloadSizeKb/1024).Length));
            strBuff.Append(Convert.ToString(_downloadSizeKb/1024));
            strBuff.Append(" " + _enclosingInstance._dataUnit + "   ");
            strBuff.Append(AppendSpaces(MIN_COLUMN_WIDTH - _url.Length));
            strBuff.Append(_url.Length <= MAX_URL_SIZE
                              ? _url
                              : _url.Substring(0, MAX_URL_SIZE));

            return strBuff.ToString();
         }

         /// <summary> returns a string with double spaces.</summary>
         /// <param name="space">: no of spaces to required in the string</param>
         private static String AppendSpaces(int space)
         {
            String emptyStr = new StringBuilder().ToString();
            for (int index = space; index > 0; index--)
               emptyStr += "  ";

            return emptyStr;
         }

         /// <summary> returns the requestExecutedTime as a formatted string</summary>
         /// <returns></returns>
         private String GetFormattedRequestExecutedTime()
         {
            var timeBuf = new StringBuilder();

            // separate HH, MM and SS from request executed time
            int currHH = _requestTime.Hours;
            int currMM = _requestTime.Minutes;
            int currSS = _requestTime.Seconds;

            if (currHH < 10)
               timeBuf.Append("0");
            timeBuf.Append(currHH);

            timeBuf.Append(":");

            if (currMM < 10)
            {
               timeBuf.Append("0");
            }
            timeBuf.Append(currMM);

            timeBuf.Append(":");

            if (currSS < 10)
            {
               timeBuf.Append("0");
            }
            timeBuf.Append(currSS);

            return timeBuf.ToString();
         }
      }

      #endregion
   }
}