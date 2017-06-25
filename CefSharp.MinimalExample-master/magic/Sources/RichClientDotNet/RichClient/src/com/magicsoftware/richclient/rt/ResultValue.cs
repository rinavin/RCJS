using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// This class is introduced to read the result from server for any operation.
   /// </summary>
   class ResultValue : IResultValue
   {

      internal String Value { get; set; }    // the result returned by the server
      internal StorageAttribute Type { get; set; } // the type of the result which is returned by the server


      /// <summary>
      ///   set result value by the Result command
      /// </summary>
      /// <param name = "result">the result computed by the server </param>
      /// <param name = "type">the type of the result which was computed by the server </param>
      public void SetResultValue(String result, StorageAttribute type)
      {
         Value = result;
         Type = type;
      }

   }
}
