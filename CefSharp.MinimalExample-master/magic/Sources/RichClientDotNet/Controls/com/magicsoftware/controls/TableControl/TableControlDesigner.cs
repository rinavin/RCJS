using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;
using com.magicsoftware.controls.designers;
using com.magicsoftware.controls.utils;
using com.magicsoftware.Glyphs;
using com.magicsoftware.util.notifyCollection;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// designer for table control
   /// </summary>
   public class TableControlDesigner : ParentControlDesigner
   {

      #region fields
      private TableControl tableControl;
   
      DragDropHandler dragDropHandler;

      #endregion

      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);

         // #297589. Do not show the grids on table control. 
         BindingFlags bindingAttrs = BindingFlags.Instance | BindingFlags.NonPublic;
         Type classType = typeof(ParentControlDesigner);
         MemberInfo[] memberinfos = classType.GetMember("DrawGrid", bindingAttrs);
         PropertyInfo propInfo = memberinfos[0] as PropertyInfo;
         propInfo.SetValue(this, false, null);

         // Record instance of control we're designing
         tableControl = (TableControl)component;

         // Hook up events
         //ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));
         IComponentChangeService c = (IComponentChangeService)GetService(typeof(IComponentChangeService));
         canParentProvider = new CanParentProvider(this);

         dragDropHandler = new TableControlDragDropHandler((Control)component, this.BehaviorService, (IDesignerHost)this.GetService(typeof(IDesignerHost)));

      }

   


      public override bool ParticipatesWithSnapLines
      {
         get
         {
            return false;
         }
      }

      /// <summary>
      /// Select the attached controls of Table
      /// </summary>
      public void SelectAttachedControls()
      {
         ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         if (service != null)
         {
            ICollection selectedItems = service.GetSelectedComponents();

            foreach (Component item in selectedItems)
               if (item.Equals(tableControl.Parent))
                  service.SetSelectedComponents(new object[] { item }, SelectionTypes.Remove);
            service.SetSelectedComponents(base.AssociatedComponents, SelectionTypes.Add);
         }
      }


      protected override bool GetHitTest(System.Drawing.Point point)
      {
         bool result = base.GetHitTest(point);
         point = tableControl.PointToClient(point);
         TableHitTestResult hitTestResult = tableControl.HitTest(point);
         if (hitTestResult.Area == TableHitTestArea.OnHeader ||
            hitTestResult.Area == TableHitTestArea.OnNonClientArea)
            result = true;
         return result;
      }

     
      //Label label = new Label() { BackColor = Color.SeaGreen, BorderStyle = BorderStyle.FixedSingle};
      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de, true))
         {
            tableControl.OnDesignerDragEnter(de);
            base.OnDragEnter(de);
         }
      }
      protected override void OnDragOver(DragEventArgs de)
      {
         base.OnDragOver(de);
         tableControl.OnDesignerDragOver(de);
         //tableConrolDesignerDrag.OnDragOver( tableControl.PointToClient(new Point(de.X, de.Y)));
         
      }

      protected override void OnDragLeave(EventArgs e)
      {
         //tableConrolDesignerDrag.OnDragStop();
         tableControl.OnDesignerDragStop();
         //ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         //if (service != null)
         //{
         //   SelectionTypes selectionType = SelectionTypes.Auto;
         //   service.SetSelectedComponents(new object[] {tableControl.Parent }, selectionType);

         //   return;
         //}

         base.OnDragLeave(e);
      }


      protected override void OnDragComplete(DragEventArgs de)
      {
         tableControl.OnDesignerDragStop();
         base.OnDragComplete(de);
         //tableConrolDesignerDrag.OnDragStop();
      }

      protected override void OnDragDrop(System.Windows.Forms.DragEventArgs de)
      {
         tableControl.OnDesignerDragStop();
         //tableConrolDesignerDrag.OnDragStop();

         dragDropHandler.BeforeDragDrop();

         bool isDroppedFromToolbox = Utils.GetIsDroppedFromToolbox(this);

         tableControl.InDragDrop = true;
         base.OnDragDrop(de);
         tableControl.InDragDrop = false;

         dragDropHandler.AfterDragDrop(de, isDroppedFromToolbox);
      }

      #region CanParent

      CanParentProvider canParentProvider;

      private static bool ModifierPressed(Keys key)
      {
         return (Control.ModifierKeys & key) == key;
      }


      protected override void OnMouseDragBegin(int x, int y)
      {
         if (canParentProvider.CanDropFromSelectedToolboxItem(null, true))
         {
            if (ModifierPressed(Keys.Alt))
            {
               //select column
               Point pt = tableControl.PointToClient(new Point(x, y));
               TableHitTestResult result = tableControl.HitTest(pt);
               if (result.Area == TableHitTestArea.OnColumn && result.TableColumn != null)
               {
                  ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
                  if (service != null)
                  {
                     SelectionTypes selectionType =  SelectionTypes.Auto;
                     service.SetSelectedComponents(new object[] { result.TableColumn }, selectionType);

                     return;
                  }
               }
            }
            if (ModifierPressed(Keys.Shift))
            {
               SelectAttachedControls();
               return;
            }

            base.OnMouseDragBegin(x, y);
         }
      }

      protected override void OnMouseDragEnd(bool cancel)
      {
        // we get here on column selection
         if (!ModifierPressed(Keys.Alt))
            base.OnMouseDragEnd(cancel);
      }


      
     
      #endregion
   }
}



