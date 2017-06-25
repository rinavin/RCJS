using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.controls.designers;
using com.magicsoftware.support;
using com.magicsoftware.util;
using RuntimeDesigner.Serialization;
using Controls.com.magicsoftware;
using RuntimeDesigner.RuntimeDesignerStrategies;

namespace RuntimeDesigner
{
   delegate void ControlsDeletedDelegate(object sender, ControlsDeletedArgs canResizeArgs);

   /// <summary>
   /// Inherits from DesignSurface and hosts the RootComponent and 
   /// all other designers. It also uses loaders (BasicDesignerLoader
   /// or CodeDomDesignerLoader) when required. It also provides various
   /// services to the designers. Adds MenuCommandService which is used
   /// for Cut, Copy, Paste, etc.
   /// </summary>
   public class RuntimeHostSurface : DesignSurface
   {
      private BasicDesignerLoader _loader;

      PropertyGridManager propertyGridManager;

      ISelectionService selectionService;

      IDesignerHost host;

      RuntimeMenuService runtimeMenuService;

      internal bool AdminMode { get; set; }

      /// <summary>
      /// dictionary of components and the wrapper created for them.
      /// May be needed to be somewhere else - for serialization mainly
      /// </summary>
      Dictionary<object, ComponentWrapper> componentsDictionary = new Dictionary<object, ComponentWrapper>();
      public Dictionary<object, ComponentWrapper> ComponentsDictionary { get { return componentsDictionary; } }


      /// <summary>
      /// handles runtime designer
      /// </summary>
      RuntimeDesignerHandleState runtimeDesignerHandleState = new RuntimeDesignerHandleState();
      internal RuntimeDesignerHandleState RuntimeDesignerHandleState { get { return runtimeDesignerHandleState; } }

      /// <summary>
      /// dictionary of isn and the controls for them.
      /// May be needed to be somewhere else - for visibility of controls
      /// </summary>
      Dictionary<int, Control> controlDictionary = new Dictionary<int, Control>();
      public Dictionary<int, Control> ControlDictionary { get { return controlDictionary; } }

      internal delegate void SetValuesFromPropertiesDelegate(Control control, Dictionary<string, DesignerPropertyInfo> properties);

      /// <summary>
      /// dictionary to enable setting of value is special cases
      /// </summary>
      internal Dictionary<MgControlType, SetValuesFromPropertiesDelegate> RelatedPropertiesHandlingDictionary;

      /// <summary>
      /// 
      /// </summary>
      internal RuntimeHostSurface()
         : base()
      {
         this.AddService(typeof(IMenuCommandService), new RuntimeMenuService(this));
      }

      internal RuntimeHostSurface(IServiceProvider parentProvider, ITranslate translate)
         : base(parentProvider)
      {
         AddService(typeof(ITranslate), translate);
         runtimeMenuService = new RuntimeMenuService(this);
         AddService(typeof(IMenuCommandService), runtimeMenuService);
         InitPropHandlingDictionary();
      }

      /// <summary>
      /// init the dictionary for control-properties special treatment
      /// </summary>
      void InitPropHandlingDictionary()
      {
         RelatedPropertiesHandlingDictionary = new Dictionary<MgControlType, SetValuesFromPropertiesDelegate>()
         {
            { MgControlType.CTRL_TYPE_BUTTON, SetButtonValuesFromProperties },
            { MgControlType.CTRL_TYPE_RADIO, SetRadioButtonValuesFromProperties },
         };
      }

      internal void Initialize()
      {

         Control control = null;
         host = (IDesignerHost)this.GetService(typeof(IDesignerHost));

         if (host == null)
            return;

         try
         {
            // Set the backcolor
            Type hostType = host.RootComponent.GetType();
            control = this.View as Control;
            control.BackColor = Color.White;

            // propertygrid
            selectionService = (ISelectionService)(this.ServiceContainer.GetService(typeof(ISelectionService)));
            PropertyGrid propertyGrid = (PropertyGrid)this.GetService(typeof(PropertyGrid));
            propertyGridManager = new PropertyGridManager(propertyGrid, selectionService, host, this);
            propertyGrid.SelectedObject = this.ComponentsDictionary[host.RootComponent];
         }
         catch (Exception ex)
         {
            Trace.WriteLine(ex.ToString());
         }
      }

      internal BasicDesignerLoader Loader
      {
         get
         {
            return _loader;
         }
         set
         {
            _loader = value;
         }
      }

