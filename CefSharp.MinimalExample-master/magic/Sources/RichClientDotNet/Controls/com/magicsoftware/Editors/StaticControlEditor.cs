using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.editors;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// editor implementation for static control
   /// </summary>
   public class StaticControlEditor : Editor
   {

      public StaticControlEditor(Control parentControl)
         : base(parentControl)
      {

      }
      public BoundsComputer BoundsComputer { get; set; } //interface for computing editors bounds
      public override Rectangle Bounds()
      {
         Rectangle rect = new Rectangle();
         return BoundsComputer.computeEditorBounds(rect, false);
      }

      public override bool isHidden()
      {
         return BoundsComputer == null;
      }

      public override void Hide()
      {
         BoundsComputer = null;
         base.Hide();
      }
   }
}
