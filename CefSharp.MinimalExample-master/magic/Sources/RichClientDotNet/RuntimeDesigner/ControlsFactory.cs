using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.controls.designers;
using com.magicsoftware.support;
using com.magicsoftware.util;
using Controls.com.magicsoftware.controls.MgDummy;
using Controls.com.magicsoftware.controls.MgLine;

namespace RuntimeDesigner
{
   #region ControlFactory
   /// <summary>
   /// http://www.codeproject.com/Articles/12976/How-to-Clone-SerializeToFile-Copy-Paste-a-Windows-Forms
   /// Summary description for ControlFactory.
   /// </summary>
   class ControlFactory
   {

      /// <summary>
      /// save the current toolbar of the form
      /// </summary>
      internal ToolStripEx CurrentToolBar { get; set; }

      IDesignerLoaderHost host;
      CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControls;

      /// <summary>
      /// callback to get the ControlDesignerInfo from the caller
      /// </summary>
      GetControlDesignerInfoDelegate getControlDesignerInfo;

      RuntimeHostSurface runtimeHostSurface;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="host"></param>
      /// <param name="createAllOwnerDrawControlsDelegate"></param>
      /// <param name="getControlDesignerInfo"></param>
      internal ControlFactory(IDesignerLoaderHost host,
         CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControlsDelegate,
         GetControlDesignerInfoDelegate getControlDesignerInfo,
         RuntimeHostSurface runtimeHostSurface)
      {
         this.host = host;
         this.createAllOwnerDrawControls = createAllOwnerDrawControlsDelegate;
         this.getControlDesignerInfo = getControlDesignerInfo;
         this.runtimeHostSurface = runtimeHostSurface;
      }

      /// <summary>
      /// properties to be ignored when setting values on new designer control
      /// </summary>
      List<string> ignoredProperties = new List<string>() { "Tag", "SelectionStart", "SelectionLength", "SelectedText", "TopLevel", "Name", "AutoScrollMinSize" };

