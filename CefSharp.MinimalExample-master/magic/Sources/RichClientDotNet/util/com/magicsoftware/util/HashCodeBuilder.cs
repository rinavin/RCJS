using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util
{
   /// <summary>
   /// A class for unifying building hash codes.
   /// </summary>
   /// <example>
   /// int GetHashCode()
   /// {
   ///   var hash = new HashCodeBuilder();
   ///   hash.Append(Property1).Append(Property2).Append(Property3);
   ///   return hash.HashCode;
   /// }
   /// </example>
   public class HashCodeBuilder
   {
      const int DEFAULT_SEED = 17;
      const int DEFAULT_PRIME = 37;

      int prime;
      int hash;

      public HashCodeBuilder():
         this(DEFAULT_SEED, DEFAULT_PRIME)
      {}

      public HashCodeBuilder(int seed, int prime)
      {
         hash = seed;
         this.prime = prime;
      }

      public HashCodeBuilder Append(object value)
      {
         unchecked
         {
            int valueHashCode = 0;
            if (value != null)
               valueHashCode = value.GetHashCode();
            hash = hash * prime + valueHashCode;
         }
         return this;
      }

      public int HashCode
      {
         get { return hash; }
      }

   }
}
