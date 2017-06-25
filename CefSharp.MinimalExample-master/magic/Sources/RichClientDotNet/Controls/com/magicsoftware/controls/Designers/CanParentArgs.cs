using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   public enum DragOperationType { Drop, Move, Link }
   public class CanParentArgs : EventArgs
   {
      public Type ChildControlType { get; private set; }
      public DragOperationType DragOperationType { get; private set; }
      public Control ChildControl { get; private set; }

      public CanParentArgs(Type draggedControlType, Control childControl, DragOperationType dragOperationType)
      {
         ChildControlType = draggedControlType;
         DragOperationType = dragOperationType;
         ChildControl = childControl;
      }
   }
}