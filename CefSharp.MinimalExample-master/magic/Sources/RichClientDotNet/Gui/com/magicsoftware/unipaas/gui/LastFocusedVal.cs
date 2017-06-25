using System;

namespace com.magicsoftware.unipaas.gui
{
   /// <summary>
   /// The class is used to store  control's edited value,
   /// so it could be restored after current record is replaced by different chunk
   /// </summary>
   public class LastFocusedVal
   {
      public GuiMgControl guiMgControl { get; private set; }
      public int Line { get; private set; }
      public String Val { get; private set; }

      internal LastFocusedVal(GuiMgControl guiMgControl, int line, String val)
      {
         this.guiMgControl = guiMgControl;
         this.Line = line;
         this.Val = val;
      }
   }
}
