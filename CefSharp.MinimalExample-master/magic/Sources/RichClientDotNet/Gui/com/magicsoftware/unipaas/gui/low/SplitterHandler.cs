using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using com.magicsoftware.unipaas.gui.low;
#if PocketPC
using SplitterEventArgs = com.magicsoftware.mobilestubs.SplitterEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   internal class SplitterHandler : HandlerBase
   {
      private static SplitterHandler _instance;
      internal static SplitterHandler getInstance()
      {
         if (_instance == null)
            _instance = new SplitterHandler();
         return _instance;
      }

      /// <summary> </summary>
      private SplitterHandler()
      {
      }

      /// <summary> 
      /// adds events for mgSplitContainer
      /// </summary>
      /// <param name="splitter"></param>
      internal void addHandler(Splitter splitter)
      {
         splitter.Paint += PaintHandler;
         splitter.Resize += ResizeHandler;
#if !PocketPC
         splitter.SplitterMoved += SplitterMovedHandler;
         splitter.SplitterMoving += SplitterMovingHandler;
#endif
         //SplitterMoving 
      }

      /// <summary> handle the event of the MgSplitContainer</summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         Splitter splitter = (Splitter)sender;
         DefaultHandler defaultHandler = DefaultHandler.getInstance();

         switch (type)
         {
            case EventType.PAINT:
               onPaint(splitter, (PaintEventArgs)e);
               break;

            case EventType.SPLITTER_MOVING:
               onSplitterMoving(splitter, (SplitterEventArgs)e);
               break;

            case EventType.SPLITTER_MOVED:
               onSplitterMoved(splitter, (SplitterEventArgs)e);
               break;

            case EventType.RESIZE:
               ((MgSplitContainer)splitter.Parent).onResize();
               break;
         }
      }

      ///// <summary> handle the paint of the splitter
      ///// 
      ///// </summary>
      ///// <param name="event">
      ///// </param>
      internal void onPaint(Splitter splitter, PaintEventArgs e)
      {
         if (splitter.Parent is MgSplitContainer)
         {
            MgSplitContainer mgSahsForm = (MgSplitContainer)splitter.Parent;

            Graphics G = e.Graphics;
            Pen pen = new Pen(Color.Black, 1);
            Rectangle rect = splitter.Bounds;
            System.Console.WriteLine(rect);
            if (mgSahsForm.getOrientation() == MgSplitContainer.SPLITTER_STYLE_VERTICAL)
            {
               pen.Color = SystemColors.ControlLightLight;
               G.DrawLine(pen, 0, 0, rect.Width - 2, 0);
               G.DrawLine(pen, 0, 1, rect.Width - 2, 1);

               pen.Color = SystemColors.ControlDark;
               G.DrawLine(pen, 0, 2, rect.Width - 2, 2);
               pen.Color = Color.Black;
               G.DrawLine(pen, 0, 3, rect.Width - 3, 3);
            }
            else if (mgSahsForm.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
            {
               pen.Color = SystemColors.ControlLightLight;
               G.DrawLine(pen, 0, 0, 0, rect.Height - 2);
               G.DrawLine(pen, 1, 0, 1, rect.Height - 2);

               pen.Color = SystemColors.ControlDark;
               G.DrawLine(pen, 2, 0, 2, rect.Height - 2);
               pen.Color = Color.Black;
               G.DrawLine(pen, 3, 0, 3, rect.Height - 3);
            }
            pen.Dispose();
         }
      }

      /// <summary>
      /// perform layout after splitter was moved
      /// </summary>
      /// <param name="splitter"></param>
      /// <param name="e"></param>
      internal void onSplitterMoved(Splitter splitter, SplitterEventArgs e)
      {
         MgSplitContainer mgSplitContainer = (MgSplitContainer)splitter.Parent;
         mgSplitContainer.IgnoreLayout = false;
         mgSplitContainer.PerformLayout();
      }    

      /// <summary> update the or size according to the send parameters and orientation
      /// 
      /// </summary>
      /// <param name="controlBefore">
      /// </param>
      /// <param name="newBeforeControlBounds">
      /// </param>
      /// <param name="controlAfter">
      /// </param>
      /// <param name="newAfterControlBounds">
      /// </param>
      private void setOrgSize(MgSplitContainer mgSplitContainer, Control controlBefore, Rectangle newBeforeControlBounds, Control controlAfter, Rectangle newAfterControlBounds)
      {
         // get the layout, is not exist then create the layout for the before\after control
         MgSplitContainerData layoutDataBeforeControl = GuiUtils.getMgSplitContainerData(controlBefore);
         MgSplitContainerData layoutDataAfterControl = GuiUtils.getMgSplitContainerData(controlAfter);

         if (mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
         {
            layoutDataBeforeControl.orgWidth = newBeforeControlBounds.Width;
            layoutDataAfterControl.orgWidth = newAfterControlBounds.Width;
         }
         else
         {
            layoutDataBeforeControl.orgHeight = newBeforeControlBounds.Height;
            layoutDataAfterControl.orgHeight = newAfterControlBounds.Height;
         }         
      }


      /// <summary> handle the drag of the splitter
      /// and update the org size
      /// </summary>
      /// <param name="event">
      /// </param>      
      internal void onSplitterMoving(Splitter splitter, SplitterEventArgs e)
      {
         MgSplitContainer mgSplitContainer = (MgSplitContainer)splitter.Parent;
         mgSplitContainer.IgnoreLayout = true;

         int shift = 0;
         int beforeControlminimumSize = 0;
         int afterControlminimumSize = 0;

         // Rectangle splitterBounds - get the bound of the selected splitter- the new bound after the resize
         Rectangle boundsSelectedSplitter = splitter.Bounds;
         // check only when there is movement in the Splitter place
         bool isHorizontal = mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL;
         if ((isHorizontal && e.SplitX == boundsSelectedSplitter.X) || (!isHorizontal && e.SplitY == boundsSelectedSplitter.Y))
         {
            mgSplitContainer.IgnoreLayout = false;
            return;
         }

         int SplitterIndex = -1;

         // found the Splitter that was selected in the array Splitter
         for (int i = 0; i < mgSplitContainer.Splitters.Count; i++)
         {
            if (mgSplitContainer.Splitters[i] == splitter)
            {
               SplitterIndex = i;
               break;
            }
         }
         // if not found return
         if (SplitterIndex == -1)
         {
            mgSplitContainer.IgnoreLayout = false;
            return;
         }
         Control[] controls = mgSplitContainer.getControls(true);

         // fixed bug#:769214
         if (controls.Length <= SplitterIndex + 1)
         {
            mgSplitContainer.IgnoreLayout = false;
            return;
         }

         // get the control BEFORE the selected Splitter from the control array
         Control controlBefore = controls[SplitterIndex];
         // get the control AFTER the selected Splitter from the control array
         Control controlAfter = controls[SplitterIndex + 1];

         // get the minimum size of the before\After control
         MinSizeInfo beforeControlMinSizeInfo = GuiUtils.getMinSizeInfo(controlBefore);
         MinSizeInfo afterControlMinSizeInfo = GuiUtils.getMinSizeInfo(controlAfter);
         Point minSize;
         if (beforeControlMinSizeInfo != null)
         {
            minSize = beforeControlMinSizeInfo.getMinSize(false);
            beforeControlminimumSize = isHorizontal ? minSize.X : minSize.Y;
         }
         if (afterControlMinSizeInfo != null)
         {
            minSize = afterControlMinSizeInfo.getMinSize(false);
            afterControlminimumSize = isHorizontal ? minSize.X : minSize.Y;
         }

         // get the bounds of the two controls
         Rectangle beforeControlBounds = new Rectangle(controlBefore.Bounds.Location.X, 
                                                       controlBefore.Bounds.Location.Y, 
                                                       controlBefore.Bounds.Size.Width, 
                                                       controlBefore.Bounds.Size.Height);
         Rectangle afterControlBounds = new Rectangle(controlAfter.Bounds.Location.X, 
                                                      controlAfter.Bounds.Location.Y, 
                                                      controlAfter.Bounds.Size.Width, 
                                                      controlAfter.Bounds.Size.Height);

         ////do the movement          
         if (isHorizontal)
         {
            // found the allow shift until the minimum size
            shift = getShift(mgSplitContainer, e, boundsSelectedSplitter, beforeControlBounds, beforeControlminimumSize, afterControlBounds, afterControlminimumSize);
            if (shift == 0)
               return;
            else
            {
               // update the bounds of the Splitter control by the shift value
               beforeControlBounds.Width += shift;
               afterControlBounds.X += shift;
               afterControlBounds.Width -= shift;

               e.SplitX = beforeControlBounds.Width;

               setOrgSize(mgSplitContainer, controlBefore, beforeControlBounds, controlAfter, afterControlBounds);
            }
         }
         else
         {
            // found the allow shift until the minimum size
            shift = getShift(mgSplitContainer, e, boundsSelectedSplitter, beforeControlBounds, beforeControlminimumSize, afterControlBounds, afterControlminimumSize);
            if (shift == 0)
               return;
            else
            {
               // update the bounds of the Splitter control by the shift value
               beforeControlBounds.Height += shift;
               afterControlBounds.Y += shift;
               afterControlBounds.Height -= shift;

               setOrgSize(mgSplitContainer, controlBefore, beforeControlBounds, controlAfter, afterControlBounds);
            }
         }
      }

      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="MgSplitContainer"></param>
      /// <param name="e"></param>
      /// <param name="selectedSplitterBounds"></param>
      /// <param name="beforeControlBounds"></param>
      /// <param name="beforeControlMinimumSize"></param>
      /// <param name="afterControlBounds"></param>
      /// <param name="afterControlMinimumSize"></param>
      /// <returns></returns>
      private int getShift(MgSplitContainer mgSplitContainer, SplitterEventArgs e, Rectangle selectedSplitterBounds, Rectangle beforeControlBounds, int beforeControlMinimumSize, Rectangle afterControlBounds, int afterControlMinimumSize)
      {
         int shift = 0;

         if (mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
         {
            shift = e.SplitX - selectedSplitterBounds.X;
            if (shift < 0)
            // we need to check the allow of the before control
            {
               if (beforeControlBounds.Width > beforeControlMinimumSize)
               {
                  if (beforeControlBounds.Width + shift < beforeControlMinimumSize)
                  {
                     shift = beforeControlMinimumSize - beforeControlBounds.Width;
                     e.SplitX = selectedSplitterBounds.X + shift;
                  }
               }
               else
               {
                  shift = 0;
                  e.SplitX = selectedSplitterBounds.X;
               }
            }
            // if (shift > 0)//we need to check the allow of the after control
            else
            {
               if (afterControlBounds.Width > afterControlMinimumSize)
               {
                  if (afterControlBounds.Width + (-shift) < afterControlMinimumSize)
                  {
                     shift = afterControlBounds.Width - afterControlMinimumSize;
                     e.SplitX = selectedSplitterBounds.X - (shift);
                  }
               }
               else
               {
                  shift = 0;
                  e.SplitX = selectedSplitterBounds.X;
               }
            }
         }
         // if (getOrientation() == SWT.VERTICAL)
         else
         {
            shift = e.SplitY - selectedSplitterBounds.Y;
            if (shift < 0)
            //// we need to check the allow of the before control
            {
               if (beforeControlBounds.Height > beforeControlMinimumSize)
               {
                  if (beforeControlBounds.Height + shift < beforeControlMinimumSize)
                  {
                     shift = beforeControlMinimumSize - beforeControlBounds.Height;
                     e.SplitY = selectedSplitterBounds.Y + shift;
                  }
               }
               else
               {
                  shift = 0;
                  e.SplitY = selectedSplitterBounds.Y;
               }
            }
            // if (shift > 0)//we need to check the allow of the after control
            else
            {
               if (afterControlBounds.Height > afterControlMinimumSize)
               {
                  if (afterControlBounds.Height + (-shift) < afterControlMinimumSize)
                  {
                     shift = afterControlBounds.Height - afterControlMinimumSize;
                     e.SplitY = selectedSplitterBounds.Y - (shift);
                  }
               }
               else
               {
                  shift = 0;
                  e.SplitY = selectedSplitterBounds.Y;
               }
            }
         }
         System.Console.WriteLine("shift : " + shift);
         return shift;
      }
   }
}