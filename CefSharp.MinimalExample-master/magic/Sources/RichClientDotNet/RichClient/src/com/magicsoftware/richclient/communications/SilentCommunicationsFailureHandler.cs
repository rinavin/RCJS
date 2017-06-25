using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.httpclient;

namespace com.magicsoftware.richclient.communications
{
   /// <summary>
   /// An implementation of ICommunicationsFailureHandler that would always return 'false'
   /// for 'ShouldRetryLastRequest' and 'ShowCommunicationErrors'.
   /// </summary>
   class SilentCommunicationsFailureHandler : ICommunicationsFailureHandler
   {
      public void CommunicationFailed(string url, Exception failureException)
      {
         
      }

      public bool ShouldRetryLastRequest
      {
         get { return false; }
      }

      public bool ShowCommunicationErrors
      {
         get { return false; }
      }
   }
}
