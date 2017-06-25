using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.util
{
   public abstract class ReturnResultBase
   {
      internal abstract bool Success { get; }
      internal abstract string ErrorDescription { get; }

      internal ReturnResultBase InnerResult { get; private set; }

      abstract internal Object GetErrorId();


      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="innerResult"></param>
      public ReturnResultBase(ReturnResultBase innerResult)
      {
         InnerResult = innerResult;
      }

      /// <summary>
      /// CTOR
      /// </summary>
      public ReturnResultBase()
      {

      }

      /// <summary>
      /// return true if this error need to be handled
      /// </summary>
      /// <returns></returns>
      public virtual bool ErroNeedToBeHandled()
      {
         return false;
      }
   }
}
