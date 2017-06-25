using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
#if !PocketPC
using System.Windows.Forms.Layout;
#else
using LayoutEngine = com.magicsoftware.mobilestubs.LayoutEngine;
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// This class provides the layout for mgSplitContainer
   /// </summary>
   class MgSplitContainerLayout : LayoutEngine
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="panel"></param>
      /// <param name="wHint"></param>
      /// <param name="hHint"></param>
      /// <param name="flushCache"></param>
      /// <returns></returns>
      protected internal Point computeSize(Panel panel, int wHint, int hHint, bool flushCache)
      {
         int width = 0, height = 0;

         if (panel.Controls.Count == 0)
         {
            if (wHint != -1)//SWT.DEFAULT)
               width = wHint;
            if (hHint != -1)//SWT.DEFAULT)
               height = hHint;
            return new Point(width, height);
         }
         // compute logical size of composite
         for (int i = 0; i < panel.Controls.Count ; i++)
         {
            Rectangle rect = panel.Controls[i].Bounds;
            width = Math.Max(width, rect.X + rect.Width);
            height = Math.Max(height, rect.Y + rect.Height);
         }

         return new Point(width, height);
      }

      /// <summary> </summary>
      protected internal bool flushCache(Control control)
      {
         return true;
      }

      /// <summary>
      ///  return TRUE if the horizontal placement is allowed, otherwise return FALSE.
      /// </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private bool alloweHorPlacement(Control control)
      {
         bool allow = false;

         MgSplitContainerData data = ((TagData)control.Tag).MgSplitContainerData;
         if (data != null)
         {
            allow = data.allowHorPlacement;
         }
         return allow;
      }

      /// <summary> return TRUE if the horizontal placement is allowed and the control didn't get to his minimum size,
      /// otherwise return FALSE.
      /// </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private bool aboveMinWidth(Control control)
      {
         bool above = false;

         Point minSizePt = GuiUtils.getMinSizeInfo(control).getMinSize(false);
         above = minSizePt.X < control.Bounds.Width;

         return above;
      }

      /// <summary> </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private bool alloweVerPlacement(Control control)
      {
         bool allow = false;

         MgSplitContainerData data = ((TagData)control.Tag).MgSplitContainerData;
         if (data != null)
         {
            allow = data.allowVerPlacement;
         }
         return allow;
      }

      /// <summary> 
      /// return TRUE if the horizontal placement is allowed, otherwise return FALSE.
      /// </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private bool aboveMinHeight(Control control)
      {
         bool above = false;

         Point minSizePt = GuiUtils.getMinSizeInfo(control).getMinSize(false);
         above = minSizePt.Y < control.Bounds.Height;

         return above;
      }

      /// <summary> </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private static Point getOrgSize(Control control)
      {
         Point orgSize = System.Drawing.Point.Empty;

         MgSplitContainerData data = ((TagData)control.Tag).MgSplitContainerData;
         if (data != null)
            orgSize = new Point(data.orgWidth, data.orgHeight);

         return orgSize;
      }

      /// <summary> </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private static Point getOrgMinimumSize(Control control)
      {
         Point pt = GuiUtils.getMinSizeInfo(control).getMinSize(false);
         return pt;
      }

      /// <summary> </summary>
      /// <param name="control"> </param>
      /// <returns> </returns>
      private Point getMinimunSize(Control control)
      {
         MinSizeInfo msInfo = GuiUtils.getMinSizeInfo(control);
         return msInfo.getMinSize(false);
      }

      /// <summary> get width of control related to the sent parameters
      /// 
      /// </summary>
      /// <param name="controls">:
      /// the control array
      /// </param>
      /// <param name="includeInResize">:
      /// include or not include
      /// </param>
      /// <param name="horPlacement:">controls that include in hor\ver placement
      /// </param>
      /// <returns>
      /// </returns>
      private int getTotalSizeOfControls(Control[] controls, int orientation, bool includeInResize, bool calcByMinSize, bool checkPlacement, bool checkMinSize)
      {
         int size = 0;

         bool horPlacement = (orientation == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL);
         bool allowResize = true;

         for (int i = 0; i < controls.Length; i++)
         {
            if (horPlacement)
            {
               if (checkPlacement)
               {
                  allowResize = alloweHorPlacement(controls[i]);
                  if (checkMinSize && allowResize)
                     allowResize = aboveMinWidth(controls[i]);
               }

               if ((includeInResize && allowResize) || (!includeInResize && !allowResize))
               {
                  Point pt;
                  if (calcByMinSize)
                     pt = getOrgMinimumSize(controls[i]);
                  else
                     pt = getOrgSize(controls[i]);
                  size += pt.X;
               }
            }
            else
            {
               if (checkPlacement)
               {
                  allowResize = alloweVerPlacement(controls[i]);
                  if (checkMinSize && allowResize)
                     allowResize = aboveMinHeight(controls[i]);
               }

               if ((includeInResize && allowResize) || (!includeInResize && !allowResize))
               {
                  Point pt;
                  if (calcByMinSize)
                     pt = getOrgMinimumSize(controls[i]);
                  else
                     pt = getOrgSize(controls[i]);
                  size += pt.Y;
               }
            }
         }
         return size;
      }

      /// <summary> </summary>
      /// <param name="controls">
      /// </param>
      /// <param name="orientation">
      /// </param>
      /// <param name="includeInPlacement">
      /// </param>
      /// <returns>
      /// </returns>
      internal static bool allControlsHaveBounds(Control[] controls)
      {
         bool retValue = true;

         for (int i = 0; i < controls.Length && retValue; i++)
         {
            Point pt = getOrgSize(controls[i]);
            if (pt.X == 0 || pt.Y == 0)
               retValue = false;
         }
         return retValue;
      }

       /// <summary>
      /// SetNewSaveBound
       /// </summary>
       /// <param name="control"></param>
       /// <param name="width"></param>
       /// <param name="Height"></param>
      void SetNewSaveBound(Control control, int x, int y, int width, int height)
      {
          Rectangle newRect = new Rectangle(x, y, (int)width, height);
          GuiUtils.saveBounds(control, newRect);
      }


      /// <summary>
      /// </summary>
      /// <param name="container"></param>
      /// <param name="layoutEventArgs"></param>
      /// <returns></returns>
      public override bool Layout(object composite, LayoutEventArgs layoutEventArgs)
      {
         /// <summary> layout the MgSplitContainer and all his children</summary>
      //protected internal void layout(Composite composite, bool flushCache)
      //{
         MgSplitContainer mgSplitContainer = (MgSplitContainer)composite;
         //Sometime we need to ignore the layout, when we doing move to control
         if (mgSplitContainer.IgnoreLayout)
            return true;

         double[] ratios = null;
         int totalOrgSize = 0;

         Rectangle clientArea = mgSplitContainer.ClientRectangle;
         if (clientArea.Width <= 1 || clientArea.Height <= 1)
            return false;

         // get the controls on the mgSplitContainer (without the splitter, and only the visible)
         Control[] controls = mgSplitContainer.getControls(false);
         if (controls.Length == 0)
            return false;
         //      if (allControlsHaveBounds(controls) == false)
         //         return;

         // check the splitter controls
         // checkSplitterControls(MgSplitContainer, controls);

         // check the direction shell was reduce or increased
         bool parentReduced = false;
         Rectangle lastClientArea = mgSplitContainer.LastClientArea;
         if (clientArea.Width < lastClientArea.Width || clientArea.Height < lastClientArea.Height)
            parentReduced = true;
         ((TagData)mgSplitContainer.Tag).RepaintRect = lastClientArea;

         // save the last client Area, for the next time
         mgSplitContainer.LastClientArea = GuiUtils.copyRect(clientArea);

         // get the array to the splitter controls
         Splitter[] splitters = new Splitter[mgSplitContainer.Splitters.Count];
         for (int i = 0; i < mgSplitContainer.Splitters.Count; i++)
         {
            Splitter s = mgSplitContainer.Splitters[i];
            splitters[i] = s;
         }

         //MOVE TO mgSplitContainer!!!
         // for the out most MgSplitContainer form we need to change the client rect, not to include the offset
         if (GuiUtils.isOutmostMgSplitContainer(mgSplitContainer))
         {
            clientArea.X += (MgSplitContainer.SPLITTER_WIDTH / 2);
            clientArea.Y += (MgSplitContainer.SPLITTER_WIDTH / 2);
            clientArea.Width -= MgSplitContainer.SPLITTER_WIDTH;
            clientArea.Height -= MgSplitContainer.SPLITTER_WIDTH;
         }

         // check if we need to update the original size at the end of the method?
         bool saveSizeAtTheEnd = ratiosWillChange(parentReduced, mgSplitContainer.getOrientation(), controls);

         bool checkPlacement = true;
         bool checkMinimumSize = parentReduced;
         bool calcByMinSize = true;

         // get the dynamic client area by checking the placement & minimum size(only when we decrease)
         int areaS = calcAreaS(mgSplitContainer, clientArea, controls, splitters, checkPlacement, checkMinimumSize, false);

         // get the total minimum size by checking the placement & minimum size(only when we decrease)
         int totalMinimumSize = getTotalSizeOfControls(controls, mgSplitContainer.getOrientation(), true, calcByMinSize, checkPlacement, checkMinimumSize);

         // flag to set if we ignore the minimum size: Minimum size is ignored when 
         // 1. increasing the size
         // or
         // 2. reducing the size and the total minimum size is greater than the dynamic client area
         //    and when we not on outmost MgSplitContainer
         bool ignoreMinSize = false;
         if (!GuiUtils.isOutmostMgSplitContainer(mgSplitContainer) && !GuiUtils.isDirectChildOfOutmostMgSplitContainer(mgSplitContainer))
            ignoreMinSize = true;

         checkPlacement = false;
         checkMinimumSize = false;
         calcByMinSize = false;
         if (areaS > 0)
         {
            if (!parentReduced)
            {
               output(mgSplitContainer.getOrientation(), "Area ", "1");
               // Check the placement property and ignore the minimum size
               ignoreMinSize = true;
               checkPlacement = true;
               checkMinimumSize = false;
            }
            else if (areaS < totalMinimumSize)
            // parentReduced is true
            {
               output(mgSplitContainer.getOrientation(), "Area ", "2");
               // when reduce and the dynamic client area is > total minimum size we need to ignore also the
               // placement and the minimum size, all the controls will be in the resize
               ignoreMinSize = true;
               checkPlacement = false;
               checkMinimumSize = false;
            }
            else
            {
               output(mgSplitContainer.getOrientation(), "Area ", "3");
               // when parentReduced && areaS > totalMinimumSize, check the placement and minimum size
               checkPlacement = true;
               checkMinimumSize = true;
            }
         }
         // If the dynamic client area is little than or equal to zero then
         // if (areaS <= 0)
         else
         {
            output(mgSplitContainer.getOrientation(), "Area ", "<0");
            output(mgSplitContainer.getOrientation(), "areaS < totalMinimumSize? ", " " + (areaS < totalMinimumSize));

            // when we on outmost MgSplitContainer :use the minimum sizes as the weights.
            if (GuiUtils.isOutmostMgSplitContainer(mgSplitContainer))
            {
               Debug.Assert(false);
               // output(mgSplitContainer.getOrientation(), "calByMinSize ", " " + calcByMinSize);
               // checkPlacement = false;
               // checkMinimumSize = false;
               // calcByMinSize = true;
            }
            // when we not in outmost MgSplitContainer :save the size in the org size and cal with ignore all the
            // placemetn & min size
            // if (!GuiUtils.isOutmostMgSplitContainer(mgSplitContainer))
            else
            {
               output(mgSplitContainer.getOrientation(), "calByMinSize ", " " + calcByMinSize);
               checkPlacement = false;
               checkMinimumSize = false;
               calcByMinSize = false;
            }
         }

         // get the dynamic client area according to the init members
         areaS = calcAreaS(mgSplitContainer, clientArea, controls, splitters, checkPlacement, checkMinimumSize, calcByMinSize);
         // get the total size according to the init members
         totalOrgSize = getTotalSizeOfControls(controls, mgSplitContainer.getOrientation(), true, calcByMinSize, checkPlacement, checkMinimumSize);
         // get the ratios according to the init members
         ratios = calcRatios(controls, mgSplitContainer.getOrientation(), totalOrgSize, checkPlacement, checkMinimumSize, calcByMinSize, areaS, ref saveSizeAtTheEnd);

         output(mgSplitContainer.getOrientation(), "areaS ", " " + areaS);

         // 6. order the controls and splitters
         //TODOR: border is all the time 0 // int splitterWidth = splitters.Length > 0 ? MgSplitContainer.SPLITTER_WIDTH + splitters[0].getBorderWidth() * 2 : MgSplitContainer.SPLITTER_WIDTH;                 
         int splitterWidth = MgSplitContainer.SPLITTER_WIDTH;                 
         if (mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
         {
            int width = ratios[0] != -1 ? (int)(areaS * ratios[0]) : getOrgSize(controls[0]).X;
            if (!ignoreMinSize)
               width = Math.Max(width, getMinimunSize(controls[0]).X);
            int x = clientArea.X;

             // fixed bug #:293109, while the user is do resize we need to save te bound
            GuiUtils.controlSetBounds(controls[0], x, clientArea.Y, (int)width, clientArea.Height);
            SetNewSaveBound(controls[0], x, clientArea.Y, (int)width, clientArea.Height);

            x += width;

            if (controls.Length > 1)
            {
               ((TagData)splitters[splitters.Length - 1].Tag).RepaintRect = splitters[splitters.Length - 1].Bounds;
               GuiUtils.controlSetBounds(splitters[splitters.Length - 1], x, clientArea.Y, splitterWidth, clientArea.Height);
               x += splitterWidth;
               width = clientArea.Width - (x - clientArea.X);
               if (!ignoreMinSize)
                  width = Math.Max(width, getMinimunSize(controls[controls.Length - 1]).X);

               GuiUtils.controlSetBounds(controls[controls.Length - 1], x, clientArea.Y, width, clientArea.Height);
               SetNewSaveBound(controls[controls.Length - 1], x, clientArea.Y, width, clientArea.Height);
            }
         }
         else if (mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_VERTICAL)
         {
            int height = ratios[0] != -1 ? (int)(areaS * ratios[0]) : getOrgSize(controls[0]).Y;
            if (!ignoreMinSize)
               height = Math.Max(height, getMinimunSize(controls[0]).Y);

            int y = clientArea.Y;
            GuiUtils.controlSetBounds(controls[0], clientArea.X, clientArea.Y, clientArea.Width, height);
            SetNewSaveBound(controls[0], clientArea.X, clientArea.Y, clientArea.Width, height);
            y += height;

            if (controls.Length > 1)
            {
               ((TagData)splitters[splitters.Length - 1].Tag).RepaintRect = splitters[splitters.Length - 1].Bounds;
               GuiUtils.controlSetBounds(splitters[splitters.Length - 1], clientArea.X, y, clientArea.Width, splitterWidth);
               SetNewSaveBound(splitters[splitters.Length - 1], clientArea.X, y, clientArea.Width, splitterWidth);

               y += splitterWidth;
               height = clientArea.Height - (y - clientArea.Y);
               if (!ignoreMinSize)
                  height = Math.Max(height, getMinimunSize(controls[controls.Length - 1]).Y);
               GuiUtils.controlSetBounds(controls[controls.Length - 1], clientArea.X, y, clientArea.Width, height);
               SetNewSaveBound(controls[controls.Length - 1], clientArea.X, y, clientArea.Width, height);
            }
         }

         if (saveSizeAtTheEnd)
         {
            for (int k = 0; k < controls.Length; k++)
            {
               Rectangle rect = controls[k].Bounds;
               setOrgSizeBy(controls[k], rect.Width, rect.Height);
            }
         }

         return true;
      }

      /// <summary> </summary>
      /// <param name="orientation">
      /// </param>
      /// <returns>
      /// </returns>
      private bool output(int orientation, String ttl, String str)
      {

         return false;
         //      
         // if (orientation == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
         // {
         // if (str != null)
         // System.out.println(ttl + ": " + str);
         // }
         //
         // return (orientation == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL);
      }

      /// <summary> When to update the original size? 1. When dragging the splitter - because the relations between the frame
      /// change (on MgSplitContainer.onDragsplitter) 2. When one frame doesn't allow placement then again the relations
      /// between the frame change 3. When reducing the size and one frame (or more) stopped reducing its size
      /// because it has reached to its minimum size.
      /// 
      /// </summary>
      /// <param name="orientation">
      /// </param>
      /// <param name="frames">
      /// </param>
      /// <returns>
      /// </returns>
      private bool ratiosWillChange(bool shellReduced, int orientation, Control[] frames)
      {
         bool ratiosWillChange = false;
         int countFrameGetToMinSize = 0;

         for (int k = 0; k < frames.Length && !ratiosWillChange; k++)
         {
            Point minSize;
            if (orientation == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
            {
               // When one frame doesn't allow placement then again the relations between the frame change
               if (!alloweHorPlacement(frames[k]))
                  ratiosWillChange = true;
               else if (shellReduced)
               {
                  minSize = GuiUtils.getMinSizeInfo(frames[k]).getMinSize(false);
                  // When reducing the size and one frame (or more) stopped reducing its size because it has
                  // reached to its minimum size.
                  if (minSize.X >= frames[k].Bounds.Width)
                     countFrameGetToMinSize++;
               }
            }
            else
            {
               // When one frame doesn't allow placement then again the relations between the frame change
               if (!alloweVerPlacement(frames[k]))
                  ratiosWillChange = true;
               else if (shellReduced)
               {
                  minSize = GuiUtils.getMinSizeInfo(frames[k]).getMinSize(true);
                  // When reducing the size and one frame (or more) stopped reducing its size because it has
                  // reached to its minimum size.
                  if (minSize.Y >= frames[k].Bounds.Height)
                     countFrameGetToMinSize++;
               }
            }
         }

         if (!ratiosWillChange && shellReduced)
         {
            if (countFrameGetToMinSize > 0 && countFrameGetToMinSize < frames.Length)
               ratiosWillChange = true;
         }
         return ratiosWillChange;
      }

      /// <summary> The dynamic client area is calculated by taking the client area and subtracting the non-allowed
      /// placement frames and if reducing the size then also subtract frames that have reached their minimum
      /// size.
      /// 
      /// </summary>
      /// <param name="MgSplitContainer">
      /// </param>
      /// <param name="clientArea">
      /// </param>
      /// <param name="controls">
      /// </param>
      /// <param name="splitters">
      /// </param>
      /// <returns>
      /// </returns>
      private int calcAreaS(MgSplitContainer mgSplitContainer, Rectangle clientArea, Control[] controls, Splitter [] splitters, bool checkProperties, bool checkMinSize, bool calWithMinSize)
      {
         // 1- calculate the width of all the splitters
         int allSplittersWidth = MgSplitContainer.SPLITTER_WIDTH;
         //TODO: Border size is all the time 0
         //if (splitters.Length > 0)
         //allSplittersWidth = Splitters.Length * (MgSplitContainer.SPLITTER_WIDTH + Splitters[0].getBorderWidth() * 2);

         // 2- calculate the original area- width and height, of all the controls that NOT in placement
         int widthControlsNoInPlacement = getTotalSizeOfControls(controls, mgSplitContainer.getOrientation(), false, calWithMinSize, checkProperties, checkMinSize);

         // 3 - calculate the area,client area - all the Splitter width - all the controls that not include in
         // placement
         int areaS = (mgSplitContainer.getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL ? clientArea.Width : clientArea.Height);
         areaS -= (allSplittersWidth + widthControlsNoInPlacement);
         return areaS;
      } 
     

      /// <summary> </summary>
      /// <param name="controls:">array of controls
      /// </param>
      /// <param name="horSplitters">:
      /// True if it's horizontal split, otherwise it's vertical split 
      /// </param>
      /// <param name="totalOrgSize">
      /// </param>
      /// <returns>
      /// </returns>
      private double[] calcRatios(Control[] controls, int orientation, int totalSize, bool checkPlacement, bool checkMinSize, 
                                  bool calcWithMinSize, int areaS, ref bool saveSizeAtTheEnd)
      {
         // calculate the ratios of the controls
         double[] ratios = new double[controls.Length];

         bool horSpliter = orientation == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL;         

         for (int i = 0; i < controls.Length; i++)
         {
            bool allowResize = true;
            ratios[i] = -1;
            Point size;

            if (calcWithMinSize)
               size = getOrgMinimumSize(controls[i]);
            else
               size = getOrgSize(controls[i]);

            if (horSpliter)
            {
               if (checkPlacement)
               {
                  allowResize = alloweHorPlacement(controls[i]);
                  if (checkMinSize && allowResize)
                     allowResize = aboveMinWidth(controls[i]);
               }

               calcRationsForCell(horSpliter, controls, totalSize, checkMinSize, areaS, ref saveSizeAtTheEnd, ratios, i, allowResize, ref size);
            }
            else
            {
               if (checkPlacement)
               {
                  allowResize = alloweVerPlacement(controls[i]);
                  if (checkMinSize && allowResize)
                     allowResize = aboveMinHeight(controls[i]);
               }

               calcRationsForCell(horSpliter, controls, totalSize, checkMinSize, areaS, ref saveSizeAtTheEnd, ratios, i, allowResize, ref size);
            }
         }
         return ratios;
      }

      /// <summary>
      /// Check if the new size is less then the min size then reduce the cell only the offset that can be reduce 
      /// </summary>
      /// <param name="horSpliter"></param>
      /// <param name="controls"></param>
      /// <param name="totalSize"></param>
      /// <param name="checkMinSize"></param>
      /// <param name="areaS"></param>
      /// <param name="saveSizeAtTheEnd"></param>
      /// <param name="ratios"></param>     
      /// <param name="i"></param>
      /// <param name="allowResize"></param>
      /// <param name="size"></param>
      private void calcRationsForCell(bool horSpliter, Control[] controls, int totalSize, bool checkMinSize, int areaS, ref bool saveSizeAtTheEnd, double[] ratios, int i, bool allowResize, ref Point size)
      {
         bool calcNewReg = false;

         if (allowResize && (double)totalSize > 0)
         {
            ratios[i] = (horSpliter ? (double)size.X : (double)size.Y) / (double)totalSize;
            //If the control is bigger then the min size then check the offset that we can decrese it
            bool check = false;
            int minSize = 0;

            if (horSpliter)
            {
               minSize = getMinimunSize(controls[0]).X;
               check = (controls[0].Size.Width > minSize);
               
            }
            else
            {
               minSize = getMinimunSize(controls[0]).Y;
               check = (controls[0].Size.Height > minSize);
            }

            if (checkMinSize && check)
            {
               int newWidth = (int)(areaS * ratios[i]);
               //the dec will be less then the min size
               if (newWidth < minSize)
               {
                  ratios[i] = minSize / (double)areaS;

                  calcNewReg = true;
                  saveSizeAtTheEnd = true;
               }
            }

            if (calcNewReg && ratios[i] != -1 && i == controls.Length - 1)
               ratios[i] = 1 - ratios[0];
         }
      }

      /// <summary> </summary>
      /// <param name="control">
      /// </param>
      /// <param name="width">
      /// </param>
      /// <param name="height">
      /// </param>
      internal void setOrgSizeBy(Control control, int width, int height)
      {
         MgSplitContainerData data = ((TagData)control.Tag).MgSplitContainerData;
         if (data != null)
         {
            data.orgWidth = width;
            data.orgHeight = height;
         }
      }
   }
}