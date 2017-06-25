using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// table hit test area
   /// </summary>
   public enum TableHitTestArea
   {
      OnHeader, //click on header
      OnNonClientArea, //click on not client areaa
      OnColumn //click on column area
   }

   /// <summary>
   /// results of the hit test
   /// </summary>
   public class TableHitTestResult
   {
      public TableHitTestArea Area { get; set; }
      public TableColumn TableColumn { get; set; }
   }
}
