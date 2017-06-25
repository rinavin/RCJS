using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.controls.designers
{
   public delegate bool CanParentDelegate(object sender, CanParentArgs allowDragDropArgs);
   /// <summary>
   /// interface for getting information regarding ability to parent control's types
   /// </summary>
   public interface ICanParent
   {
      event CanParentDelegate CanParentEvent;
      bool CanParent(CanParentArgs allowDragDropArgs);
   }
}
