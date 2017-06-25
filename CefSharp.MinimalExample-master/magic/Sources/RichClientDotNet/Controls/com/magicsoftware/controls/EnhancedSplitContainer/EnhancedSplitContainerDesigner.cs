using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;
using System.ComponentModel;
using System.Drawing;
using com.magicsoftware.Glyphs;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   public class EnhancedSplitContainerDesigner : ParentControlDesigner
   {
      protected EnhancedSplitContainer spc;
      private CanParentProvider canParentProvider;

      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);
         spc = (EnhancedSplitContainer)component;

         canParentProvider = new CanParentProvider(this);
      }


      /// <summary>
      /// get glyhs of the designer
      /// </summary>
      /// <param name="selectionType"></param>
      /// <returns></returns>
      public override GlyphCollection GetGlyphs(GlyphSelectionType selectionType)
      {
         GlyphCollection glyphs = new GlyphCollection();
              
         // when the split container is not readonly, add the glyphs
         if (!this.spc.IsReadOnly)
         {
            Point location = base.BehaviorService.MapAdornerWindowPoint(this.spc.Handle, this.spc.DisplayRectangle.Location);
            Rectangle clientAreaRectangle = new Rectangle(location, this.spc.DisplayRectangle.Size);
            for (int i = 0; i < spc.Panels.Count - 1; i++)
            {
               Control item = spc.GetChildByIndex(i);
               PropertyDescriptor descriptor = TypeDescriptor.GetProperties(item.Controls[0])["Locked"];
               bool isLocked = (descriptor != null) ? ((bool)descriptor.GetValue(item.Controls[0])) : false;
               if (!isLocked && item.Visible) // show glyphs when form inside panel is not locked
               {
                  Behavior b = new SplitterResizeBehavior(spc, base.Component.Site, item);

                  if (spc.Orientation == Orientation.Horizontal)
                     glyphs.Add(GetVerticalResizeGlyph(clientAreaRectangle, item.Bounds.Bottom, b));
                  else
                     glyphs.Add(GetHorizontalResizeGlyph(clientAreaRectangle, item.Bounds.Right, b));

               }
            }

         }
         return glyphs;
      }

      /// <summary>
      /// add glyph for vertical resize action
      /// </summary>
      /// <param name="glyphs"></param>
      /// <param name="clientAreaRectangle"></param>
      /// <param name="height"></param>
      /// <param name="behavior"></param>
      private ResizeGlyph GetVerticalResizeGlyph(Rectangle clientAreaRectangle, int height, Behavior behavior)
      {
         int thinkness = spc.SplitterThinkness + 2;
         Point controlLocation = new Point(clientAreaRectangle.Left, clientAreaRectangle.Top + height);

         // get the rect of control
         Rectangle controlRect = new Rectangle(clientAreaRectangle.Left, clientAreaRectangle.Top + height, clientAreaRectangle.Width, thinkness);

         // get glyph rect using offset
         Point splitterOffset = spc.GetSplitterOffset();
         Rectangle rectangleForGylph = new Rectangle(controlRect.Left + splitterOffset.X,
                                             controlRect.Top + splitterOffset.Y,
                                             controlRect.Width - splitterOffset.X,
                                             controlRect.Height - splitterOffset.Y); 
        
         ResizeGlyph glyph = new ResizeGlyph(rectangleForGylph, controlRect, Cursors.HSplit, behavior);
         return glyph;

      }


      /// <summary>
      /// add glyph for horizontal resize action
      /// </summary>
      /// <param name="glyphs"></param>
      /// <param name="clientAreaRectangle"></param>
      /// <param name="height"></param>
      /// <param name="behavior"></param>
      private ResizeGlyph GetHorizontalResizeGlyph(Rectangle clientAreaRectangle, int width, Behavior behavior)
      {
         int thinkness = spc.SplitterThinkness + 2;
         Rectangle glyphRectangle = new Rectangle(clientAreaRectangle.Left + width - thinkness/2, clientAreaRectangle.Top, thinkness, clientAreaRectangle.Height);
         ResizeGlyph glyph = new ResizeGlyph(glyphRectangle, Cursors.VSplit, behavior);
         return glyph;

      }


      protected override void OnMouseDragBegin(int x, int y)
      {
         if (canParentProvider.CanDropFromSelectedToolboxItem())
            base.OnMouseDragBegin(x, y);
      }

      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de))
            base.OnDragEnter(de);
      }
   }
}