      /// <summary>
      /// set values from RT control on the new designer control
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="propertyList"></param>
      void SetControlProperties(Control ctrl, Hashtable propertyList)
      {
         InitDefaultPropertiesValue(ctrl);

         PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(ctrl);

         foreach (PropertyDescriptor myProperty in properties)
         {
            // ignore read-only properties
            if (myProperty.IsReadOnly)
               continue;

            // ignore some properties
            if (ignoredProperties.Contains(myProperty.Name))
               continue;

            if (propertyList.Contains(myProperty.Name))
            {
               Object obj = propertyList[myProperty.Name];
               try
               {
                  object value = myProperty.GetValue(ctrl);
                  if ((value == null && obj != null) ||
                     (value != null && !value.Equals(obj)))
                     myProperty.SetValue(ctrl, obj);
               }
               catch (Exception ex)
               {
                  //do nothing, just continue
                  System.Diagnostics.Trace.WriteLine(ex.Message);
               }

            }

         }

         if (runtimeHostSurface.AdminMode)
         {
            // In admin mode, all controls should be visible. they may be hidden later, if they were hidden by previous designer executions
            ctrl.Visible = true;
         }
         else
            // set the visibility according to the copied control state. For some unknown reason, this does not work when done in the previous loop
            ctrl.Visible = (bool)propertyList[Constants.WinPropVisible];
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal Form FindForm(Control control)
      {
         //if the send obj is control climb up the parents till it find the form. 
         while (control != null && !(control is Form))
            control = control.Parent;
         return (Form)control;
      }


      /// <summary>
      /// Clones controls
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="host"></param>
      /// <returns></returns>
      internal Control CloneCtrl(Control sourceControl, IDesignerHost host)
      {
         SerializedControlData copiesControl = new SerializedControlData(sourceControl);
         ControlDesignerInfo newTagData = getControlDesignerInfo(sourceControl);

         Control newCtrl = CreateComponent(sourceControl, newTagData != null ? newTagData.ControlType : 0);
         if (sourceControl is Form) //this is needed for tests
         {
            newCtrl.Controls.Clear();
         }

         newCtrl.Tag = newTagData;

         if (SetControlPropertiesIsAllowed(sourceControl))
            SetControlProperties(newCtrl, copiesControl.PropertyList);

         // If the parent is scrolled, change the control's location. The scroll state can not be set on the parent, as the
         // parent is not really created yet.
         ScrollableControl s = sourceControl.Parent as ScrollableControl;
         if (s != null)
         {
            newCtrl.Location = new Point(newCtrl.Location.X - s.AutoScrollPosition.X, newCtrl.Location.Y - s.AutoScrollPosition.Y);
         }

         SetStudioValues(newCtrl);

         if (newCtrl.Tag != null && ((ControlDesignerInfo)newCtrl.Tag).Properties != null)
            runtimeHostSurface.CreateComponentWrapper(newCtrl);
         if (newCtrl.Tag != null)
            runtimeHostSurface.AddControlToControlDictionary(newCtrl);

         if (newCtrl is ICanParent)
            ((ICanParent)newCtrl).CanParentEvent += RuntimeHostSurface.CanParent;

         newCtrl.LocationChanged += RuntimeHostSurface.LocationChanged;
         newCtrl.SizeChanged += RuntimeHostSurface.SizeChanged;
         SetMimimumSize(newCtrl);

         return newCtrl;
      }

      /// <summary>
      /// set the minimum size  - a control shouldn't be smaller than its placement size
      /// </summary>
      /// <param name="control"></param>
      void SetMimimumSize(Control control)
      {
         ControlDesignerInfo cdi = control.Tag as ControlDesignerInfo;
         if (cdi != null)
         {
            // make sure the size is not smaller than the placement bounds
            control.MinimumSize = new Size(cdi.PreviousPlacementBounds.Width, cdi.PreviousPlacementBounds.Height);
         }
      }

      /// <summary>
      /// Set the studio values on the controls
      /// </summary>
      /// <param name="newCtrl"></param>
      private void SetStudioValues(Control newCtrl)
      {
         // get the control's prop descriptors
         PropertyDescriptorCollection originalPropDescriptors = TypeDescriptor.GetProvider(newCtrl).GetTypeDescriptor(newCtrl).GetProperties();

         // get the properties dictionary from the control's tag
         Dictionary<string, DesignerPropertyInfo> properties = null;
         if (newCtrl.Tag != null)
            properties = ((ControlDesignerInfo)((Control)newCtrl).Tag).Properties;

         if (properties != null)
         {
            // if there's a special treatment for this control type, deal with it here
            runtimeHostSurface.HandleControlValueReset(newCtrl, null);

            if (newCtrl is Form)
               newCtrl.Location = new Point(0, 0);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      Control CreateComponent(Control sourceControl, MgControlType controlType)
      {
         Control newCtrl = null;
         if (controlType == MgControlType.CTRL_TYPE_DOTNET)
            newCtrl = CreateDotNetWrapper(sourceControl);
         else if  (controlType == MgControlType.CTRL_TYPE_COMBO && ((MgComboBox)sourceControl).DrawMode == DrawMode.OwnerDrawFixed)
            newCtrl = CreateMgFlexiCombo(sourceControl);
          else if (SelectIsAllowed(sourceControl))
            newCtrl = (Control)host.CreateComponent(GetCreateType(sourceControl));
         else
            newCtrl = (Control)Activator.CreateInstance(GetCreateType(sourceControl));
         return newCtrl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      Type GetCreateType(Control sourceControl)
      {
         Type newCtrlType = sourceControl.GetType();

         if (sourceControl is TableControlUnlimitedItems || sourceControl is TableControlLimitedItems)
            newCtrlType = typeof(TableControlUnlimitedItemsRuntimeDesigner);
         else if (NeedToCreateImageFromControl(sourceControl))
            newCtrlType = typeof(MgDummyImage);
         else if (sourceControl is MgTextBox)
            newCtrlType = typeof(MgTextBoxRuntimeDesigner);
         else if (sourceControl is MgRichTextBox)
         {
            ControlDesignerInfo controlDesignerInfo = getControlDesignerInfo(sourceControl);
            if (controlDesignerInfo.ControlType == MgControlType.CTRL_TYPE_RICH_TEXT)
               newCtrlType = typeof(MgRichTextBoxRuntimeDesigner);
            else
               newCtrlType = typeof(MgRichEditBoxRuntimeDesigner);
         }
         else if (sourceControl is MgWebBrowser)
            newCtrlType = typeof(MgWebBrowserRuntimeDesigner);
         else if (sourceControl is MgPanel)
            newCtrlType = typeof(MgPanelRuntimeDesigner);
         else
            newCtrlType = sourceControl.GetType();

         return newCtrlType;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      bool IsSubformControl(Control sourceControl)
      {
         bool isControlIsSubform = false;
         //ControlDesignerInfo controlDesignerInfo = getControlDesignerInfo(sourceControl);

         //if (controlDesignerInfo != null && (controlDesignerInfo.ControlType == MgControlType.CTRL_TYPE_SUBFORM))
         //   isControlIsSubform = true;

         return isControlIsSubform;

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      bool SetControlPropertiesIsAllowed(Control sourceControl)
      {
         bool setControlPropertiesIsAllowed = true;

         if (NeedToCreateImageFromControl(sourceControl))
            setControlPropertiesIsAllowed = false;

         return setControlPropertiesIsAllowed;
      }

      /// <summary>
      /// Copy Container Controls
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <param name="parent"></param>
      /// <returns></returns>
      internal Control CopyContainerControl(Control sourceControl, Control parent)
      {
         Control result = null;
         if (sourceControl is MenuStrip)
            result = CreateMenuStripForForm(sourceControl as MenuStrip, parent);
         else if (sourceControl is ToolStripEx)
            result = CreatetToolStripForForm(sourceControl as ToolStripEx, parent as ToolStripEx);
         else
            result = CreateControl(sourceControl, parent, result);
         return result;
      }

      ///
      private Control CreateControl(Control sourceControl, Control parent, Control result)
      {
         result = CloneCtrl((Control)sourceControl, host);

         if (result is ISetSpecificControlPropertiesForFormDesigner)
            ((ISetSpecificControlPropertiesForFormDesigner)result).setSpecificControlPropertiesForFormDesigner(sourceControl);

         if (parent != null)
         {
            parent.Controls.Add(result);
            // prevent the resize of frames in framesets
            if (parent.Tag != null && ((ControlDesignerInfo)parent.Tag).ControlType == MgControlType.CTRL_TYPE_FRAME_SET)
               TypeDescriptor.GetProperties(result)["Locked"].SetValue(result, true);
         }

         if (CloneChildrenAllowed(sourceControl))
         {
            // get the list of child controls, and duplicate them
            Dictionary<Control, bool> list = createAllOwnerDrawControls != null ? createAllOwnerDrawControls(sourceControl) : new Dictionary<Control, bool>();

            foreach (var item in list)
            {
               CopyContainerControl(item.Key, result);
               // if the control is the representation of a logical control, dispose it
               if(item.Value)
                  item.Key.Dispose();
            }
         }

         if (result is TabControl)
         {
            // The selected index of a tab control can be set only after the tab pages were created
            ((TabControl)result).SelectedIndex = ((TabControl)sourceControl).SelectedIndex;
            // After the tab pages were created, need to recalculate the MaxTextWidth
            ((MgTabControl)result).UpdateMaxTextWidth();
         }

         if (result is IChoiceControl)
            ((IChoiceControl)result).SelectedIndexChanged += runtimeHostSurface.SelectedIndexChanged;

         // special settings for MgPanel
         if (result is MgPanel)
         {
            result.Paint += RuntimeHostSurface.MgPanel_Paint;

            //for MgPanel with parent MgTabPage 
            if (result.Parent is MgTabPage)
            {
               // set paint event
               ((MgPanel)result).ComponentDropped += runtimeHostSurface.MgTabControl_ComponentDropped;
               // set the tab control background color to be the same as the panel
               result.Parent.Parent.BackColor = result.BackColor;
            }
         }
         return result;
      }

      /// <summary>
      /// Copy Menu Strip
      /// </summary>
      /// <param name="menustrip"></param>
      /// <returns></returns>
      private MenuStrip CopyMenuStrip(ref MenuStrip menustrip)
      {
         MenuStrip ms = new MenuStrip();
         ToolStripItem msiNew;

         foreach (ToolStripItem msi in menustrip.Items)
         {
            if ((msi is ToolStripMenuItem))
            {
               msiNew = new ToolStripMenuItem(msi.Text, null, null, msi.Name);
               ms.Items.Add(msiNew);
            }
            else if ((msi is ToolStripSeparator))
            {
               msiNew = new ToolStripSeparator();
               ms.Items.Add(msiNew);
            }
         }
         return ms;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <param name="parent"></param>
      /// <returns></returns>
      private Control CreatetToolStripForForm(ToolStripEx sourceControl, ToolStripEx parent)
      {
         ToolStripEx newToolBox = null;

         if (sourceControl is ToolStripEx && sourceControl.Visible == true)
         {
            newToolBox = new ToolStripEx();
            ToolStripItem msiNew = null;

            foreach (ToolStripItem msi in sourceControl.Items)
            {
               if ((msi is ToolStripSeparator))
                  msiNew = new ToolStripSeparator();
               else if ((msi is ToolStripButton))
               {
                  msiNew = new ToolStripButton();
                  msiNew.Image = msi.Image;
                  msiNew.DisplayStyle = msi.DisplayStyle;
                  msiNew.Text = msi.Text;
                  msiNew.Visible = msi.Visible;
                  msiNew.Enabled = msi.Enabled;
               }

               if (msiNew != null)
                  newToolBox.Items.Add(msiNew);
            }
         }

         CurrentToolBar = newToolBox;
         return newToolBox;
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <param name="parent"></param>
      /// <returns></returns>
      private Control CreateMenuStripForForm(MenuStrip sourceControl, Control parent)
      {
         MenuStrip newMenuStrip = null;

         if (sourceControl is MenuStrip && sourceControl.Visible == true)
         {
            newMenuStrip = CopyMenuStrip(ref sourceControl);

            if (parent != null)
               parent.Controls.Add(newMenuStrip);

            Form form = FindForm(newMenuStrip);
            if (form != null)
               form.MainMenuStrip = newMenuStrip;
         }
         return newMenuStrip;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <param name="parent"></param>
      /// <returns></returns>
      internal Control PrepearForm(Control sourceControl, Control parent)
      {
         Control retControl = CopyContainerControl(sourceControl, parent);

         PrepeareToolBar(retControl as Form);

         return retControl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="retControl"></param>
      private void PrepeareToolBar(Form CurrentForm)
      {
         if (CurrentToolBar != null)
         {
            CurrentForm.Controls.Add(CurrentToolBar);
            //find menustrip
            foreach (Control control in CurrentForm.Controls)
            {
               if (control is MenuStrip)
               {
                  //QCR #779146, make sure pulldown menu always above the toolbar
                  int idx = CurrentForm.Controls.IndexOf(CurrentForm.MainMenuStrip);
                  CurrentForm.Controls.SetChildIndex(CurrentToolBar, idx);
                  break;
               }
            }

            CurrentToolBar = null;
         }
      }

      ///// <summary>
      ///// 
      ///// </summary>
      ///// <param name="sourceControl"></param>
      ///// <returns></returns>
      private bool CloneChildrenAllowed(Control sourceControl)
      {
         bool cloneChildren = true;

         if (NeedToCreateImageFromControl(sourceControl))
            cloneChildren = false;
         else if (sourceControl is com.magicsoftware.controls.TableControl ||
             sourceControl is MgWebBrowser)
            cloneChildren = false;

         return cloneChildren;
      }

      bool NeedToCreateImageFromControl(Control control)
      {
         bool createImageFromControl = false;
         if (control is StatusStrip)
            createImageFromControl = true;

         return createImageFromControl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      bool SelectIsAllowed(Control control)
      {
         bool selectIsAllowed = true;

         if (control is MenuStrip || control is ToolStrip)
            selectIsAllowed = false;
         else if (control is StatusStrip)
            selectIsAllowed = false;
         // the children of MgRadioPanel are not allowed to be selectable 
         else if (control.Parent is MgRadioPanel)
            selectIsAllowed = false;
         else
            // line control isn't allowed to be select
            if (control is MgLine)
               selectIsAllowed = false;
         //else if ancestor is DotNet\Subform\Frameset
         //     selectIsAllowed = false;

         return selectIsAllowed;
      }

      /// <summary>
      /// store the default values from the control, for the cases where the runtime did not have a value for the property
      /// </summary>
      /// <param name="control"></param>
      private void InitDefaultPropertiesValue(Control control)
      {
         if (((ControlDesignerInfo)control.Tag) != null)
         {
            // properties sent from the designer invoker
            Dictionary<string, DesignerPropertyInfo> mgProperties = ((ControlDesignerInfo)control.Tag).Properties;
            // properties of the newly created control
            PropertyDescriptorCollection nativeProperties = TypeDescriptor.GetProperties(control);

            // for some controls(as menu\toolbar...) we don't create  DesignerPropertyInfo
            if (mgProperties != null)
            {
               foreach (var item in mgProperties)
               {
                  // if the property from the runtime has the default value, and it is a real property of the control
                  if (item.Value.IsDefaultValue && (nativeProperties[item.Key] != null))
                     item.Value.Value = nativeProperties[item.Key].GetValue(control);
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      private Control CreateDotNetWrapper(Control sourceControl)
      {
         Control wrapper = (Control)host.CreateComponent(typeof(DotNetComponentWrapper));
         Control dotNetControl = (Control)Activator.CreateInstance(sourceControl.GetType());
         dotNetControl.Dock = DockStyle.Fill;
         wrapper.Controls.Add(dotNetControl);
         
         return wrapper;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sourceControl"></param>
      /// <returns></returns>
      private Control CreateMgFlexiCombo(Control sourceControl)
      {
         Control combo = (Control)host.CreateComponent(typeof(MgFlexiHeightComboBox));
         ((MgFlexiHeightComboBox)combo).MgComboBox.SetDrawMode(((ComboBox)sourceControl).DrawMode);

         return combo;
      }

   }

   #endregion


}
