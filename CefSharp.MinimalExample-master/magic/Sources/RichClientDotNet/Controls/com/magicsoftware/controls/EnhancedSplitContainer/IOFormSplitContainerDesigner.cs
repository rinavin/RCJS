using com.magicsoftware.Glyphs;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design.Behavior;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Designer for IOForm.
   /// This show additional glyphs for resizing width/ height of form
   /// </summary>
   public class IOFormSplitContainerDesigner : EnhancedSplitContainerDesigner
   {
      public override GlyphCollection GetGlyphs(GlyphSelectionType selectionType)
      {
         GlyphCollection glyphs = base.GetGlyphs(selectionType);

         if (spc.Orientation == Orientation.Horizontal)
         {
            Point location = base.BehaviorService.MapAdornerWindowPoint(this.spc.Handle, this.spc.DisplayRectangle.Location);
            Rectangle clientAreaRectangle = new Rectangle(location, this.spc.DisplayRectangle.Size);
            int thinkness = 2;
            IOFormSplitContainer iOFormSplitContainer = spc as IOFormSplitContainer;

            if (!iOFormSplitContainer.IsReadOnly) // Show glyphs only when control is not readonly
            {
               // add glyph to change width of the container
               Behavior widthResizeBehavior = new IOFormWidthResizeBehavior(spc as IOFormSplitContainer, base.Component.Site);
               Rectangle WEGlyphRect = new Rectangle(clientAreaRectangle.Left + clientAreaRectangle.Width - thinkness / 2, clientAreaRectangle.Top, thinkness, clientAreaRectangle.Height);
               ResizeGlyph WEGlyph = new ResizeGlyph(WEGlyphRect, Cursors.SizeWE, widthResizeBehavior);
               glyphs.Add(WEGlyph);
            }

            // Add the glyph to change the height of last form when it is visible
            if (spc.Panels.Count > 0 && spc.Panels.Last().Visible)
            {
               PropertyDescriptor descriptor = TypeDescriptor.GetProperties(spc.Panels.Last().Controls[0])["Locked"];
               bool isLocked = descriptor != null? (bool)descriptor.GetValue(spc.Panels.Last().Controls[0]) : false;

               if (!isLocked) // Show glyphs when panel is not locked
               {
                  SplitterResizeBehavior heightResizeBehavior = new SplitterResizeBehavior(iOFormSplitContainer, base.Component.Site, spc.Panels.Last());
                  Rectangle NSGlyphRect = new Rectangle(clientAreaRectangle.Left, clientAreaRectangle.Bottom - thinkness, clientAreaRectangle.Width, thinkness);
                  ResizeGlyph NSGlyph = new ResizeGlyph(NSGlyphRect, Cursors.SizeNS, heightResizeBehavior);
                  glyphs.Add(NSGlyph);
               }
            }
         }

         return glyphs;
      }
   }
}