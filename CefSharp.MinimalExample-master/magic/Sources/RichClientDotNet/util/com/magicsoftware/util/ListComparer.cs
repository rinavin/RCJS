using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace com.magicsoftware.util
{
   /// <summary>
   /// Implements IEqualityComparer to compare lists when used as keys in dictionary.<para/>
   /// This comparer assumes that for two lists A and B the following is true:<para/>
   /// (1) A == B --> (A == NULL && B == NULL) || ((A.Count == B.Count) && (For each i in (0 .. Count) A[i] == B[i])<para/>
   /// (2) Hash(A) == Hash(B) --> A == B<para/>
   /// <para/>
   /// Note that these rules imply that if the items order in the lists is different - the lists are considered unequal.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public class ListComparer<T> : IEqualityComparer<T>
         where T : IList
   {

      public bool Equals(T x, T y)
      {
         if (x == null)
            return y == null;

         if (y == null)
            return false;

         if (x.Count != y.Count)
            return false;

         for (int i = 0; i < x.Count; i++)
         {
            if (!Object.Equals(x[i], y[i]))
               return false;
         }

         return true;
      }

      public int GetHashCode(T list)
      {
         var hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(list.Count);
         foreach (var item in list)
         {
            hashBuilder.Append(item);
         }
         return hashBuilder.HashCode;
      }
   }
}
