using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// interface to be used by toolbox items which may create different types of controls,
   /// depending on whether the target type is a table or not
   /// </summary>
   public interface ICanParentArgsProvider
   {
      CanParentArgs GetCanParentArgs(IDesignerHost host, bool forTable, DragOperationType dragOperationType);
   }
}
