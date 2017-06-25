using System;
using System.Net;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.http
{
   /// <summary>
   /// this class is responsible for recording session statistics.
   /// </summary>
   internal static class Statistics
   {
      private static Object LOCK = new Object(); 
      
      private static readonly long _startTime = Misc.getSystemMilliseconds();
      private static ulong _requestsCnt;                             // requests count.
      private static ulong _accumulatedExternalTime;                 // time (in ms) spent (by the client) waiting for each response to a request that was sent.
      private static ulong _accumulatedMiddlewareTime;               // time (in ms) spent waiting for an available thread or user.
      private static ulong _accumulatedServerTime;                   // time (in ms) spent inside the xpa server (between accepting each request to sending its response).
      private static ulong _accumulatedDownloaded;                   // size (in KB) of data downloaded from the server.
      private static ulong _accumulatedDownloadedAfterDecompression; // in bytes
      private static ulong _accumulatedUploaded;                     // in bytes
      private static ulong _accumulatedUploadedBeforeCompression;    // in bytes

      /// <summary>elapsed session time</summary>
      /// <returns></returns>
      internal static ulong GetSessionTime()
      {
         return (ulong)(Misc.getSystemMilliseconds() - _startTime);
      }

      /// <summary>record a request + its over-all execution time (client -> Web Server -> Middleware/Runtime-engine -> Web Server -> Client)</summary>
      /// <param name="externalTime">time (in ms) spent (by the client) waiting for each response to a request that was sent.</param>
      /// <param name="uploadedKB">size of data uploaded to the server, in KB</param>
      /// <param name="downloadedKB">size of data downloaded from the server, in KB</param>
      internal static void RecordRequest(ulong externalTime, ulong uploadedKB, ulong downloadedKB)
      {
         lock (LOCK)
         {
            _requestsCnt++;
            _accumulatedExternalTime += externalTime;
            _accumulatedUploadedBeforeCompression += uploadedKB;
            _accumulatedDownloadedAfterDecompression += downloadedKB;
         }
      }

      /// <summary>return requests count.</summary>
      internal static ulong GetRequestsCnt()
      {
         lock (LOCK)
         {
            return _requestsCnt;
         }
      }

      /// <summary>time (in ms) spent (by the client) waiting for each response to a request that was sent.</summary>
      internal static ulong GetAccumulatedExternalTime()
      {
         lock (LOCK)
         {
            return _accumulatedExternalTime;
         }
      }

      /// <summary>time (in ms) spent waiting for the middleware to allocate an available thread or user.</summary>
      private static void RecordMiddlewareTime(ulong middlewareTime)
      {
         lock (LOCK)
         {
            _accumulatedMiddlewareTime += middlewareTime;
         }
      }

      internal static ulong GetAccumulatedMiddlewareTime()
      {
         lock (LOCK)
         {
            return _accumulatedMiddlewareTime;
         }
      }

      /// <summary>time (in ms) spent inside the xpa server (between accepting each request to sending its response).</summary>
      private static void RecordServerTime(ulong serverTime)
      {
         lock (LOCK)
         {
            _accumulatedServerTime += serverTime;
         }
      }
      internal static ulong GetAccumulatedServerTime()
      {
         lock (LOCK)
         {
            return _accumulatedServerTime;
         }
      }

      /// <summary>update the size of uploaded data after compression
      /// and the size of downloaded after before decompression</summary>
      /// <param name="sizeUploadedAfterCompression">in bytes</param>
      /// <param name="sizeDownloadedBeforeDecompression">in bytes</param>
      internal static void UpdateUploadDownload(ulong sizeUploadedAfterCompression,
                                         ulong sizeDownloadedBeforeDecompression)
      {
         lock (LOCK)
         {
            _accumulatedUploaded += sizeUploadedAfterCompression;
            _accumulatedDownloaded += sizeDownloadedBeforeDecompression;
         }
      }

      /// <summary>size (in KB) of data uploaded to the server, before compression.</summary>
      private static ulong GetAccumulatedUploadedKBBeforeCompression()
      {
         return _accumulatedUploadedBeforeCompression / 1024;
      }

      /// <summary>size (in KB) of data uploaded to the server.</summary>
      internal static ulong GetAccumulatedUploadedKB()
      {
         return _accumulatedUploaded / 1024;
      }

      /// <summary>size (in KB) of data downloaded from the server.</summary>
      internal static ulong GetAccumulatedDownloadedKB()
      {
         return _accumulatedDownloaded / 1024;
      }

      /// <summary>size (in KB) of data downloaded from the server, after decompression.</summary>
      private static ulong GetAccumulatedDownloadedKBAfterDecompression()
      {
         return _accumulatedDownloadedAfterDecompression / 1024;
      }

      /// <summary>Uploaded data compression ratio.</summary>
      internal static double GetUploadedCompressionRatio()
      {
         double compressionRatio = 0;

         if (GetAccumulatedUploadedKBBeforeCompression() > 0)
            compressionRatio = Math.Round(
               (((GetAccumulatedUploadedKBBeforeCompression() - GetAccumulatedUploadedKB()) * 100) /
                (double)(GetAccumulatedUploadedKBBeforeCompression())));

         return compressionRatio;
      }

      /// <summary>Downloaded data compression ratio.</summary>
      internal static double GetDownloadedCompressionRatio()
      {
         double compressionRatio = 0;

         if (GetAccumulatedDownloadedKBAfterDecompression() > 0)
            compressionRatio = Math.Round(
               (((GetAccumulatedDownloadedKBAfterDecompression() - GetAccumulatedDownloadedKB()) * 100) /
                (double)(GetAccumulatedDownloadedKBAfterDecompression())));

         return compressionRatio;
      }

      /// <summary>Extract and process MgxpaRuntimeExecutionTime and MgMiddlewareWaitTime from HTTP header.</summary>
      /// <param name="response">response received after executing request to server.</param>
      internal static void ProcessExecutionTimeAndMiddlewareWaitTime(HttpWebResponse response)
      {
         // private HTTP header 'MgxpaRuntimeExecutionTime' - execution time inside the runime-engine
         string MgxpaRuntimeExecutionTime = response.Headers.Get("MgxpaRuntimeExecutionTime");
         if (MgxpaRuntimeExecutionTime != null)
         {
            string[] MgxpaRuntimeExecutionTimes = MgxpaRuntimeExecutionTime.Split(",".ToCharArray());

            // pure execution time in the runtime-engine (i.e. excluding network/Middleware times)
            RecordServerTime(ulong.Parse(MgxpaRuntimeExecutionTimes[0]));

            if (MgxpaRuntimeExecutionTimes.Length == 2)
               // time waiting for license checkout request (batch tasks)
               RecordMiddlewareTime(ulong.Parse(MgxpaRuntimeExecutionTimes[1]));
         }

         // private HTTP header 'MgMiddlewareWaitTime' - waiting for the middleware to allocate an available thread or user
         string mgMiddlewareWaitTime = response.Headers.Get("MgMiddlewareWaitTime");
         if (mgMiddlewareWaitTime != null)
         {
            try
            {
               RecordMiddlewareTime(ulong.Parse(mgMiddlewareWaitTime));
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex); // TODO!!
            }
         }
      }

      /// <summary>
      /// Subscribe for Statistics events defined at HttpClientEvents and 
      /// attach appropriate event handlers
      /// </summary>
      /// <returns></returns>
      internal static void RegisterDelegates()
      {
         com.magicsoftware.httpclient.HttpClientEvents.UpdateUpDownDataSizes_Event += Statistics.UpdateUploadDownload;
         com.magicsoftware.httpclient.HttpClientEvents.ProcessInternalHttpHeaders_Event += Statistics.ProcessExecutionTimeAndMiddlewareWaitTime;
      }
   }
}
