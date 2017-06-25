using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// strategy for the runtime designer property descriptor CanReset method, for the height property on ListBox
   /// </summary>
   class CanResetListBoxHeightStrategy : ICanResetStrategy
   {
      MgListBox listBox;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="listBox"></param>
      public CanResetListBoxHeightStrategy(MgListBox listBox)
      {
         this.listBox = listBox;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public bool CanResetData(object defaultValue, object value)
      {
         //adjust the default height according to the items height, and compare it to the current value
         int bordersHeight = listBox.Height % listBox.ItemHeight;                       // height of area out of the items
         int itemsCount = ((int)defaultValue - bordersHeight) / listBox.ItemHeight;     // number of items that fit in the default height
         int realHeightFromDefaultValue = itemsCount * listBox.ItemHeight + bordersHeight;

         return realHeightFromDefaultValue != (int)value;
      }
   }
}
