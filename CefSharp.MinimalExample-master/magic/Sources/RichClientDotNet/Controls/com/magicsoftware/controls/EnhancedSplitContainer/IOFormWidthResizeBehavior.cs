using com.magicsoftware.Glyphs;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Behavior for resizing width/ height of IO Form
   /// </summary>
   internal class IOFormWidthResizeBehavior : ResizeBehavior
   {
      private IOFormSplitContainer splitterContrainer;

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="splitterContrainer"></param>
      /// <param name="seviceProvider"></param>
      /// <param name="currentPanel"></param>
      public IOFormWidthResizeBehavior(IOFormSplitContainer splitterContrainer, IServiceProvider seviceProvider)
         : base(splitterContrainer, seviceProvider)
      {
         this.splitterContrainer = splitterContrainer;
         Orientation = Orientation.Vertical; // resize Width
      }

      /// <summary>
      /// check if new minimum is valid
      /// </summary>
      /// <param name="newSize"></param>
      /// <returns></returns>
      override protected bool CanResize(int newSize)
      {
         return ((EnhancedSplitContainer)splitterContrainer).CanResize(new CanResizeArgs(splitterContrainer, newSize, Orientation.Vertical));
      }

      /// <summary>
      /// Set new value during drag
      /// </summary>
      /// <param name="value"></param>
      override protected void SetNewValueOnDrag(int value)
      {
      }

      /// <summary>
      /// get  the current value of splitter
      /// </summary>
      /// <returns></returns>
      override protected int GetOrgValue()
      {
         return splitterContrainer.Width; // return the current width
      }

      /// <summary>
      /// changed property is width
      /// </summary>
      /// <returns></returns>
      override protected PropertyDescriptor GetChangedProp()
      {
         return TypeDescriptor.GetProperties(splitterContrainer)["Width"];
      }

      /// <summary>
      /// update the value when resize is completed
      /// </summary>
      /// <param name="value"></param>
      protected override void SetNewValueOnFinishResize(int value)
      {
         splitterContrainer.UpdateWidth(value);
      }

      /// <summary>
      /// invalidate the required region
      /// </summary>
      protected override void Invalidate()
      {
         Point location = behaviorService.MapAdornerWindowPoint(this.splitterContrainer.Handle, this.splitterContrainer.DisplayRectangle.Location);
         Rectangle clientAreaRectangle = new Rectangle(location, this.splitterContrainer.DisplayRectangle.Size);
         int padding = 2 * ResizeGlyph.SIZE + ((EnhancedSplitContainer)splitterContrainer).SplitterThinkness;

         // paint the region - starting from previous position unto new position and add the glyph size 
         int oldx = clientAreaRectangle.Right + previousDelta;
         int newX = clientAreaRectangle.Right + Delta;
         int x = Math.Min(oldx, newX) - ResizeGlyph.SIZE;

         this.behaviorService.Invalidate(new Rectangle(x, clientAreaRectangle.Top, Math.Abs(newX - oldx) + padding, clientAreaRectangle.Height));

      }
   }
}