using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{
   internal enum UnitComputeErrorCode { NoError = 0, LocateFailed, RangeFailed };
   
   /// <summary>
   /// result of computing unit
   /// </summary>
   internal class UnitComputeResult : ReturnResultBase
   {
      internal override bool Success { get { return ErrorCode == UnitComputeErrorCode.NoError; } }
      internal UnitComputeErrorCode ErrorCode { get; set; }

      internal override string ErrorDescription
      {
         get { return ErrorCode.ToString(); }
      }

      internal override object GetErrorId()
      {
         return ErrorCode;
      }
   }
}
