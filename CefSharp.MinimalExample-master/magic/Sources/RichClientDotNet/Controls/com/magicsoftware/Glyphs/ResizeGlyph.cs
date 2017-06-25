using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.Design.Behavior;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.Glyphs
{
   /// <summary>
   /// glyph for the resize action
   /// inspired by TableLayoutPanelResizeGlyph
   /// </summary>
   internal class ResizeGlyph : Glyph
   {
      private Rectangle rectangleForGlyph; // rectangle used for displaying glyph
      private Cursor hitTestCursor;
      private Rectangle controlRect ; // rectangle used painting 

      internal ResizeGlyph(Rectangle controlBounds, Cursor hitTestCursor, Behavior behavior)
         : this(controlBounds, controlBounds, hitTestCursor, behavior)
      {
       
      }

      /// <summary>
      /// Ctor which takes rectangle for painting the horizontal line as in some cased control bounds may not be same
      /// </summary>
      /// <param name="rectangleForGlyph"></param>
      /// <param name="controlBounds"></param>
      /// <param name="hitTestCursor"></param>
      /// <param name="behavior"></param>
      internal ResizeGlyph(Rectangle rectangleForGlyph, Rectangle controlBounds, Cursor hitTestCursor, Behavior behavior)
         : base(behavior)
      {
         this.rectangleForGlyph = rectangleForGlyph;
         this.controlRect = controlBounds;
         this.hitTestCursor = hitTestCursor;
      }

      public override Cursor GetHitTest(Point p)
      {
         if (this.rectangleForGlyph.Contains(p) || ((ResizeBehavior)Behavior).IsPushedBehavior)
         {
            return this.hitTestCursor;
         }

         return null;
      }

      public static int SIZE = 3; // indicates the size of glyph to be painted

      public override void Paint(PaintEventArgs pe)
      {
         if (((ResizeBehavior)Behavior).Orientation == Orientation.Horizontal)
         {
            //paint horizontal line
            int y = rectangleForGlyph.Top + ((ResizeBehavior)Behavior).Delta;
            if (((ResizeBehavior)Behavior).IsPushedBehavior)
            {
               using (Pen pen = new Pen(Brushes.DarkGray, SIZE))
               {
                  pe.Graphics.DrawLine(pen,
                  controlRect.X, y, controlRect.X + controlRect.Width, y);
               }
            }
         }
         else
         {
            //paint vertical line
            int x = rectangleForGlyph.Left + ((ResizeBehavior)Behavior).Delta;
            if (((ResizeBehavior)Behavior).IsPushedBehavior)
            {
               using (Pen pen = new Pen(Brushes.DarkGray, SIZE))
               {
                  pe.Graphics.DrawLine(pen,
                  x, controlRect.Y, x, controlRect.Y + controlRect.Height);
               }
            }
         }
      }

      public override Rectangle Bounds
      {
         get
         {
            return this.rectangleForGlyph;
         }
      }
   }

}

