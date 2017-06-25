using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using Controls.com.magicsoftware;
using com.magicsoftware.support;

namespace RuntimeDesigner
{
   /// <summary>
   /// interface for adjusting properties values when setting/getting the value via the property descriptor
   /// </summary>
   interface ITranslator
   {
      object AdjustedGetValue(object value);
      object AdjustSetValue(object value);
   }

   /// <summary>
   /// translator for the location properties
   /// </summary>
   class LocationTranslator : ITranslator
   {
      Control control;
      Axe xy;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="control"></param>
      /// <param name="xy"></param>
      internal LocationTranslator(Control control, Axe xy)
      {
         this.control = control;
         this.xy = xy;
      }

      #region ITranslator
      public object AdjustedGetValue(object value)
      {
         int shift = xy == Axe.X ? GetDX() : GetDY();
         return (int)value - shift;
      }

      public object AdjustSetValue(object value)
      {
         int shift = xy == Axe.X ? GetDX() : GetDY();
         return (int)value + shift;
      }
      #endregion

      /// <summary>
      /// checks if the value is not below the allowed minimum, considering the scrollbar state
      /// </summary>
      /// <param name="val"></param>
      /// <returns></returns>
      internal int EnsureValueIsLegal(int val)
      {
         ControlDesignerInfo cdi = control.Tag as ControlDesignerInfo;
         int originalPlacement = cdi == null ? 0 : (xy == Axe.X ? cdi.PreviousPlacementBounds.X : cdi.PreviousPlacementBounds.Y);
         int scrollShift = xy == Axe.X ? GetDX() : GetDY();

         if (control.Parent != null && control.Parent.RightToLeft == RightToLeft.Yes && xy == Axe.X)
         {
          //no need to change the location if RTL - controls taken over the right edge will cause a scrollbar, not disappear
         }
         else
         {
            if (val - scrollShift < originalPlacement)
               val = scrollShift + originalPlacement;
         }

         return val;
      }

      /// <summary>
      /// get the horizontal scrollbar shift
      /// </summary>
      /// <returns></returns>
      internal int GetDX()
      {
         ScrollableControl parent = control.Parent as ScrollableControl;
         return (parent != null) ? parent.AutoScrollPosition.X : 0;
      }
      
      /// <summary>
      /// get the vertical scrollbar shift
      /// </summary>
      /// <returns></returns>
      internal int GetDY()
      {
         ScrollableControl parent = control.Parent as ScrollableControl;
         return (parent != null) ? parent.AutoScrollPosition.Y : 0;
      }

   }
}
