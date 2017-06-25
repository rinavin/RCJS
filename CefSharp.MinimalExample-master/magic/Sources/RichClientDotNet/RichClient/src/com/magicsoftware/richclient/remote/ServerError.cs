using System;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.remote
{
   /// <summary></summary>
   internal class ServerError : ApplicationException
   {
      internal const int INF_NO_RESULT = -11;
      internal const int ERR_CTX_NOT_FOUND = -197;
      internal const int ERR_AUTHENTICATION = -157;
      internal const int ERR_ACCESS_DENIED = -133;
      internal const int ERR_LIMITED_LICENSE_CS = -136;
      internal const int ERR_UNSYNCHRONIZED_METADATA = -271;
      internal const int ERR_CANNOT_EXECUTE_OFFLINE_RC_IN_ONLINE_MODE = -272;
      internal const int ERR_INCOMPATIBLE_RIACLIENT = -275;

      private readonly int _code;

      internal ServerError(String msg)
         : base(msg)
      {
      }

      internal ServerError(String msg, int code)
         : base(msg)
      {
         _code = code;
      }

      internal ServerError(String msg, Exception innerException)
         : base(msg, innerException)
      {
      }

      internal int GetCode()
      {
         return _code;
      }

      ///<summary>
      ///  Return error message for ServerError exception.
      ///  This method will return detailed error message when :-
      ///      1) when detailed message is to be shown (i.e. DisplayGenericError = N in execution.properties) 
      ///      2) when ServerError.Code is 0.
      ///  Otherwise generic error message will be returned.
      ///</summary>
      ///<returns>!!.</returns>
      internal string GetMessage()
      {
         String message = "";

         bool shouldDisplayGenericError = ClientManager.Instance.ShouldDisplayGenericError();
         if (shouldDisplayGenericError && GetCode() != 0)
         {
            String genericErrorMessage = ClientManager.Instance.getMessageString(MsgInterface.STR_GENERIC_ERROR_MESSAGE);
            genericErrorMessage = StrUtil.replaceStringTokens(genericErrorMessage, "%d", 1, "{0}");
            message = String.Format(genericErrorMessage, GetCode());
         }
         else
            message = this.Message;

         return message;
      }
   }
}
