using System;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   public class MgTextBox:TextBox
   {
      // Override IsInputKey method to identify the Special keys
      protected override bool IsInputKey(Keys keyData)
      {
         switch (keyData)
         {
            // Add the list of special keys that you want to handle 
            case Keys.Tab:
               return true;
            default:
               return base.IsInputKey(keyData);
         }
      }
   }
}
