using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using com.magicsoftware.httpclient.utils;

namespace com.magicsoftware.httpclient
{
   /// <summary>
   /// This class support functionality of HTTP Client assembly without exposing to it higher level classes (RC Client).
   /// It contains the definitions of delegates and events that raised by HttpClient and handled
   /// at RC Client assembly.
   /// </summary>
   public static class HttpClientEvents
   {
      #region HttpManager events

      /// <summary></summary>
      /// <returns>communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip), in ms.</returns>
      internal static uint GetHttpCommunicationTimeout()
      {
         return (GetHttpCommunicationTimeout_Event != null
                     ? GetHttpCommunicationTimeout_Event()
                     : 5000);
      }
      public delegate uint GetHttpCommunicationTimeout_Delegate();
      public static event GetHttpCommunicationTimeout_Delegate GetHttpCommunicationTimeout_Event;

      /// <summary>
      /// update the size of uploaded data after compression
      /// and the size of downloaded data before decompression
      /// </summary>
      /// <param name="sizeUploadedAfterCompression">in bytes</param>
      /// <param name="sizeDownloadedBeforeDecompression">in bytes</param>
      /// <returns></returns>
      internal static void UpdateUpDownDataSizes(ulong sizeUploadedAfterCompression,
                                                ulong sizeDownloadedBeforeDecompression)
      {
         if (UpdateUpDownDataSizes_Event != null)
            UpdateUpDownDataSizes_Event(sizeUploadedAfterCompression, sizeDownloadedBeforeDecompression);
      }
      public delegate void UpdateUpDownDataSizes_Delegate(ulong sizeUploadedAfterCompression,
                                                          ulong sizeDownloadedBeforeDecompression);
      public static event UpdateUpDownDataSizes_Delegate UpdateUpDownDataSizes_Event;

      /// <summary>
      /// Process internal http headers from HTTP response.
      /// </summary>
      /// <param name="response">response received after executing request to server</param>
      internal static void ProcessInternalHttpHeaders(HttpWebResponse response)
      {
         if (ProcessInternalHttpHeaders_Event != null)
            ProcessInternalHttpHeaders_Event(response);
      }

      public delegate void ProcessInternalHttpHeaders_Delegate(HttpWebResponse response);
      public static event ProcessInternalHttpHeaders_Delegate ProcessInternalHttpHeaders_Event;

      #endregion //HttpManager events
      
      
      #region ClientManager events


      /// <summary>
      /// Retrieves execution property value by it's name
      /// </summary>
      /// <param name="propertyName"></param>
      /// <returns>property value as string</returns>
      internal static string GetExecutionProperty(string propertyName)
      {
         return (GetExecutionProperty_Event != null
            ? GetExecutionProperty_Event(propertyName)
            : null);
      }

      public delegate string GetExecutionProperty_Delegate(string propertyName);
      public static event GetExecutionProperty_Delegate GetExecutionProperty_Event;



      /// <summary>
      /// Retrieves global unique ID of the session
      /// </summary>
      /// <returns>'machine unique ID' + 'process ID'</returns>
      internal static string GetGlobalUniqueSessionID()
      {
         return (GetGlobalUniqueSessionID_Event != null
                  ? GetGlobalUniqueSessionID_Event()
                  : null);
      }

      public delegate string GetGlobalUniqueSessionID_Delegate();
      public static event GetGlobalUniqueSessionID_Delegate GetGlobalUniqueSessionID_Event;

      /// <summary>
      /// If a server error occurred, display a generic error message and instead of the message from the server.
      /// True by default.
      /// </summary>
      /// <returns></returns>
      internal static bool ShouldDisplayGenericError()
      {
         return (ShouldDisplayGenericError_Event != null
                  ? ShouldDisplayGenericError_Event()
                  : true);
      }

      public delegate bool ShouldDisplayGenericError_Delegate();
      public static event ShouldDisplayGenericError_Delegate ShouldDisplayGenericError_Event;


      /// <summary>
      /// Retrieves message text according to message ID and language settings
      /// </summary>
      /// <param name="msgId"></param>
      /// <returns></returns>
      internal static string GetMessageString(string msgId)
      {
         return (GetMessageString_Event != null
                  ? GetMessageString_Event(msgId)
                  : null);
      }

      public delegate string GetMessageString_Delegate(string msgId);
      public static event GetMessageString_Delegate GetMessageString_Event;



      /// <summary>
      /// Retrieves runtime context ID
      /// </summary>
      /// <returns></returns>
      internal static string GetRuntimeCtxID()
      {
         return (GetRuntimeCtxID_Event != null
                   ? GetRuntimeCtxID_Event()
                   : "");
      }

      public delegate string GetRuntimeCtxID_Delegate();
      public static event GetRuntimeCtxID_Delegate GetRuntimeCtxID_Event;


      /// <summary>
      /// Used to determine is Client received the first HTTP request (ctxID equals to RC_NO_CONTEXT_ID)
      /// </summary>
      /// <returns></returns>
      internal static bool IsFirstRequest()
      {

         return (IsFirstRequest_Event != null
                  ? IsFirstRequest_Event()
                  : false);
      }

      public delegate bool IsFirstRequest_Delegate();
      public static event IsFirstRequest_Delegate IsFirstRequest_Event;

      #endregion //ClientManager events


      #region RemoteCommandProcessor events

      /// <summary>
      /// Retrieves session counter value
      /// </summary>
      /// <returns></returns>
      internal static long GetSessionCounter()
      {
         return (GetSessionCounter_Event != null
                  ? GetSessionCounter_Event()
                  : 0);
      }

      public delegate long GetSessionCounter_Delegate();
      public static event GetSessionCounter_Delegate GetSessionCounter_Event;



      /// <summary>
      /// Used by HTTPClient for updating session counter 
      /// </summary>
      /// <param name="value"></param>
      /// <returns></returns>
      internal static void CheckAndSetSessionCounter(long value)
      {
         if (CheckAndSetSessionCounter_Event != null)
            CheckAndSetSessionCounter_Event(value);
      }

      public delegate void CheckAndSetSessionCounter_Delegate(long value);
      public static event CheckAndSetSessionCounter_Delegate CheckAndSetSessionCounter_Event;

      #endregion //RemoteCommandProcessor events
   }
}
