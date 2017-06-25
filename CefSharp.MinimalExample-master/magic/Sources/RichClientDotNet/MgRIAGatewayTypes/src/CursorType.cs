using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// This enum defines cursor types.
   /// </summary>
   public enum CursorType
   {
      Regular = 1,
      Join,
      PartOfJoin,
      MainPartOfJoin,
      PartOfOuter
   }
}
