using System.Collections.Generic;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.cursor
{
   /// <summary>
   /// runtime cursor data
   /// </summary>
   public class RuntimeCursorData
   {
      public List<RangeData> Ranges { get; set; }
      public FieldValues CurrentValues { get; set; }
      public FieldValues OldValues { get; set; }
      //public LocalTransaction LocalTransaction { get; set; }
   }

}
