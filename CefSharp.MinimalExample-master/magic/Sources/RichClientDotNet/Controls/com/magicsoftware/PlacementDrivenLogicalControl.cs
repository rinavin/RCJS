using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Logical control which knows placement information of it.
   /// </summary>
   public class PlacementDrivenLogicalControl : ICoordinator
   {
      public int _mgRow { get; set; }

      public int _mgColumn { get; set; }

      public PlacementData PlacementData { get; set; }

      protected ICoordinator _coordinator; //coordinator is responsible for coordination between
      public ICoordinator Coordinator
      {
         get { return _coordinator; }
         set { _coordinator = value; }
      }

      public int X
      {
         get { return _coordinator.X; }
         set { _coordinator.X = value; }
      }

      /// <summary>
      ///   Y coordinate
      /// </summary>
      public int Y
      {
         get { return _coordinator.Y; }
         set { _coordinator.Y = value; }
      }

      /// <summary>
      ///   Height
      /// </summary>
      public int Height
      {
         get { return _coordinator.Height; }
         set { _coordinator.Height = value; }
      }

      /// <summary>
      ///   Width
      /// </summary>
      public virtual int Width
      {
         get { return _coordinator.Width; }
         set { _coordinator.Width = value; }
      }

      /// <summary>
      /// Controls Z Order
      /// </summary>
      public int ZOrder { get; set; }

      public virtual Point GetLeftTop() { return new Point(X, Y); }
      public virtual Point GetRightBottom() { return new Point(X + Width, Y + Height); }

      public virtual Rectangle getRectangle()
      {
         return _coordinator.getRectangle();
      }

      /// <summary>
      ///   thue if the control should be refreshed on next refresh
      /// </summary>
      public bool RefreshNeeded
      {
         get { return _coordinator.RefreshNeeded; }
         set { _coordinator.RefreshNeeded = value; }
      }

      /// <summary>
      /// refresh the logical control
      /// </summary>
      /// <param name="changed"></param>
      public virtual void Refresh(bool changed)
      {
         if (_coordinator != null)
            _coordinator.Refresh(changed);
      }


      private bool _visible;
      public virtual bool Visible
      {
         get { return _visible; }
         set
         {
            bool changed = (_visible != value);
            _visible = value;
            if (changed && VisibleChanged != null)
               VisibleChanged(this, new EventArgs());
            else
               Refresh(changed);
         }
      }

      // event is raised when logical control's visibility is changed
      public event EventHandler VisibleChanged;

      public int getPlacementDif(PlacementDim placementDim)
      {
         return _coordinator.getPlacementDif(placementDim);
      }


   }
}
