using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// Defines a single property to determine whether a property's display should be refreshed or not.
   /// </summary>
   public interface IPropertyRefreshRule
   {
      /// <summary>
      /// Gets whether the property, to which the rule is assigned, should be refreshed or not.
      /// </summary>
      bool ShouldRefresh { get; }
   }
}
