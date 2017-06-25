using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// Interface for setting Result Value.
   /// </summary>
   internal interface IResultValue
   {
      /// <summary>
      /// set the result value and it's type.
      /// </summary>
      /// <param name="exp"></param>
      void SetResultValue(String result, StorageAttribute type);

   }
}
