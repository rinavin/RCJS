using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// A rule that specifies that a property should be refreshed whenever any of dependencies 
   /// have changed.
   /// </summary>
   public class AlwaysRefreshRule : IPropertyRefreshRule
   {
      public bool ShouldRefresh
      {
         get { return true; }
      }
   }
}
