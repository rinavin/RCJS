using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;

namespace Controls.com.magicsoftware
{
   public enum PlacementDim
   {
      PLACE_X,
      PLACE_DX,
      PLACE_Y,
      PLACE_DY
   }

   public enum Axe
   {
      X,
      Y
   }

   /// <summary>controls placement data</summary>
   /// <author> rinav </author>
   public class PlacementData
   {
      public Rectangle Placement { get; private set; }
      private float _accCtrlMoveDx; // keeps track of ctrl movement when the ctrl placement is less than 100
      private float _accCtrlMoveX;  // keeps track of ctrl movement when the ctrl placement is less than 100
      private float _accCtrlMoveDy; // keeps track of ctrl movement when the ctrl placement is less than 100
      private float _accCtrlMoveY;  // keeps track of ctrl movement when the ctrl placement is less than 100

      public PlacementData(Rectangle placement)
      {
         Placement = placement;
      }

      /**
  * return saved  accCtrlMove according the dimention placement value.
  * @param placementDim
  * @return
  */
      public float getAccCtrlMove(PlacementDim placementDim)
      {
         if (placementDim == PlacementDim.PLACE_DX)
            return _accCtrlMoveDx;
         else if (placementDim == PlacementDim.PLACE_X)
            return _accCtrlMoveX;
         else if (placementDim == PlacementDim.PLACE_DY)
            return _accCtrlMoveDy;
         else if (placementDim == PlacementDim.PLACE_Y)
            return _accCtrlMoveY;
         else
         {
            Debug.Assert(false);
            return 0;
         }
      }

      /**
       *  set accCtrlMove according the dimention placement value.
       * @param placementDim
       * @param accCtrlMove
       */
      public void setAccCtrlMove(PlacementDim placementDim, float accCtrlMove)
      {
         if (placementDim == PlacementDim.PLACE_DX)
            _accCtrlMoveDx = accCtrlMove;
         else if (placementDim == PlacementDim.PLACE_X)
            _accCtrlMoveX = accCtrlMove;
         else if (placementDim == PlacementDim.PLACE_DY)
            _accCtrlMoveDy = accCtrlMove;
         else if (placementDim == PlacementDim.PLACE_Y)
            _accCtrlMoveY = accCtrlMove;
         else
         {
            Debug.Assert(false);
         }
      }

      /**
       * get accCtrlMove according the dimention placement value.
       * @param placementDim
       * 
       * @return
       */

      public int getPlacement(PlacementDim placementDim, bool containerRightToLeft)
      {
         if (placementDim == PlacementDim.PLACE_DX)
            return Placement.Width;
         else if (placementDim == PlacementDim.PLACE_X)
            return (containerRightToLeft ? 100 - Placement.X : Placement.X);
         else if (placementDim == PlacementDim.PLACE_DY)
            return Placement.Height;
         else if (placementDim == PlacementDim.PLACE_Y)
            return Placement.Y;
         else
         {
            Debug.Assert(false);
            return 0;
         }
      }
   }
}
