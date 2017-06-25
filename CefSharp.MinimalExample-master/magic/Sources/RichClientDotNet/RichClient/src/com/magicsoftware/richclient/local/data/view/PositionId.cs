using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// id of position for position cache
   /// </summary>
   internal class PositionId
   {
      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      internal int ClientRecordId { get; private set; }
      internal int ViewId { get; private set; }

      public PositionId(int clientRecordId, int viewId)
      {
         ClientRecordId = clientRecordId;
         ViewId = viewId;
      }

      public PositionId(int clientRecordId)
         : this(clientRecordId, RuntimeViewBase.MAIN_VIEW_ID)
      {
      }
      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         int hash = SEED;

         hash = PRIME_NUMBER * hash + ClientRecordId.GetHashCode();
         hash = PRIME_NUMBER * hash + ViewId.GetHashCode();

         return hash;
      }

      public override bool Equals(object obj)
      {
         if (obj != null && obj is PositionId)
            return (this.GetHashCode() == obj.GetHashCode());
         else
            return false;
      }
   }

}
