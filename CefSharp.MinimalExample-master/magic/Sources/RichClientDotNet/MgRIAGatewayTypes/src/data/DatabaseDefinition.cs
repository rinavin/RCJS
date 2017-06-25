using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.gatewaytypes.data
{
   /// <summary>
   /// class holds the data used to define a database
   /// </summary>
   public class DatabaseDefinition : ICloneable
   {
      public string Name { get; set; }
      public string Location { get; set; }
      public int DatabaseType { get; set; }
      public string UserPassword { get; set; }

      public object Clone()
      {
         DatabaseDefinition dbDefinition = (DatabaseDefinition)this.MemberwiseClone();
         return dbDefinition;
      }
   }
}
