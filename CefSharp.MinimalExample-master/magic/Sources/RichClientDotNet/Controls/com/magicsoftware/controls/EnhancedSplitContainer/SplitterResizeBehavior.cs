using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.Glyphs;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// class for table splitter Resize
   /// </summary>
   internal class SplitterResizeBehavior : ResizeBehavior
   {
      Control splitterContrainer;
      Control currentPanel;

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="splitterContrainer"></param>
      /// <param name="seviceProvider"></param>
      /// <param name="currentPanel"></param>
      public SplitterResizeBehavior(Control splitterContrainer, IServiceProvider seviceProvider,Control currentPanel )
         : base(splitterContrainer, seviceProvider)
      {
         this.splitterContrainer = splitterContrainer;
         this.currentPanel = currentPanel;
         Orientation = ((EnhancedSplitContainer)splitterContrainer).Orientation;
      }


     
      /// <summary>
      /// check if new minimum is valid
      /// </summary>
      /// <param name="newSize"></param>
      /// <returns></returns>
      override protected bool CanResize(int newSize)
      {
         return ((EnhancedSplitContainer)splitterContrainer).CanResize(new CanResizeArgs((Panel)currentPanel, newSize, Orientation));
      }

      /// <summary>
      /// Set new value during drag
      /// </summary>
      /// <param name="value"></param>
      override protected void SetNewValueOnDrag(int value)
      {
         
      }

      override protected int GetOrgValue()
      {
         if (Orientation == Orientation.Horizontal)
            return currentPanel.Height;
         else
            return currentPanel.Width;

      }

      override protected PropertyDescriptor GetChangedProp()
      {
         String propertyName = Orientation == Orientation.Horizontal ? "Height" : "Width";
         return TypeDescriptor.GetProperties(currentPanel)[propertyName];
      }

      protected override void SetNewValueOnFinishResize(int value)
      {
         if (Orientation == Orientation.Horizontal)
            currentPanel.Height = value;
         else
            currentPanel.Width = value;
      }

      protected override DesignerTransaction CreateResizeTransaction(IDesignerHost host)
      {
         return host.CreateTransaction("Resizing splitter with panel index " + splitterContrainer.Controls.IndexOf(currentPanel));
      }

      protected override void Invalidate()
      {
         Point location = behaviorService.MapAdornerWindowPoint(this.splitterContrainer.Handle, this.splitterContrainer.DisplayRectangle.Location);
         Rectangle clientAreaRectangle = new Rectangle(location, this.splitterContrainer.DisplayRectangle.Size);
         int padding = 2 * ResizeGlyph.SIZE + ((EnhancedSplitContainer)splitterContrainer).SplitterThinkness;

         if (Orientation == Orientation.Vertical)
         {
            // paint the region - starting from previous position unto new position and add the glyph size 
            int oldX = currentPanel.Right + previousDelta;
            int newX = currentPanel.Right + Delta;
            int x = Math.Min(oldX, newX) - ResizeGlyph.SIZE;
            x = behaviorService.MapAdornerWindowPoint(this.splitterContrainer.Handle, new Point(x, clientAreaRectangle.Top)).X;

            this.behaviorService.Invalidate(new Rectangle(x, clientAreaRectangle.Top, Math.Abs(newX - oldX) + padding, clientAreaRectangle.Height));
         }
         else
         {
            // paint the region - starting from previous position unto new position and add the glyph size 
            int oldY = currentPanel.Bottom + previousDelta;
            int newY = currentPanel.Bottom + Delta;
            int y = Math.Min(oldY, newY) - ResizeGlyph.SIZE;

            y = behaviorService.MapAdornerWindowPoint(this.splitterContrainer.Handle, new Point(clientAreaRectangle.Left, y)).Y;

            this.behaviorService.Invalidate(new Rectangle(clientAreaRectangle.X, y, clientAreaRectangle.Width, Math.Abs(newY - oldY) + padding));
         }
      }
   }
}