      internal void AddService(Type type, object serviceInstance)
      {
         this.ServiceContainer.AddService(type, serviceInstance);
      }

      /// <summary>
      /// process keyboard events on the designer
      /// </summary>
      /// <param name="e"></param>
      internal void HandleKeyDown(PreviewKeyDownEventArgs e)
      {
         Action<Control> action = null;

         switch (e.KeyCode)
         {
            // Arrow keys - set the property to be changed and the direction of move
            case Keys.Left:
               if (e.Shift)
                  action = new Action<Control>((control) => { control.Width--; });
               else
                  action = new Action<Control>((control) => { control.Left--; });
               break;

            case Keys.Right:
               if (e.Shift)
                  action = new Action<Control>((control) => { control.Width++; });
               else
                  action = new Action<Control>((control) => { control.Left++; });
               break;

            case Keys.Up:
               if (e.Shift)
                  action = new Action<Control>((control) => { control.Height--; });
               else
                  action = new Action<Control>((control) => { control.Top--; });
               break;

            case Keys.Down:
               if (e.Shift)
                  action = new Action<Control>((control) => { control.Height++; });
               else
                  action = new Action<Control>((control) => { control.Top++; });
               break;

            case Keys.Delete:
               if (AdminMode)
               {
                  if (CanDeleteSelectedItems())
                     DeleteSelectedControls();
               }
               return;
         }

         if (action != null)
            ForEachSelectedComponent(action);
      }

