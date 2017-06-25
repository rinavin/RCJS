using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.util
{
   /// <summary>
   /// This class is used to identify objects in a recompute table. The objects
   /// have a unique identifier (usually index) within the collection of their own
   /// type, hence the use 'Type' and 'Int' members to construct this identifier.
   /// <para/>A component should use the <see cref="RecomputeIdFactory"/> to construct objects
   /// of this type.
   /// </summary>
   /// <see cref="RecomputeIdFactory"/>
   /// <see cref="RecomputeIdTest"/>
   class RecomputeId
   {
      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      int objectId;
      Type objectType;

      public RecomputeId(Type type, int objectId)
      {
         this.objectType = type;
         this.objectId = objectId;
      }

      public override int GetHashCode()
      {
         int hash = SEED;
         unchecked
         {
            hash = hash * PRIME_NUMBER + objectId.GetHashCode();
            hash = hash * PRIME_NUMBER + objectType.GetHashCode();
         }
         return hash;
      }

      public override bool Equals(object obj)
      {
         RecomputeId other = obj as RecomputeId;
         if (other == null)
            return false;

         return (other.objectType == objectType) && (objectId == other.objectId);
      }

      public override string ToString()
      {
         return "{" + objectType + "," + objectId + "}";
      }
   }
}
