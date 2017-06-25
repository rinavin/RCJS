using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls;
using System.Collections;
using System.Diagnostics;
#if PocketPC
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using Panel = com.magicsoftware.controls.MgPanel;
#endif

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Handles placement of control
   /// </summary>
   public class PlacementLayoutBase
   {
      protected readonly Control _mainComposite; // shell or scrolled composite for subforms or tab or table or group
      protected Rectangle _prevRect; // previous rectangle
      private Point _prevLogicalSize;

      public PlacementLayoutBase(Control mainComposite, Rectangle rect, bool initLogSize)
      {
         _mainComposite = mainComposite;


         _prevRect = rect; 

         int NonClientAreaHeight = 0;
         int NonClientAreaWidth = 0;

         // limit the change for subforms only
         if (mainComposite is MgPanel && mainComposite.Parent != null && mainComposite.Parent is MgPanel)
         {
            Point clientLocation = mainComposite.PointToScreen(new Point());
            Point controlLocation = mainComposite.Parent.PointToScreen(mainComposite.Location);

            if (mainComposite.Size.Width - mainComposite.ClientSize.Width > 0)
               NonClientAreaWidth = (clientLocation.X - controlLocation.X) * 2;

            if (mainComposite.Size.Height - mainComposite.ClientSize.Height > 0)
               NonClientAreaHeight = (clientLocation.Y - controlLocation.Y) * 2;

         }
         else
         {
            NonClientAreaHeight = mainComposite.Size.Height - mainComposite.ClientSize.Height;
            NonClientAreaWidth = mainComposite.Size.Width - mainComposite.ClientSize.Width;
         }

         ReCalculateAndRefresh();
         
         _prevRect.Height += NonClientAreaHeight;
         _prevRect.Width += NonClientAreaWidth;

         _prevLogicalSize = (initLogSize
                                 ? new Point(0, 0)
            // do not initial the logical size, let the placement work
                                 : computeSize(GetInnerControl(mainComposite)));
         
      }

      protected virtual void ReCalculateAndRefresh()
      {
         LogicalControlsContainer logicalControlsContainer = GetLogicalControlsContainer(_mainComposite);
         if (logicalControlsContainer != null && !logicalControlsContainer.SizeChange.IsEmpty)
         {
            logicalControlsContainer.SizeChange = new Point();
            foreach (var item in logicalControlsContainer.LogicalControls)
            {
               ReCalculateAndRefresh(item);
            }
         }
      }

      /// <summary>
      /// Returns inner control for a container control
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      protected virtual Control GetInnerControl(Control container)
      {
         throw new NotImplementedException();
      }
      
      /// <summary>
      /// Return LogicalControlsContainer for a container control
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      protected virtual LogicalControlsContainer GetLogicalControlsContainer(Control container)
      {
         return null;
      }

      /// <summary>
      /// Returns whether TableControl is in Column Creation mode
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual bool IsTableInColumnCreation(TableControl control)
      {
         return false;
      }

      /// <summary>
      /// Execute placement for TableControl
      /// </summary>
      /// <param name="control">TableControl</param>
      /// <param name="dx">horizontal placement value</param>
      /// <param name="dy">vertical placement value</param>
      protected void ExecuteTableControlPlacement(TableControl control, int dx, int dy,  Rectangle rect)
      {
         if (!IsTableInColumnCreation(control))
         {

            int prevWidth = 0;
#if !PocketPC //tmp
            bool containerRightToLeft = control.Parent.RightToLeft == RightToLeft.Yes;
#else
            bool containerRightToLeft = false;
#endif

            //negative width and height becomes 0 in setBounds, so get the actual height from the saved data.
            Rectangle? savedRect = GetSavedBounds(control);
            if (savedRect != null)
               rect.Height = ((Rectangle)savedRect).Height;
            rect.Height += dy;

            prevWidth = rect.Width;
            // compute allowed width placement depending on columns placement
            dx = GetTablePlacementManager(control).computeWidthPlacement(dx, GetTableColumns(control));
            rect.Width += dx;
            if (containerRightToLeft)
               rect.X -= dx;

            // update columns on table placement
            ExecuteTablePlacement(control, prevWidth, dx, rect);
         }
      }

      /// <summary>
      /// Executes Table Placement
      /// </summary>
      /// <param name="control"></param>
      /// <param name="prevWidth"></param>
      /// <param name="dx"></param>
      /// <param name="rect"></param>
      protected virtual void ExecuteTablePlacement(TableControl control, int prevWidth, int dx, Rectangle rect)
      {
      }

      /// <summary>
      /// Get TablePlacementManager object
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected TablePlacementManagerBase GetTablePlacementManager(TableControl control)
      {
         ITableManager tableManager = GetTableManager(control);

         if (tableManager != null)
            return tableManager.TablePlacementManager;

         return null;
      }

      /// <summary>
      /// Gets List of LogicalColumns
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      List<ILogicalColumn> GetTableColumns(TableControl control)
      {
         ITableManager tableManager = GetTableManager(control);
         return tableManager.ColumnsManager.Columns;
      }

      protected virtual ITableManager GetTableManager(TableControl control)
      {
         return null;
      }
      
      /// <summary>
      /// Return bounds of control saved earlier
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual Rectangle? GetSavedBounds(Control control)
      {
         throw new NotImplementedException();
      }
      
      /// <summary>
      /// Set bounds to control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="rect"></param>
      protected virtual void SetBounds(Control control, Rectangle rect)
      {
         throw new NotImplementedException();
      }
      
      /// <summary>
      /// Returns actual placement for Table considering all columns placement value (Yes/No)
      /// </summary>
      /// <param name="control"></param>
      /// <param name="place"></param>
      /// <param name="dx"></param>
      /// <param name="totalPlaceX"></param>
      void GetTableTotalPlaceX(TableControl control, Rectangle place, int dx, ref int totalPlaceX)
      {
         int columnPlace = 0; // all the widths of columns with placement
         TablePlacementManagerBase tablePlacementManager = GetTablePlacementManager(control);

         if (tablePlacementManager != null && place.Width != 0 && dx < 0)
         {
            // accumulates all the widths of columns with placement,
            // reruns -1 if there is no such columns
            columnPlace = tablePlacementManager.allowedPlacementWidth(dx, GetTableColumns(control));
            if (columnPlace >= 0)
            {
               //compute real table placement = when some columns are resized and some are not(because they have placement = no"
               int realTableDXPlacement = Math.Min(place.Width, -columnPlace * 100 / dx);
               totalPlaceX = place.X + realTableDXPlacement;
            }
            else
            {
               columnPlace = 0;
            }
         }
      }
      
      /// <summary>
      /// Returns whether placement can be applied to object
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual bool ShouldApplyPlacement(object control) { return true; }
      
      /// <summary>
      /// Returns whether can LimitPlacement to container control
      /// </summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      protected virtual bool CanLimitPlacement(Control containerControl) { return true; }
      
      /// <summary>
      /// Returns whether LimitPlacement should be applied to control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual bool ShouldLimitPlacement(Control control) { return true; }
      
      /// <summary>
      /// Returns whether layout can be performed for a form
      /// </summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      protected virtual bool CanPerformLayout(Control containerControl) { return true; }
      
      /// <summary>
      /// Get bounds of control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual Rectangle GetBounds(Control control)
      {
         // TODO: Ashish: Consider offset
         return control.Bounds; 
      }
      
      /// <summary>
      /// Return maximum value for width and height from all controls in container
      /// </summary>
      /// <param name="container"></param>
      /// <param name="width">maximum width</param>
      /// <param name="height">maximum height</param>
      protected virtual void GetMaxOfActualControlDimensions(Control container, ref int width, ref int height)
      {

      }

      /// <summary>
      /// recalculate control's position and refresh it 
      /// </summary>
      /// <param name="obj"></param>
      protected virtual void ReCalculateAndRefresh(Object obj)
      {
         BasicCoordinator staticControl = (BasicCoordinator)((PlacementDrivenLogicalControl)obj).Coordinator;
         staticControl.calcDisplayRect();
      }

      /// <summary>
      /// Get Bounds
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      protected virtual Rectangle Bounds(Object obj)
      {
         return ((PlacementDrivenLogicalControl)obj).getRectangle();
      }

      /// <summary>
      /// Get Placement data
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      protected virtual PlacementData PlacementData(Object obj)
      {
         return null;
      }

      /// <summary>
      /// Compute logical size
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      public Point computeSize(Control container)
      {
         int width = 0, height = 0;

         //all this is not relevant for table control , to improve performance return size
         if (container is TableControl)
            return new Point(container.Size.Width, container.Size.Height);

         GetMaxOfActualControlDimensions(container, ref width, ref height);

         Size size = new Size();
         LogicalControlsContainer logicalControlsContainer = GetLogicalControlsContainer(container);
         if (logicalControlsContainer != null)
            size = logicalControlsContainer.computeSize();

         return new Point(Math.Max(width, size.Width), Math.Max(height, size.Height));
      }


      /// <summary> computes and updates the minimum logical size required based on contained controls. </summary>
      /// <param name="mainControl"></param>
      public void computeAndUpdateLogicalSize(Control mainControl)
      {
         _prevLogicalSize = computeSize(mainControl);
         updateAutoScrollMinSize(mainControl, new Size(_prevLogicalSize.X, _prevLogicalSize.Y));
      }

      /// <summary>
      /// update min size
      /// </summary>
      /// <param name="mainControl"></param>
      /// <param name="size"></param>
      void updateAutoScrollMinSize(Control mainControl, Size size)
      {
         if (mainControl is Panel && ((Panel)mainControl).AutoScroll)
            ((Panel)mainControl).AutoScrollMinSize = size;
      }

      
      /// <summary>
      /// update the movement value according the dimension placement value.
      ///  copies from ctrl.cpp updateMoveValue
      /// </summary>
      /// <param name="placementData"></param>
      /// <param name="actualMove"></param>
      /// <param name="placementDim"></param>
      /// <returns></returns>
      static int updateMoveValue(PlacementData placementData, int actualMove, PlacementDim placementDim, int prevAxeMove, bool containerRightToLeft)
      {
         float remainder;
         float result;
         int move;
         int place = placementData.getPlacement(placementDim, containerRightToLeft);
         float accMove = placementData.getAccCtrlMove(placementDim);

         if (place > 0 && place < 100)
         {
            result = (float)(actualMove * place) / 100;
            move = (int)((actualMove * place) / 100);
            remainder = result - move;
            accMove += remainder;
            if (accMove >= 1 || accMove <= -1)
            {
               move += (int)accMove;
               if (move > 0)
                  accMove -= 1;
               else
                  accMove += 1;
            }
         }
         else
            if (place == 0)   // no placement
               move = 0;
            else
               move = actualMove;   // placement is 100%
         placementData.setAccCtrlMove(placementDim, accMove);

         return move;
      }

      /// <summary>
      /// controls placement
      /// </summary>
      /// <param name="control"></param>
      /// <param name="actualPlacement"></param>
      public void ControlPlacement(Object obj, Point actualPlacement)
      {
         if (obj is Control)
         {
            Control control = (Control)obj;
            if (!ShouldApplyPlacement(control))
               return;
            int dx, dy;
            Rectangle rect = Bounds(control); 
#if !PocketPC //tmp
            bool containerRightToLeft = control.Parent.RightToLeft == RightToLeft.Yes;
#else
            bool containerRightToLeft = false;
#endif
            PlacementData placementData = PlacementData(control);

            if (placementData == null)
               return;
            
            // process placement for all children
            int xChange = updateMoveValue(placementData, actualPlacement.X, PlacementDim.PLACE_X, 0, containerRightToLeft);
            rect.X += xChange;
           
            int yChange = updateMoveValue(placementData, actualPlacement.Y, PlacementDim.PLACE_Y, 0, containerRightToLeft);
            rect.Y += yChange;

            // compute width and height placement
            // compute width and height placement
            dx = updateMoveValue(placementData, actualPlacement.X, PlacementDim.PLACE_DX, xChange, containerRightToLeft);
            dy = updateMoveValue(placementData, actualPlacement.Y, PlacementDim.PLACE_DY, yChange, containerRightToLeft);

            //QCR #288201, due to calculation problems caused by rounding float to int sometimes scrollbar is shown
            //This is fix for the case when total placement is 100 %
            dx = fixRounding(xChange, dx, actualPlacement.X, Axe.X, placementData, containerRightToLeft);
            dy = fixRounding(yChange, dy, actualPlacement.Y, Axe.Y, placementData, containerRightToLeft);
            
            if (control is TableControl)
            {
               ExecuteTableControlPlacement((TableControl)control, dx, dy, rect);
            }
            else
            {
               //QCR #933614, negative width and height turned to 0 in setBounds,
               //so we must save then on the control's data
               Rectangle? savedRect = GetSavedBounds(control);
               if (savedRect != null)
               {
                  rect.Width = ((Rectangle)savedRect).Width;
                  rect.Height = ((Rectangle)savedRect).Height;
               }

               rect.Width += dx;
               if (containerRightToLeft)
                  rect.X -= dx;

               rect.Height += dy;
               SetBounds(control, rect);
            }
         }
         else if (obj is PlacementDrivenLogicalControl)
         {
            ReCalculateAndRefresh(obj);
         }
      }

      /// <summary>
      /// Fix Rounding mistakes caused by placement calculations
      /// </summary>
      /// <param name="originPlacement"> placement of the origine (x or y placement) </param>
      /// <param name="lengthPlacement"> widht or height placement (dx or dy placement)</param>
      /// <param name="totalChange"> total change for control</param>
      /// <param name="axe"> X or Y axe</param>
      /// <param name="placementData"> placement data</param>
      /// <param name="containerRightToLeft">RightToLeft</param>
      /// <returns></returns>
      static int fixRounding(int originPlacement, int lengthPlacement, int totalChange, Axe axe, PlacementData placementData, bool containerRightToLeft)
      {
         PlacementDim originPlacementDim = axe == Axe.X ? PlacementDim.PLACE_X : PlacementDim.PLACE_Y;
         PlacementDim lengthPlacementDim = axe == Axe.X ? PlacementDim.PLACE_DX : PlacementDim.PLACE_DY; 

         if (placementData.getPlacement(originPlacementDim, containerRightToLeft) + placementData.getPlacement(lengthPlacementDim, containerRightToLeft) == 100)
         {
            //total placement is 100% this means that originPlacement + lengthPlacement MUST BE EQUAL to totalChange
            //there are may be some mistakes caused by rounding of float numbers to integer, we must fix them
            //If these small mistakes are not fix - scroll bar might be created when it is not needed
            if (Math.Abs(originPlacement + lengthPlacement) != Math.Abs(totalChange))
            {
               int diff = totalChange - (originPlacement + lengthPlacement);
               float accMove = placementData.getAccCtrlMove(lengthPlacementDim);
               //recalculate length placement
               lengthPlacement += diff;
               //update AccCtrlMove for future placement
               placementData.setAccCtrlMove(lengthPlacementDim, accMove + diff);
            }
         }
         return lengthPlacement;
      }

      /// <summary> limit placement check if one of the ctrl's caused the scrollbar to be shown or removed, if so limit the
      /// movement. dx & dy are the actual window movement. Will return the allowed movement before scrollbars.
      /// this code is based on GUI::LimitPlacement method in gui.cpp
      /// </summary>
      /// <param name="composite"></param>
      /// <param name="dx">delta X</param>
      /// <param name="dy">delta Y</param>
      /// <param name="newContainerRect">new Recatngle of container</param>
      /// <returns> new dx,dy</returns>
      private Point limitPlacement(Control containerControl, int dx, int dy, Rectangle newContainerRect)
      {
         int totalPlaceX, totalPlaceY;
         int fromPrevToCtrlX, fromPrevToCtrlY;
         int bottom;
         Rectangle place = new Rectangle(0, 0, 0, 0);
         ArrayList controlsList;
         bool containerRightToLeft = isRTL(containerControl);
         bool canHaveScrollbar = (containerControl is ScrollableControl && ((ScrollableControl)containerControl).AutoScroll);

         if (!canHaveScrollbar) //QCR #982071, if control does not support scrollbars - tab, group - we should not limit placement
            return new Point(dx, dy);

         if (CanLimitPlacement(containerControl) && (dx < 0 || dy < 0))
         {
            controlsList = GetControlsList(containerControl);

            foreach (Object obj in controlsList)
            {
#if PocketPC
               if (containerControl is Panel && obj == ((Panel)containerControl).dummy)
                  continue;
#endif
               //ignore editors, they will be handled by logical controls
               Control control = obj as Control;
               if (!ShouldLimitPlacement(control)) //it will be handled by its logicalcontrol
                  continue;

               Rectangle rect = Bounds(obj);
               PlacementData placementData = PlacementData(obj);
               if (placementData != null)
                  place = placementData.Placement;
               else
                  place = new Rectangle(0, 0, 0, 0);
               if (containerRightToLeft)
                  totalPlaceX = (100 - place.X) + place.Width;
               else
                  totalPlaceX = place.X + place.Width;
               totalPlaceY = place.Y + place.Height;

               if (obj is TableControl)
               {
                  GetTableTotalPlaceX((TableControl)obj, place, dx, ref totalPlaceX);
               }

               int childRight = rect.X + rect.Width;
               int childBottom = rect.Y + rect.Height;

               fromPrevToCtrlX = _prevRect.Width - childRight;
               fromPrevToCtrlY = _prevRect.Height - childBottom;

               int right = 0;
               // Checks which of the controls will be the closest one to the right border of the form
               if (dx < 0 && totalPlaceX < 100 && fromPrevToCtrlX >= 0 && newContainerRect.Width < childRight)
               {
                  right = -((fromPrevToCtrlX * 100) / (100 - totalPlaceX));
                  dx = Math.Max(dx, right);
               }

               if (containerRightToLeft && dx < 0) //QCR #974011, 805098
               {
                  long totalPlaceXLeft = (100 - place.X) - place.Width;
                  long left = rect.X + (dx * totalPlaceXLeft / 100); //new left coordinate of control

                  if (left < right /* not hidden*/ && left < 0)
                     dx = Math.Max(-rect.Left, dx);

               }

               // Checks which of the controls will be the closest one to the bottom border of the form
               if (dy < 0 && totalPlaceY < 100 && fromPrevToCtrlY >= 0 && newContainerRect.Height < childBottom)
               {
                  bottom = -((fromPrevToCtrlY * 100) / (100 - totalPlaceY));
                  dy = Math.Max(dy, bottom);
               }

            }
         }

         if (dx > 0)
         {
            // if there was a horizontal scrollbar and after the current resizing it might won't be
            if (_prevRect.Width < _prevLogicalSize.X && newContainerRect.Width >= _prevLogicalSize.X)
               dx = newContainerRect.Width - _prevLogicalSize.X;
         }

         if (dy > 0)
         {
            // if there was a vertical scrollbar and after the current resizing it might won't be
            if (_prevRect.Height < _prevLogicalSize.Y && newContainerRect.Height >= _prevLogicalSize.Y)
               dy = newContainerRect.Height - _prevLogicalSize.Y;
         }

         return new Point(dx, dy);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      private bool isRTL(Control containerControl)
      {
#if !PocketPC
         return containerControl.RightToLeft == RightToLeft.Yes;
#else
         return false;
#endif
      }

      /// <summary>
      /// return list of chlid controls including static controls
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      private ArrayList GetControlsList(Control containerControl)
      {
         ArrayList array = new ArrayList(); // Can't use List<T> - may hold controls and logical controls

         array.AddRange(containerControl.Controls);
         LogicalControlsContainer logicalControlsContainer = GetLogicalControlsContainer(containerControl);
         if (logicalControlsContainer != null && logicalControlsContainer.LogicalControls != null)
            array.AddRange(logicalControlsContainer.LogicalControls);
         return array;
      }

      
      /// <summary> implement placement</summary>
      public void layout(Control containerControl, LayoutEventArgs ea)
      {
         if (!CanPerformLayout(containerControl))
            return;

         containerControl.SuspendLayout();

         Rectangle rectangleToSave, newRect = rectangleToSave = GetBounds(_mainComposite);

         FixRuntimeDesignerPlacement(ref newRect);

         Point actualPlacement = new Point(0, 0);
         int deltaX = 0, deltaY = 0;
         ArrayList controlsList;

         bool compositeMoved = newRect.Width - _prevRect.Width != 0 || newRect.Height - _prevRect.Height != 0;
         if (compositeMoved)
         {
            deltaX = newRect.Width - _prevRect.Width;
            deltaY = newRect.Height - _prevRect.Height;

            // we have horizontal scrollbar and it will remain after placement
            if ((_prevRect.Width < _prevLogicalSize.X && newRect.Width < _prevLogicalSize.X) || _prevLogicalSize.X < 0)
               if (containerControl is ScrollableControl && ((ScrollableControl)containerControl).AutoScroll)
                  deltaX = 0;

            if ((_prevRect.Height < _prevLogicalSize.Y && newRect.Height < _prevLogicalSize.Y) || _prevLogicalSize.Y < 0)
               // we have vertical scrollbar and it will remain after placement
               deltaY = 0;

            if (deltaX != 0 || deltaY != 0)
               actualPlacement = limitPlacement(containerControl, deltaX, deltaY, newRect);
            Debug.Assert(!(IsTabControl(containerControl)));

            if (!(containerControl is TableControl))
            {
               controlsList = GetControlsList(containerControl);

               // execute placement
               LogicalControlsContainer logicalControlsContainer = GetLogicalControlsContainer(containerControl);
               if (logicalControlsContainer != null)
                  logicalControlsContainer.addChange(actualPlacement);
               foreach (Object obj in controlsList)
               {
#if PocketPC
                  // No placement for dummy control
                  if (containerControl is Panel && obj == ((Panel)containerControl).dummy)
                     continue;
#endif
                  ControlPlacement(obj, actualPlacement);
               }

               if (logicalControlsContainer.LogicalControls != null)
                  containerControl.Invalidate();
            }
         }

         _prevRect = rectangleToSave;
         if (isRTL(containerControl))
            gui_upd_win_scroll_bars_hebrew(containerControl);
         computeAndUpdateLogicalSize(containerControl);
         containerControl.ResumeLayout();
      }

      protected virtual void FixRuntimeDesignerPlacement(ref Rectangle newRect)
      {
         
      }

      /// <summary>
      /// Returns if control is Tab Control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected virtual bool IsTabControl(Control control)
      {
         return (control is TabControl);
      }

      /// <summary>
      /// copied from  GUI::gui_upd_win_scroll_bars_hebrew in online QCR #978524
      /// when controls receive negative location (possible during hebrew placement)
      /// we must move all the control so they will have positive locations,
      /// like it is done in online
      /// </summary>
      /// <param name="containerControl"></param>
      void gui_upd_win_scroll_bars_hebrew(Control containerControl)
      {
         ArrayList controlsList = GetControlsList(containerControl);
         int leftDX = 0;
         foreach (var item in controlsList)
         {
            if (!ShouldApplyPlacement(item))
               continue;
            //check visible!!!
            Rectangle rect = Bounds(item);
            leftDX = Math.Min(rect.X, leftDX);
         }
         if (leftDX < 0)
         {
            LogicalControlsContainer logicalControlsContainer = GetLogicalControlsContainer(containerControl);
            logicalControlsContainer.NegativeOffset = leftDX;
            foreach (var item in controlsList)
            {
               if (!ShouldApplyPlacement(item))
                  continue;
               Control control = item as Control;
               if (control != null)
               {
#if PocketPC
                   // skip dummy control
                  if (containerControl is Panel && item == ((Panel)containerControl).dummy)
                     continue;
#endif
                  Rectangle rect = Bounds(control);
                  Rectangle? savedRect = GetSavedBounds(control);
                  if (savedRect != null)
                  {
                     rect.Width = ((Rectangle)savedRect).Width;
                     rect.Height = ((Rectangle)savedRect).Height;
                  }
                  rect.X -= leftDX;
                  SetBounds(control, rect);
               }
               else //logical control
               {
                  ReCalculateAndRefresh(item);
               }
            }
         }
      }
   }
}