      /// <summary>
      /// perform the specified action on all selected controls
      /// </summary>
      /// <param name="action"></param>
      void ForEachSelectedComponent(Action<Control> action)
      {
         DesignerTransaction transaction = host.CreateTransaction("move by keyboard");

         ICollection selectedItems = selectionService.GetSelectedComponents();

         foreach (Control item in selectedItems)
            if (!(item is Form))
               action(item);

         transaction.Commit();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal static void LocationChanged(object sender, EventArgs e)
      {
         Control control = sender as Control;
         
         LocationTranslator locationTranslator = new LocationTranslator(control, Axe.X);
         int x = locationTranslator.EnsureValueIsLegal(control.Location.X);

         locationTranslator = new LocationTranslator(control, Axe.Y);
         int y = locationTranslator.EnsureValueIsLegal(control.Location.Y);

         control.Location = new Point(x, y);
      }
      
      internal static void SizeChanged(object sender, EventArgs e)
      {
         Control control = sender as Control;
         if ((control is MgFlexiHeightComboBox) && (((MgFlexiHeightComboBox)control).MgComboBox.DrawMode == DrawMode.OwnerDrawFixed))
         {
            ((MgFlexiHeightComboBox)control).MgComboBox.SetItemHeight(control.Height);
            ((MgFlexiHeightComboBox)control).Height = control.Height;
         }
      }

      /// <summary>
      /// Can a control be reparented
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="allowDragDropArgs"></param>
      /// <returns></returns>
      static internal bool CanParent(object sender, CanParentArgs allowDragDropArgs)
      {
         // if it is in the same container - allow
         if (allowDragDropArgs.ChildControl.Parent == sender)
            return true;

         // if it is in the same tab control - allow
         if (allowDragDropArgs.ChildControl.Parent.Parent is MgTabPage && sender is MgTabPage &&
            allowDragDropArgs.ChildControl.Parent.Parent.Parent == ((Control)sender).Parent)
            return true;

         // else - deny
         return false;
      }

      /// <summary>
      /// components dropped on tab page panel - set the layer
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal void MgTabControl_ComponentDropped(object sender, ComponentDroppedArgs e)
      {
         int layer = GetCurrentTabControlLayer((MgTabControl)((Control)sender).Parent.Parent);

         foreach (Object item in e.Components)
         {
            if (item is Control)
            {
               ComponentWrapper cw = ComponentsDictionary[item];
               cw.PropertiesDescriptors[Constants.WinPropLayer].SetValue(cw, layer);
            }
         }
      }

      /// <summary>
      /// get the current layer of the tab control, according to the current tab page and the visible layer list property.
      /// The value returned is the 1-based property value, not the 0-based tab index 
      /// </summary>
      /// <param name="tabControl"></param>
      /// <returns></returns>
      int GetCurrentTabControlLayer(MgTabControl tabControl)
      {
         MgArrayList visibleLayers = (MgArrayList)componentsDictionary[tabControl].GetProperties()[Constants.WinPropVisibleLayerList].GetValue(tabControl);
         int currentTabPage = tabControl.SelectedIndex;

         // no visible layers property - return current tab page layer. Add 1 since the layers are 1-based, and tabpage index is 0-based
         if (visibleLayers.Count == 0 || ((string[])visibleLayers[0]).Length == 0)
            return currentTabPage + 1;

         string currentTabPageLayerString = ((string[])visibleLayers[0])[currentTabPage];

         return Int32.Parse(currentTabPageLayerString);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal void SelectedIndexChanged(object sender, EventArgs e)
      {
         HandleVisibility(sender);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal static void MgPanel_Paint(object sender, PaintEventArgs e)
      {
         Control ctrl = sender as MgPanel;
        // for mgpanel with parent MgTabPage set paint event
         if (ctrl is MgPanel && ctrl.Parent is MgTabPage)
         {
            MgPanel senderMgPanel = (MgPanel)sender;
            ControlRenderer.FillRectAccordingToGradientStyle(((PaintEventArgs)e).Graphics, senderMgPanel.ClientRectangle, senderMgPanel.BackColor,
                                                                  senderMgPanel.ForeColor, ControlStyle.NoBorder, false, senderMgPanel.GradientColor,
                                                                  senderMgPanel.GradientStyle);
         }
         else
         {
            ControlRenderer.PaintMgPanel(ctrl, e.Graphics);
         }
      }


      /// <summary>
      /// while selected index is changed we need tp reorder the visibility of the controls
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal void HandleVisibility(object sender)
      {
         Control control = sender as Control;

         if (control != null)
         {
            int visibleLayer = 0;
            if (sender is IChoiceControl)
            {
               IChoiceControl choiceControl = sender as IChoiceControl;
               if (sender is MgTabControl)
               {
                  SetVisibilityForTabPage(((MgTabControl)sender));
                  visibleLayer = GetCurrentTabControlLayer((MgTabControl)sender) - 1;
               }
               else
                  visibleLayer = choiceControl.SelectedIndex;
            }

            SetChildrenVisibilityForControl(control, visibleLayer);
         }
      }

      /// <summary>
      /// set child visibility for choice control
      /// </summary>
      /// <param name="parentControl"></param>
      private void SetChildrenVisibilityForControl(Control parentControl, int currentVisiableLayer)
      {
         ControlDesignerInfo controlDesignerInfo = parentControl.Tag as ControlDesignerInfo;

         if (controlDesignerInfo != null && controlDesignerInfo.LinkedIds != null)
         {
            foreach (int controlId in controlDesignerInfo.LinkedIds)
            {
               if (ControlDictionary.ContainsKey(controlId))
               {
                  Control linkedControl = ControlDictionary[controlId];

                  if (ComponentsDictionary.ContainsKey(linkedControl))
                  {
                     int valueLayer = (int)ComponentsDictionary[linkedControl].PropertiesDescriptors[Constants.WinPropLayer].GetValue(linkedControl);

                     if (parentControl.Visible)
                     {
                        PropertyDescriptor prop = ComponentsDictionary[linkedControl].PropertiesDescriptors[Constants.WinPropVisible];
                        bool visible = prop == null ? true : (bool)prop.GetValue(linkedControl);
                        linkedControl.Visible = (valueLayer == 0 || valueLayer == currentVisiableLayer + 1) && visible;
                     }
                     else
                        linkedControl.Visible = false;

                     HandleVisibility(linkedControl);
                  }
               }
               else
                  MessageBox.Show("try to use control that didn't created", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
         }

      }
      /// <summary>
      /// ReorderVisibility
      /// </summary>
      /// <param name="tab"></param>
      internal static void SetVisibilityForTabPage(MgTabControl tab)
      {
         MgPanel mgPanel = GetMgPanel(tab);

         if (mgPanel == null)
            return;

         // move the mg panel to be child of the new selected tab page
         tab.SelectedTab.Controls.Add(mgPanel);
      }

      /// <summary>
      /// check if the control has a hidden ancestor
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal bool IsControlAncestorHidden(Control control)
      {
         while (true)
         {
            // parent ID
            int parentId = ((ControlDesignerInfo)control.Tag).ParentId;
            // parent control
            Control parent = this.ControlDictionary[parentId];
            // parent component wrapper
            ComponentWrapper cw;
            if (this.ComponentsDictionary.TryGetValue(parent, out cw))
            {

               // parent component visible property descriptor
               PropertyDescriptor prop = cw.PropertiesDescriptors[Constants.WinPropVisible];

               if (prop == null)
                  // parent control can not be hidden
                  return false;

               if (!(bool)prop.GetValue(parent))
                  // parent control is hidden
                  return true;
            }

            // check next parent
            control = parent;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="currentControl"></param>
      /// <returns></returns>
      static MgPanel GetMgPanel(MgTabControl currentControl)
      {
         foreach (TabPage item in currentControl.TabPages)
         {
            if (item.Controls.Count > 0 && item.Controls[0] is MgPanel)
               return item.Controls[0] as MgPanel;
         }

         return null;
      }

      /// <summary>
      /// return the ComponentWrapper object for this component
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal ComponentWrapper GetComponentWrapper(Control control)
      {
         return componentsDictionary[control];
      }

      /// <summary>
      /// Create the ComponentWrapper object for this component
      /// </summary>
      /// <param name="control"></param>
      internal void CreateComponentWrapper(Control control)
      {
         Debug.Assert(!componentsDictionary.ContainsKey(control));
         componentsDictionary[control] = new ComponentWrapper(control, AdminMode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal void AddControlToControlDictionary(Control control)
      {
         // while create ComponentWrapper add it to the controlDictionary (isn is the key and Control is the value)
         ControlDesignerInfo controlDesignerInfo = control.Tag as ControlDesignerInfo;
         if (controlDesignerInfo != null)
            controlDictionary[controlDesignerInfo.Id] = control;
      }

      /// <summary>
      /// reset all selected controls properties to studio values
      /// </summary>
      public void ResetSelectedControls()
      {
         DesignerTransaction transaction = host.CreateTransaction("Reset Selected Controls");
         ICollection collection = selectionService.GetSelectedComponents();
         foreach (Control item in collection)
         {
            ResetControl(item);
         }

         transaction.Commit();
      }

      /// <summary>
      /// reset all controls properties to studio values
      /// </summary>
      public void ResetAllControls()
      {
         DesignerTransaction transaction = host != null ? host.CreateTransaction("Reset All Controls") : null;
         foreach (var item in componentsDictionary.Keys)
         {
            ResetControl((Control)item);
         }
         if(transaction != null)
            transaction.Commit();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      public void ResetControl(Control control)
      {
         componentsDictionary[control].ResetAllProperties(AdminMode);
         HandleControlValueReset(control, null);

         // in case the reset caused the layer to change, need to recalculate visibility
         int parentId = ((ControlDesignerInfo)control.Tag).ParentId;
         if (parentId != 0)
            HandleVisibility(ControlDictionary[parentId]);
         else if (AdminMode)
            control.Visible = true;
      }

      /// <summary>
      /// Is the selected controls be deleted
      /// </summary>
      /// <returns></returns>
      internal bool CanDeleteSelectedItems()
      {
         ICollection collection = selectionService.GetSelectedComponents();

         // form can't be deleted 
         if (collection.Count == 0)
            return false;

         foreach (var item in collection)
         {
            if (item is Form)
               return false;
            // fixed defect #:128210, frame set can't be deleted 
            if (IsFrame(item as Control))
               return false;
            else if (ComponentsDictionary[item].PropertiesDescriptors[Constants.WinPropVisible] == null)
            {
               return false;
            }
         }

         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      bool IsFrame(Control control)
      {
         bool isFrame = false;
         if (control != null)
         {
            ControlDesignerInfo controlDesignerInfo = (control.Tag as ControlDesignerInfo);
            isFrame = (controlDesignerInfo != null && controlDesignerInfo.IsFrame) || controlDesignerInfo.ControlType == MgControlType.CTRL_TYPE_CONTAINER;
         }
         return isFrame;
      }

      /// <summary>
      /// delete selected controls
      /// </summary>
      public void DeleteSelectedControls()
      {
         ICollection selected = selectionService.GetSelectedComponents();

         // Don't delete controls which can not be deleted
         foreach (Control item in selected)
         {
            if (ComponentsDictionary[item].PropertiesDescriptors[Constants.WinPropVisible] == null)
               return;
         }

         // hide the controls
         foreach (Control item in selected)
         {
            ComponentsDictionary[item].PropertiesDescriptors[Constants.WinPropVisible].SetValue(item, false);
            item.Visible = false;
            HandleVisibility(item);
         }

         // cancel the selection
         selectionService.SetSelectedComponents(new object[] { }, SelectionTypes.Replace);

         // raise the event
         OnControlsDeleted(selected);
      }

      /// <summary>
      /// restore a control to be visible on the designer
      /// </summary>
      /// <param name="control"></param>
      internal void RestoreControl(Control control)
      {
         ComponentsDictionary[control].PropertiesDescriptors[Constants.WinPropVisible].SetValue(control, true);
         // if the control is not hidden due to a hidden ancestor, try to show it
         if (!IsControlAncestorHidden(control))
         {
            control.Visible = true;
            // if the control is linked, set its visibility according to its parent
            int parentId = ((ControlDesignerInfo)control.Tag).ParentId;
            if (parentId != 0)
               HandleVisibility(ControlDictionary[parentId]);
            else
               HandleVisibility(control);

            selectionService.SetSelectedComponents(new object[] { control }, SelectionTypes.Replace);
         }
      }

      internal event ControlsDeletedDelegate ControlsDeleted;

      /// <summary>
      /// raise the controls hidden event
      /// </summary>
      /// <param name="controls"></param>
      void OnControlsDeleted(ICollection controls)
      {
         if (ControlsDeleted != null)
            ControlsDeleted(this, new ControlsDeletedArgs(controls));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="orgString"></param>
      /// <returns></returns>
      internal static String GetTranslatedString(RuntimeHostSurface runtimeHostSurface, String orgString)
      {
         String retString = orgString;
         ITranslate host = (ITranslate)runtimeHostSurface.GetService(typeof(ITranslate));
         if (host != null)
            retString = host.GetTranslateString(orgString);

         return retString;
      }

      /// <summary>
      /// special treatment for button properties
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      void SetButtonValuesFromProperties(Control control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         // check the control type: The button control type link is CTRL_TYPE_BUTTON but not MgButtonBase
         if (control is MgButtonBase)
         {
            if (properties.ContainsKey("BackColor") && properties["BackColor"].IsDefaultValue)
               ((MgButtonBase)control).UseVisualStyleBackColor = true;
         }
      }

      /// <summary>
      /// special treatment for radio buttons
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      void SetRadioButtonValuesFromProperties(Control control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         foreach (MgRadioButton item in control.Controls)
         {
            if (item.IsBasePaint)
               item.Text = item.TextToDisplay;
         }
      }

      /// <summary>
      /// perform special treatment when a property value is reset
      /// </summary>
      /// <param name="control"></param>
      /// <param name="propeKey"> the property that reset, if it is null then all properties are reset </param>
      internal void HandleControlValueReset(Control control, string propeKey)
      {
         if (RelatedPropertiesHandlingDictionary.ContainsKey(((ControlDesignerInfo)control.Tag).ControlType))
            RelatedPropertiesHandlingDictionary[((ControlDesignerInfo)control.Tag).ControlType](control, ((ControlDesignerInfo)control.Tag).Properties);

         UpdateGradientStyleProperty(control, propeKey);
      }

      /// <summary>
      /// if background color is reset then reset also the gradient style
      /// </summary>
      /// <param name="control"></param>
      /// <param name="propeKey"></param>
      private void UpdateGradientStyleProperty(Control control, string propeKey)
      {         
         if (propeKey == null || propeKey.Equals(Constants.WinPropBackColor))
         {
            if (ComponentsDictionary.ContainsKey(control) &&
                ComponentsDictionary[control].PropertiesDescriptors[Constants.WinPropGradientStyle] != null)
            {
               RTDesignerPropertyDescriptor propertyDescriptor = ComponentsDictionary[control].PropertiesDescriptors[Constants.WinPropGradientStyle] as RTDesignerPropertyDescriptor;
               ((IGradientColorProperty)control).GradientStyle = (GradientStyle)propertyDescriptor.DefaultValue;
               
               // for tab control reset the panel of the tab control 
               MgPanel mgPanel = BackgroundColorStrategy.GetMgPanelOfTabControl(control);
               if (mgPanel != null)
                  ((IGradientColorProperty)mgPanel).GradientStyle = (GradientStyle)propertyDescriptor.DefaultValue;
               control.Invalidate();
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="disposing"></param>
      protected override void Dispose(bool disposing)
      {
         runtimeMenuService.Dispose();
         base.Dispose(disposing);
      }
   }// class

   /// <summary>
   /// event args for controls hiding event
   /// </summary>
   class ControlsDeletedArgs : EventArgs
   {
      internal ICollection Controls;

      internal ControlsDeletedArgs(ICollection controls)
      {
         Controls = controls;
      }
   }

}// namespace
