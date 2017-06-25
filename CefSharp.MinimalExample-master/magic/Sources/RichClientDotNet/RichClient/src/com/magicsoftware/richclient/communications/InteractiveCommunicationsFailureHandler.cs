using System;
using com.magicsoftware.httpclient;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using util.com.magicsoftware.util;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.richclient.communications
{
   /// <summary>
   /// This is the handler for communications that will be used in most cases. This
   /// implementation shows a dialog that asks the user whether to retry the last
   /// communications attempt or not and sets the ShouldRetryLastRequest accordingly.
   /// </summary>
   class InteractiveCommunicationsFailureHandler : ICommunicationsFailureHandler
   {
      public void CommunicationFailed(string url, Exception ex)
      {
         // message box:  caption = HH:MM:SS exception type, message = exception message
         string exceptionCaption = string.Format("{0} {1}",
                                                 DateTimeUtils.ToString(DateTime.Now,
                                                                        XMLConstants.HTTP_ERROR_TIME_FORMAT),
                                                 (ex.InnerException != null
                                                     ? ex.InnerException.GetType()
                                                     : ex.GetType()).Name);
         string exceptionMessage = url.Split('?')[0] + OSEnvironment.EolSeq + OSEnvironment.EolSeq +
                                   (ex.InnerException != null
                                       ? ex.InnerException.Message
                                       : ex.Message) +
                                    OSEnvironment.EolSeq + OSEnvironment.EolSeq;

         ShouldRetryLastRequest = (Commands.messageBox(null, exceptionCaption,
                                       exceptionMessage + "Do you wish to retry connecting?",
                                       Styles.MSGBOX_BUTTON_YES_NO | Styles.MSGBOX_DEFAULT_BUTTON_2) == Styles.MSGBOX_RESULT_YES);
      }

      public bool ShouldRetryLastRequest
      {
         get;
         private set;
      }

      public bool ShowCommunicationErrors
      {
         get { return true; }
      }

   }
}
