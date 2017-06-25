using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using com.magicsoftware.win32;

namespace com.magicsoftware.controls.designers
{

   /// <summary>
   /// this class is a replication of window's control designer
   /// It directly uses TabPage designer, and since we replaced in, we must replace this class as well
   /// </summary>
   public class MgTabControlDesigner : ParentControlDesigner
   {
      private bool addingOnInitialize;
      private bool disableDrawGrid;
      protected bool ForwardOnDrag;
      private int persistedSelectedIndex;
      private DesignerVerb removeVerb;
      protected bool TabControlSelected;
      private DesignerVerbCollection verbs;

      public override bool CanParent(Control control)
      {
         return ((control is TabPage) && !this.Control.Contains(control));
      }

      private void CheckVerbStatus()
      {
         if (this.removeVerb != null)
         {
            this.removeVerb.Enabled = ((MgTabControl)this.Control).TabCountExcludingDummyTab() > 0;
         }
      }

      protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize)
      {
         TabControl control = (TabControl)this.Control;
         if (control.SelectedTab == null)
         {
            throw new ArgumentException("TabControlInvalidTabPageType");
         }
         IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         if (service != null)
         {
            TabPageDesigner toInvoke = (TabPageDesigner)service.GetDesigner(control.SelectedTab);
            ParentControlDesigner.InvokeCreateTool(toInvoke, tool);
         }
         return null;
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
            if (service != null)
            {
               service.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
            }
            IComponentChangeService service2 = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
            if (service2 != null)
            {
               service2.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged);
            }
            TabControl control = this.Control as TabControl;
            if (control != null)
            {
               control.SelectedIndexChanged -= new EventHandler(this.OnTabSelectedIndexChanged);
               control.GotFocus -= new EventHandler(this.OnGotFocus);
               control.RightToLeftLayoutChanged -= new EventHandler(this.OnRightToLeftLayoutChanged);
               control.ControlAdded -= new ControlEventHandler(this.OnControlAdded);
            }
         }
         base.Dispose(disposing);
      }

      protected override bool GetHitTest(Point point)
      {
         TabControl control = (TabControl)this.Control;
         if (this.TabControlSelected)
         {
            Point pt = this.Control.PointToClient(point);
            return !control.DisplayRectangle.Contains(pt);
         }
         return false;
      }

      protected TabPageDesigner GetSelectedTabPageDesigner()
      {
         TabPageDesigner designer = null;
         TabPage selectedTab = ((TabControl)base.Component).SelectedTab;
         if (selectedTab != null)
         {
            IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));
            if (service != null)
            {
               designer = service.GetDesigner(selectedTab) as TabPageDesigner;
            }
         }
         return designer;
      }

      internal static TabPage GetTabPageOfComponent(object comp)
      {
         if (!(comp is Control))
         {
            return null;
         }
         Control parent = (Control)comp;
         while ((parent != null) && !(parent is TabPage))
         {
            parent = parent.Parent;
         }
         return (TabPage)parent;
      }

      public override void Initialize(IComponent component)
      {
         base.Initialize(component);
         base.AutoResizeHandles = true;
         TabControl control = component as TabControl;
         ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         if (service != null)
         {
            service.SelectionChanged += new EventHandler(this.OnSelectionChanged);
         }
         IComponentChangeService service2 = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
         if (service2 != null)
         {
            service2.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
         }
         if (control != null)
         {
            control.SelectedIndexChanged += new EventHandler(this.OnTabSelectedIndexChanged);
            control.GotFocus += new EventHandler(this.OnGotFocus);
            control.RightToLeftLayoutChanged += new EventHandler(this.OnRightToLeftLayoutChanged);
            control.ControlAdded += new ControlEventHandler(this.OnControlAdded);
         }
      }

      public override void InitializeNewComponent(IDictionary defaultValues)
      {
         base.InitializeNewComponent(defaultValues);
         try
         {
            this.addingOnInitialize = true;
         }
         finally
         {
            this.addingOnInitialize = false;
         }
         MemberDescriptor member = TypeDescriptor.GetProperties(base.Component)["Controls"];
         base.RaiseComponentChanging(member);
         base.RaiseComponentChanged(member, null, null);
         TabControl component = (TabControl)base.Component;
         if (component != null)
         {
            component.SelectedIndex = 0;
         }
      }

      private void OnAdd(object sender, EventArgs eevent)
      {
         MgTabControl component = (MgTabControl)base.Component;
         component.OnDesignerAddTabPage(this.addingOnInitialize);
      }

      private void OnComponentChanged(object sender, ComponentChangedEventArgs e)
      {
         this.CheckVerbStatus();
      }

      private void OnControlAdded(object sender, ControlEventArgs e)
      {
         if ((e.Control != null) && !e.Control.IsHandleCreated)
         {
            IntPtr handle = e.Control.Handle;
         }
      }

      protected override void OnMouseDragBegin(int x, int y)
      {
         if(Control.Controls.Count != 0)
            base.OnMouseDragBegin(x, y);
      }

      protected override void OnDragDrop(DragEventArgs de)
      {
         if (this.ForwardOnDrag)
         {
            TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
            if (selectedTabPageDesigner != null)
            {
               selectedTabPageDesigner.OnDragDropInternal(de);
            }
         }
         else
         {
            base.OnDragDrop(de);
         }
         this.ForwardOnDrag = false;
         ((MgTabControl)this.Control).IsDragging = false;
      }

      protected override void OnDragEnter(DragEventArgs de)
      {
         ((MgTabControl)this.Control).IsDragging = true;
         this.ForwardOnDrag = false;
         /*"System.Windows.Forms.Design.Behavior.DropSourceBehavior.BehaviorDataObject"*/
         // DropSourceBehavior.BehaviorDataObject data = de.Data as DropSourceBehavior.BehaviorDataObject;
         Object data = de.Data;
         if (data != null && ReflecionDesignHelper.IsAssignableFrom(data, "System.Windows.Forms.Design.Behavior.DropSourceBehavior+BehaviorDataObject", ReflecionDesignHelper.FormsAssembly))
         {
            int primaryControlIndex = -1;
            //ArrayList sortedDragControls = data.GetSortedDragControls(ref primaryControlIndex);
           

            //Use reflection
            Type type = ReflecionDesignHelper.GetType("System.Windows.Forms.Design.Behavior.DropSourceBehavior+BehaviorDataObject", ReflecionDesignHelper.FormsAssembly);
            System.Reflection.MethodInfo method = type.GetMethod("GetSortedDragControls", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] args = new object[1];
            args[0] =  primaryControlIndex ;
            ArrayList sortedDragControls = (ArrayList)method.Invoke(data, args);

            if (sortedDragControls != null)
            {
               for (int i = 0; i < sortedDragControls.Count; i++)
               {
                  if (!(sortedDragControls[i] is Control) || ((sortedDragControls[i] is Control) && !(sortedDragControls[i] is TabPage)))
                  {
                     this.ForwardOnDrag = true;
                     break;
                  }
               }
            }
         }
         else
         {
            this.ForwardOnDrag = true;
         }
         if (this.ForwardOnDrag)
         {
            TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
            if (selectedTabPageDesigner != null)
            {
               selectedTabPageDesigner.OnDragEnterInternal(de);
            }
         }
         else
         {
            base.OnDragEnter(de);
         }
      }

      protected override void OnDragLeave(EventArgs e)
      {
         if (this.ForwardOnDrag)
         {
            TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
            if (selectedTabPageDesigner != null)
            {
               selectedTabPageDesigner.OnDragLeaveInternal(e);
            }
         }
         else
         {
            base.OnDragLeave(e);
         }
         this.ForwardOnDrag = false;
         ((MgTabControl)this.Control).IsDragging = false;
      }

      protected override void OnDragOver(DragEventArgs de)
      {
         if (this.ForwardOnDrag)
         {
            TabControl control = (TabControl)this.Control;
            Point pt = this.Control.PointToClient(new Point(de.X, de.Y));
            if (!control.DisplayRectangle.Contains(pt))
            {
               de.Effect = DragDropEffects.None;
            }
            else
            {
               TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
               if (selectedTabPageDesigner != null)
               {
                  selectedTabPageDesigner.OnDragOverInternal(de);
               }
            }
         }
         else
         {
            base.OnDragOver(de);
         }
      }

      protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
      {
         if (this.ForwardOnDrag)
         {
            TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
            if (selectedTabPageDesigner != null)
            {
               selectedTabPageDesigner.OnGiveFeedbackInternal(e);
            }
         }
         else
         {
            base.OnGiveFeedback(e);
         }
      }

      private void OnGotFocus(object sender, EventArgs e)
      {
         //Use reflection
         Type eventHandlerService = ReflecionDesignHelper.GetType("System.Windows.Forms.Design.IEventHandlerService", ReflecionDesignHelper.DesignAssembly);
         object service = this.GetService(eventHandlerService);
         if (service != null)
         {
            Control focusWindow = (Control)ReflecionDesignHelper.InvokeProperty(service, "FocusWindow"); ;
            if (focusWindow != null)
            {
               focusWindow.Focus();
            }
         }
      }

      protected override void OnPaintAdornments(PaintEventArgs pe)
      {
         try
         {
            this.disableDrawGrid = true;
            base.OnPaintAdornments(pe);
         }
         finally
         {
            this.disableDrawGrid = false;
         }
      }

      private void OnRemove(object sender, EventArgs eevent)
      {
         MgTabControl component = (MgTabControl)base.Component;
         if ((component != null) && (component.TabPages.Count != 0))
         {
            component.OnDesignerRemoveTabPage(sender, eevent);
         }
      }

      private void OnRightToLeftLayoutChanged(object sender, EventArgs e)
      {
         if (base.BehaviorService != null)
         {
            base.BehaviorService.SyncSelection();
         }
      }

      private void OnSelectionChanged(object sender, EventArgs e)
      {
         ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         this.TabControlSelected = false;
         if (service != null)
         {
            ICollection selectedComponents = service.GetSelectedComponents();
            TabControl component = (TabControl)base.Component;
            foreach (object obj2 in selectedComponents)
            {
               if (obj2 == component)
               {
                  this.TabControlSelected = true;
               }
               TabPage tabPageOfComponent = GetTabPageOfComponent(obj2);
               if ((tabPageOfComponent != null) && (tabPageOfComponent.Parent == component))
               {
                  this.TabControlSelected = false;
                  component.SelectedTab = tabPageOfComponent;
                  //Use reflection
                  object selectionManager = this.GetService(ReflecionDesignHelper.GetType("System.Windows.Forms.Design.Behavior.SelectionManager", ReflecionDesignHelper.DesignAssembly));
                  ReflecionDesignHelper.InvokeMethod(selectionManager, "Refresh");
                  //((SelectionManager)this.GetService(typeof(SelectionManager))).Refresh();
                  break;
               }
            }
         }
      }

      protected virtual void OnTabSelectedIndexChanged(object sender, EventArgs e)
      {
         ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         if (service != null)
         {
            ICollection selectedComponents = service.GetSelectedComponents();
            TabControl component = (TabControl)base.Component;
            bool flag = false;
            foreach (object obj2 in selectedComponents)
            {
               TabPage tabPageOfComponent = GetTabPageOfComponent(obj2);
               if (((tabPageOfComponent != null) && (tabPageOfComponent.Parent == component)) && (tabPageOfComponent == component.SelectedTab))
               {
                  flag = true;
                  break;
               }
            }
            if (!flag)
            {
               service.SetSelectedComponents(new object[] { base.Component }, SelectionTypes.Replace);
            }
         }
      }

      protected override void PreFilterProperties(IDictionary properties)
      {
         base.PreFilterProperties(properties);
         string[] strArray = new string[] { "SelectedIndex" };
         Attribute[] attributes = new Attribute[0];
         for (int i = 0; i < strArray.Length; i++)
         {
            PropertyDescriptor oldPropertyDescriptor = (PropertyDescriptor)properties[strArray[i]];
            if (oldPropertyDescriptor != null)
            {
               properties[strArray[i]] = TypeDescriptor.CreateProperty(typeof(MgTabControlDesigner), oldPropertyDescriptor, attributes);
            }
         }
      }

      protected override void WndProc(ref Message m)
      {
         switch (m.Msg)
         {
            case 0x114:
            case 0x115:
               base.BehaviorService.Invalidate(base.BehaviorService.ControlRectInAdornerWindow(this.Control));
               base.WndProc(ref m);
               return;

            case 0x84:
               base.WndProc(ref m);
               if (((int)((long)m.Result)) != -1)
               {
                  break;
               }
               m.Result = (IntPtr)1;
               return;

            case 0x7b:
               {
                  int x = NativeWindowCommon.LoWord((int)((long)m.LParam));
                  int y = NativeWindowCommon.HiWord((int)((long)m.LParam));
                  if ((x == -1) && (y == -1))
                  {
                     Point position = Cursor.Position;
                     x = position.X;
                     y = position.Y;
                  }
                  this.OnContextMenu(x, y);
                  return;
               }
            default:
               base.WndProc(ref m);
               break;
         }
      }

      protected override bool AllowControlLasso
      {
         get
         {
            return false;
         }
      }

      protected override bool DrawGrid
      {
         get
         {
            if (this.disableDrawGrid)
            {
               return false;
            }
            return base.DrawGrid;
         }
      }

      public override bool ParticipatesWithSnapLines
      {
         get
         {
            if (!this.ForwardOnDrag)
            {
               return false;
            }
            TabPageDesigner selectedTabPageDesigner = this.GetSelectedTabPageDesigner();
            if (selectedTabPageDesigner != null)
            {
               return selectedTabPageDesigner.ParticipatesWithSnapLines;
            }
            return true;
         }
      }

      private int SelectedIndex
      {
         get
         {
            return this.persistedSelectedIndex;
         }
         set
         {
            this.persistedSelectedIndex = value;
         }
      }

      public override DesignerVerbCollection Verbs
      {
         get
         {
            if (this.verbs == null)
            {
               this.removeVerb = new DesignerVerb(Controls.Properties.Resources.RemoveTab_s, new EventHandler(this.OnRemove));
               this.verbs = new DesignerVerbCollection();
               this.verbs.Add(new DesignerVerb(Controls.Properties.Resources.AddTab_s, new EventHandler(this.OnAdd)));
               this.verbs.Add(this.removeVerb);
            }
            
            return this.verbs;
         }
      }
   }

}
