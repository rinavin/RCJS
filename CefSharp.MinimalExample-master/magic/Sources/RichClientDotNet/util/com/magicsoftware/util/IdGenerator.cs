using System;
using System.Collections.Generic;
using System.Text;

namespace util.com.magicsoftware.util
{
   /// <summary>
   /// Id generator
   /// </summary>
   public class IdGenerator
   {
      int mask;
      int lastId = 0;

      /// <summary>
      /// Creates a generator that does not mask the generated identifier.
      /// </summary>
      public IdGenerator():
         this(0)
      {}

      /// <summary>
      /// Creates a generator that masks the generated identifier using bitwise OR (|) operation.
      /// </summary>
      /// <param name="mask">The value to be ORed with the identifier.</param>
      public IdGenerator(int mask)
      {
         this.mask = mask;
      }

      public int GenerateId()
      {
         return (++lastId | mask);
      }
   }
}
