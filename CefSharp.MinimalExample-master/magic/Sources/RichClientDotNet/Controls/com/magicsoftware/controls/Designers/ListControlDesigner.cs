using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Provides a designer that can design components that extend listControl.
   /// </summary>        
   class ListControlDesigner : ControlDesigner
   {
      #region Fields/Properties

      private ListControl control;
      private DesignerVerbCollection verbs;

      public override void InitializeExistingComponent(System.Collections.IDictionary defaultValues)
      {
         base.InitializeExistingComponent(defaultValues);
      }

      /// <summary>
      /// Gets the design-time verbs supported by the component that is associated with the designer.
      /// </summary>
      public override DesignerVerbCollection Verbs
      {
         get
         {
            if (verbs == null)
            {
               verbs = new DesignerVerbCollection();

               IEnumerator enumerator = null;

               string currentItem;

               if (control is MgComboBox)
               {
                  if (((MgComboBox)control).Items.Count > 0)
                     enumerator = ((MgComboBox)control).Items.GetEnumerator();
               }
               else
               {
                  if (((MgListBox)control).Items.Count > 0)
                     enumerator = ((MgListBox)control).Items.GetEnumerator();
               }

               if (enumerator != null)
               {
                  verbs.Add(new DesignerVerb(Controls.Properties.Resources.ClearSelectedValue_s, new EventHandler(OnSelectItem)));

                  while (enumerator.MoveNext())
                  {
                     currentItem = (string)enumerator.Current;
                     verbs.Add(new DesignerVerb(Controls.Properties.Resources.SwitchTo_s + currentItem, new EventHandler(OnSelectItem)));
                  }
               }
            }

            CheckVerbStatus();

            return verbs;
         }
      }

      #endregion

      public override SelectionRules SelectionRules
      {
         get
         {
            SelectionRules ret = base.SelectionRules;
            if (control is MgComboBox)
            {
               if ((control as MgComboBox).DrawMode == DrawMode.Normal && !(control as MgComboBox).Is3DResizableComboBox)
                  ret &= ~(SelectionRules.BottomSizeable | SelectionRules.TopSizeable);
            }
            return ret;
         }
      }

      /// <summary>
      /// Initializes the designer with the component.
      /// </summary>
      /// <param name="component"></param>
      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);

         if (component is MgFlexiHeightComboBox)
            control = ((MgFlexiHeightComboBox)component).MgComboBox;
         else
            control = (ListControl)component;

         if (control is MgListBox)
            ((MgListBox)control).SelectedIndexChanged += control_SelectedIndexChanged;
         else
            ((MgComboBox)control).SelectedIndexChanged += control_SelectedIndexChanged;

         IComponentChangeService service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
         if (service != null)
         {
            service.ComponentChanging += new ComponentChangingEventHandler(service_ComponentChanging);
         }
      }

      void service_ComponentChanging(object sender, ComponentChangingEventArgs e)
      {
         //we need to initialize verbs collection to null because the collection is changed from property grid & form designer separately.
         verbs = null;
      }

      void control_SelectedIndexChanged(object sender, EventArgs e)
      {
         CheckVerbStatus();
         RefreshSmartTag();
      }

      private void CheckVerbStatus()
      {
         if (verbs != null)
         {
            for (int index = 0; index < verbs.Count; index++)
               verbs[index].Enabled = (control.SelectedIndex + 1) != index;
         }
      }

      /// <summary>
      /// Refresh the smart tag added of component. 
      /// </summary>
      private void RefreshSmartTag()
      {
         DesignerActionUIService actionUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));

         if (actionUIService != null)
         {
            actionUIService.Refresh(Component);
         }
      }

      /// <summary>
      /// Raises event to select the listed item in listControl 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnSelectItem(object sender, System.EventArgs e)
      {
         control.SelectedIndex = verbs.IndexOf(((DesignerVerb)sender)) - 1;
      }

      protected override void Dispose(bool disposing)
      {
         verbs = null;

         if (control is MgListBox)
            ((MgListBox)control).SelectedIndexChanged -= control_SelectedIndexChanged;
         else
            ((MgComboBox)control).SelectedIndexChanged -= control_SelectedIndexChanged;

         IComponentChangeService service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
         service.ComponentChanging -= service_ComponentChanging;

         base.Dispose(disposing);
      }
   }
}
