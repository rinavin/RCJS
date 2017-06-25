using System;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Interface to be used for choice controls.
   /// </summary>   
   public interface IChoiceControl
   {
      event EventHandler SelectedIndexChanged;
      int SelectedIndex { get; set; }
   }
}
