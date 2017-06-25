using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.util
{
   /// <summary>
   /// class to be used for the result of operations - failure indicator and a string describing the problem
   /// </summary>
   internal class ReturnResult : ReturnResultBase
   {
      private string errorDescription = null;
      private bool success;

      internal override bool Success { get { return success; } }
      internal override string ErrorDescription { get { return errorDescription; } }
      
      internal String ErrorId { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      internal ReturnResult(string errorDescriptionCode)
      {
         success = false;
         ErrorId = errorDescriptionCode;
         errorDescription = ClientManager.Instance.getMessageString(errorDescriptionCode);
      }

      /// <summary>
      /// CTOR
      /// </summary>
      public ReturnResult()
      {
         success = true;
         ErrorId = "";
      }

      /// <summary>
      /// CTOR with inner result and description
      /// </summary>
      public ReturnResult(String errorDescription, ReturnResultBase innerResult)
         : base(innerResult)
      {
         success = false;
         this.errorDescription = errorDescription;
      }

      /// <summary>
      /// CTOR - use the inner command's success ad description
      /// </summary>
      /// <param name="innerResult"></param>
      public ReturnResult(ReturnResultBase innerResult) : base(innerResult)
      {
         success = innerResult.Success;
         errorDescription = innerResult.ErrorDescription;
      }


      internal static ReturnResult SuccessfulResult = new ReturnResult();

      internal override object GetErrorId()
      {
         return ErrorId;
      }
   }
}
