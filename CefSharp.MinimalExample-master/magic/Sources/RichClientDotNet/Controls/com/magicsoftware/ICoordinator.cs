using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// interface for coordination between logical control and Container managers
   /// 
   /// </summary>
   public interface ICoordinator : IRefreshable
   {
      //bounds properties/methods
      int X { get; set; }
      int Y { get; set; }
      int Width { get; set; }
      int Height { get; set; }

      /// <summary>
      /// returns rectangle of the control according to its container
      /// </summary>
      /// <returns></returns>
      Rectangle getRectangle();

      /// <summary>
      /// returns pacement dx or dy according to the axe
      /// </summary>
      /// <param name="placementDim"></param>
      /// <returns></returns>
      int getPlacementDif(PlacementDim placementDim);

   }
}
