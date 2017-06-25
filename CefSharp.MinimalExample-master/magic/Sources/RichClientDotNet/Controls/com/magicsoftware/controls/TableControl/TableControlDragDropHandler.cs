using com.magicsoftware.controls.designers;
using System.Windows.Forms;
using System.Windows.Forms.Design.Behavior;
using System.Drawing;
using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.ComponentModel.Design;

namespace com.magicsoftware.controls
{
   class TableControlDragDropHandler : DragDropHandler
   {
      Point dragPoint = Point.Empty;

      public TableControlDragDropHandler(Control control, BehaviorService behaviorService, IDesignerHost designerHost)
         : base(control, behaviorService, designerHost)
      {

      }

      internal override void AfterDragDrop(DragEventArgs de, bool isDroppedFromToolbox)
      {
         dragPoint = new Point(de.X, de.Y);

         if (components.Count == 0)
            SaveDraggedComponents(de);

         controlLocations.Clear();
         foreach (var c in components)
         {
            controlLocations.Add(GetControlLocation((Control)c));
         }

         base.AfterDragDrop(de, isDroppedFromToolbox);

         dragPoint = Point.Empty;
      }

      /// <summary>
      /// return an blank point as the location of the control does not depend on control but on drag point
      /// </summary>
      /// <param name="c"></param>
      /// <returns></returns>
      protected override Point GetNewLocation(Control c)
      {
         return dragPoint;
      }

      /// <summary>
      /// Get Location of control dragged on Table
      /// </summary>
      /// <param name="c"></param>
      /// <returns></returns>
      public Point GetControlLocation(Control c)
      {
         return base.GetNewLocation(c);
      }

      /// <summary>
      /// save DragComponents in the components list
      /// </summary>
      /// <param name="de"></param>
      public void SaveDraggedComponents(DragEventArgs de)
      {
         if ((Control.ModifierKeys & Keys.Control) != 0)
            return;

         //BehaviorDataObject is internal class in .NET
         // use this work around to get dragged objects
         //http://vbcity.com/forums/t/163927.aspx

         Type t = de.Data.GetType();
         if (t.Name == "BehaviorDataObject")
         {
            PropertyInfo pi = t.GetProperty("DragComponents");
            ArrayList comps = pi.GetValue(de.Data, null) as ArrayList;
            if (comps != null && comps.Count > 0)
            {
               foreach (Object item in comps)
               {
                  if (item is IComponent)
                     components.Add((IComponent)item);
               }
            }
         }
      }
   }
}
