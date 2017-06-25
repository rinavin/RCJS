using System.Collections.Generic;
using com.magicsoftware.gatewaytypes;
using System.Collections;

namespace com.magicsoftware.richclient.local.data.view
{
   
   /// <summary>
   /// position cache
   /// </summary>
   internal class PositionCache
   {
      /// <summary>
      /// includes first record
      /// </summary>
      internal bool IncludesFirst { get; set; }

      /// <summary>
      /// includes last record
      /// </summary>
      internal bool IncludesLast { get; set; }

      /// <summary>
      /// holds position by client id
      /// </summary>
      Dictionary<PositionId, DbPos> positions = new Dictionary<PositionId, DbPos>();

      internal PositionCache()
      {
      }

      /// <summary>
      /// get position value
      /// </summary>
      /// <param name="positionId"></param>
      /// <param name="position"></param>
      /// <returns></returns>
      internal bool TryGetValue(PositionId positionId, out DbPos position)
      {
         return positions.TryGetValue(positionId, out position);
      }

      /// <summary>
      /// Adds position value
      /// </summary>
      /// <param name="clientId"></param>
      /// <param name="position"></param>
      internal void Set(PositionId positionId, DbPos position)
      {
         positions[positionId] = position.Clone();
      }

      /// <summary>
      /// remove position value
      /// </summary>
      /// <param name="clientId"></param>
      /// <param name="position"></param>
      internal void Remove(PositionId positionId)
      {
         positions.Remove(positionId);
      }

      internal int Count
      {
         get
         {
            return positions.Count;
         }
      }

      /// <summary>
      /// get Idx of Position passed in Position Cache.
      /// </summary>
      /// <param name="value"></param>
      internal int IdxOf(DbPos value)
      {
         int idx = -1;
      
         if (positions.ContainsValue(value))
         {
            int i = 0;
            IEnumerator posEnumerator = positions.Values.GetEnumerator();
            while (posEnumerator.MoveNext())
            {
               DbPos currPos = (DbPos)posEnumerator.Current;

               if (currPos.Equals(value))
                  return i;
               i++;
            }
         }
         return idx;
      }

      /// <summary>
      /// reset the position cache to initial state
      /// </summary>
      internal void Reset()
      {
         positions = new Dictionary<PositionId, DbPos>();
         IncludesFirst = IncludesLast = false;
      }

      internal void UpdateEnd(bool reverse)
      {
         if (reverse)
            IncludesFirst = true;
         else
            IncludesLast = true;
      }

 
      /// <summary>
      /// get the key of a specific position
      /// </summary>
      /// <param name="value"></param>
	  /// <param name="viewId">id of view of the record</param>
      /// <returns></returns>
      internal PositionId GetKeyOfValue(DbPos value, int viewId)
      {
         if (positions.ContainsValue(value))
         {
            foreach (var item in positions)
            {
                if (item.Value.Equals(value) && item.Key.ViewId == viewId)
                  return item.Key;
            }            
         }

         return new PositionId(0);
      }

      /// <summary>
      /// is the position present in the cache?
      /// </summary>
      /// <param name="dbPos"></param>
      /// <returns></returns>
      internal bool ContainsValue(DbPos dbPos)
      {
         return positions.ContainsValue(dbPos);
      }
   }
}
