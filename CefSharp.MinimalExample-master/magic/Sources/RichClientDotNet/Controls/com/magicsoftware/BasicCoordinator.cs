using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Handles/Maintains bounds of object and handles placement information
   /// </summary>
   public class BasicCoordinator : ICoordinator
   {
      protected readonly PlacementDrivenLogicalControl _lg;
      protected readonly Control _containerControl;
      protected readonly LogicalControlsContainer _logicalControlsContainer;
      private bool _refreshNeeded;
      private int _height;
      private int _width;
      private int _x;
      private int _y;

      public Rectangle DisplayRect { get; set; } // display rectangle of the control, used for invalidation

      public BasicCoordinator(PlacementDrivenLogicalControl logicalControl, LogicalControlsContainer logicalControlsContainer)
      {
         _lg = logicalControl;
         _logicalControlsContainer = logicalControlsContainer;
         _containerControl = logicalControlsContainer.mainControl;
         _logicalControlsContainer.Add(logicalControl);
         _lg.VisibleChanged += lg_VisibleChanged;
      }

      /// <summary>
      ///   occur when visibility is changed
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      private void lg_VisibleChanged(object sender, EventArgs e)
      {
         OnVisibleChanged(sender, e);
      }

      protected virtual void OnVisibleChanged(object sender, EventArgs e)
      {
         if (_containerControl is ScrollableControl)
         {
            //update parent's scrollbar 

#if !PocketPC
            // layout will calculate new AutoScrollMinSize
            _containerControl.PerformLayout(_containerControl, "Visible");
#else
            // TODO: Ashish: Handle TableControl (not needed)
            //if (_containerControl is TableControl)
            //{
            //   ((TableControl)_containerControl).PerformLayout();
            //   ((TableControl)_containerControl).PerformLayout();
            //}
#endif
         }
      }

      #region ICoordinator Members

      int ICoordinator.X
      {
         get { return _x; }
         set
         {
            bool changed = _x != value;
            _x = value;
            OnBounds(changed);
         }
      }

      int ICoordinator.Y
      {
         get { return _y; }
         set
         {
            bool changed = _y != value;
            _y = value;
            OnBounds(changed);
         }
      }

      int ICoordinator.Height
      {
         get { return _height; }
         set
         {
            bool changed = _height != value;
            _height = value;
            OnBounds(changed);
         }
      }

      int ICoordinator.Width
      {
         get { return _width; }
         set
         {
            bool changed = _width != value;
            _width = value;
            OnBounds(changed);
         }
      }


      /// <summary>
      ///   return difference caused by placement
      /// </summary>
      /// <param name = "placementDim"></param>
      /// <returns></returns>
      public int getPlacementDif(PlacementDim placementDim)
      {
         int result = 0;
#if !PocketPC
         //tmp
         bool containerRightToLeft = _containerControl.RightToLeft == RightToLeft.Yes;
#else
         bool containerRightToLeft = false;
#endif
         switch (placementDim)
         {
            case PlacementDim.PLACE_X:
               result = _logicalControlsContainer.SizeChange.X *
                        _lg.PlacementData.getPlacement(placementDim, containerRightToLeft) / 100;
               result -= _logicalControlsContainer.NegativeOffset;
               break;
            case PlacementDim.PLACE_DX:
               result = _logicalControlsContainer.SizeChange.X *
                        _lg.PlacementData.getPlacement(placementDim, containerRightToLeft) / 100;
               break;
            case PlacementDim.PLACE_Y:
            case PlacementDim.PLACE_DY:
               result = _logicalControlsContainer.SizeChange.Y *
                        _lg.PlacementData.getPlacement(placementDim, containerRightToLeft) / 100;
               break;
            default:
               break;
         }


         return result;
      }


      public virtual void Refresh(bool changed)
      {

      }

      public virtual void Refresh(bool changed, bool wholeParent)
      {
      }
      public bool RefreshNeeded
      {
         get
         {
            return _refreshNeeded;
         }
         set
         {
            _refreshNeeded = value;
         }
      }

      Rectangle ICoordinator.getRectangle()
      {
         return DisplayRect;
      }

      #endregion

      /// <summary>
      ///   calculate display rectangle
      /// </summary>
      public virtual void calcDisplayRect()
      {
         if (_lg is ILine)
            ((ILine)_lg).LineHelper.calcDisplayRect(((ILine)_lg).LineHelper.calcLineRectangle());
         else
         {
            Rectangle rect = new Rectangle(_x, _y, _width, _height);
#if !PocketPC
            //tmp
            bool containerRightToLeft = _containerControl.RightToLeft == RightToLeft.Yes;
#else
            bool containerRightToLeft = false;
#endif
            // add placement change placement for all children
            rect.X += getPlacementDif(PlacementDim.PLACE_X);
            rect.Y += getPlacementDif(PlacementDim.PLACE_Y);
            rect.Height += getPlacementDif(PlacementDim.PLACE_DY);
            int dx = getPlacementDif(PlacementDim.PLACE_DX);
            rect.Width += dx;
            if (containerRightToLeft)
               rect.X -= dx;
            DisplayRect = rect;
         }
      }

      /// <summary> Refreshes the coordinator and its parent when
      /// the bounds of the coordinator changes. </summary>
      /// <param name="changed"></param>
      protected virtual void OnBounds(bool changed)
      {
         if (changed)
         {
            Refresh(true, true);
            calcDisplayRect();
#if !PocketPC


            //If the coordinator's bounds changes, it might lead to 
            //showing or hiding of the scrollbar on its parent. 
            //So, perform the layout to its parent to refresh it.
            if (changed && _containerControl is ScrollableControl)
            {


               //we call Panel.PerformLayout() so that Layout event will be fired and from there, the scrollbar will be re-evaluated.
               //While handling the Layout event, we call PlacementLayout.layout(), which in-turn calls updateAutoScrollMinSize() 
               //(via computeAndUpdateLogicalSize()).
               //Now, updateAutoScrollMinSize() actually sets the Panel.AutoScrollMinSize
               //Perform layout must have active control attribute to effect the scrollbar immideatly  
               _containerControl.PerformLayout(_containerControl, "");
            }
#endif
         }
      }

   }
}
