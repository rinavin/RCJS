using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design.Behavior;
using System.ComponentModel.Design;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Handle the drag and drop on the container control
   /// </summary>
   class DragDropHandler
   {
      protected List<IComponent> components = new List<IComponent>(); //list of components 
      protected Control control;
      protected BehaviorService behaviorService;
      protected bool isDroppedItemToolBoxItem = false;
      protected List<Point> controlLocations = new List<Point>();

      IDesignerHost designerHost;
      protected DesignerTransaction transaction;

      public DragDropHandler(Control control, BehaviorService behaviorService, IDesignerHost designerHost)
      {
         this.control = control;
         this.behaviorService = behaviorService;
         this.designerHost = designerHost;
         control.ControlAdded += new ControlEventHandler(control_ControlAdded);
      }

      /// <summary>
      /// handle the added event to add the component in the List
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void control_ControlAdded(object sender, ControlEventArgs e)
      {
         components.Add(e.Control);
      }

      /// <summary>
      /// clear the list before actual drag drop
      /// </summary>
      internal void BeforeDragDrop()
      {
         transaction = designerHost.CreateTransaction("dropping");
         components.Clear();
      }

      /// <summary>
      /// handle the dragdrop 
      /// </summary>
      internal virtual void AfterDragDrop(DragEventArgs de, bool isDroppedFromToolbox)
      {
         List<Point> newLocations = new List<Point>();
         foreach (var c in components)
            newLocations.Add(GetNewLocation((Control)c)); // Get the location of the control in screen points

         IMgContainer mgContainer = null;
         if (control is IMgContainer)
            mgContainer = (IMgContainer)control;
         else
            mgContainer = (IMgContainer)control.Parent;

         SetParent();

         mgContainer.OnComponentDropped(new ComponentDroppedArgs() { Components = components, Locations = newLocations, ControlLocations = controlLocations, IsDroppedFromToolbox = isDroppedFromToolbox });

         transaction.Commit();
      }

      /// <summary>
      /// get the new location of the control
      /// </summary>
      /// <returns></returns>
      protected virtual Point GetNewLocation(Control c)
      {
         // Get the location of the control in screen points
         System.Drawing.Rectangle rect = this.behaviorService.ControlRectInAdornerWindow(c);
         return this.behaviorService.AdornerWindowPointToScreen(new Point(rect.X, rect.Y));
      }

      /// <summary>
      /// Set the parentof added components
      /// </summary>
      protected virtual void SetParent()
      {

      }
   }
}
