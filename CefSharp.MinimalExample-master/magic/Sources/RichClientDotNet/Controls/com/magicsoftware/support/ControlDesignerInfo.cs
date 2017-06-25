using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.support
{
   /// <summary>
   /// the information kept for each property
   /// </summary>
   public class DesignerPropertyInfo
   {
      // Should the property be Native Property(real property or virtual property)
      public bool IsNativeProperty { get; set; }

      // Should the property be displayed in the runtime designer property grid
      public bool VisibleInPropertyGrid { get; set; }

      // Is the value the default value of the property
      public bool IsDefaultValue { get; set; }

      // the property's value
      public object Value { get; set; }

      // the property's runtime value
      public object RuntimeValue { get; set; }

      /// <summary>
      /// 
      /// </summary>
      public DesignerPropertyInfo()
      {
         IsNativeProperty = true; 
      }
   }

   /// <summary>
   /// the passed to the runtime designer, for each control
   /// </summary>
   public class ControlDesignerInfo
   {
      public Dictionary<string, DesignerPropertyInfo> Properties { get; set; }

      public int Isn { get; set; }

      // unique Id to identify the control - uses hashcode, not ISN, to ensure uniqueness across subforms
      public int Id { get; set; }

      // unique Id to identify the parent control
      public int ParentId { get; set; }

      // unique Ids to identify the linked controls
      public List<int> LinkedIds { get; set; }

      public MgControlType ControlType { get; set; }

      public bool IsFrame { get; set; }

      public Rectangle PreviousPlacementBounds { get; set; }

      public String FileName { get; set; }

      public int GetPlacementForProp(string propertyName)
      {
         switch (propertyName)
         {
            case Constants.WinPropLeft:
               return PreviousPlacementBounds.X;
            case Constants.WinPropTop:
               return PreviousPlacementBounds.Y;
            case Constants.WinPropWidth:
               return PreviousPlacementBounds.Width;
            case Constants.WinPropHeight:
               return PreviousPlacementBounds.Height;
         }
         return 0;
      }

   }
}
