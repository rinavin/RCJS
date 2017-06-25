using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.ErrorHandling
{
   /// <summary>
   /// 
   /// </summary>
   internal class ErrorHandlingInfo
   {
      public bool Quit { get; set; }
      public VerifyDisplay DisplayType { get; set; }

      /// <summary>
      /// 
      /// </summary>
      internal ErrorHandlingInfo()
      {
         DisplayType = VerifyDisplay.Status;
      }     
   }
}
