using System;
using System.Windows.Forms.Design;
using com.magicsoftware.controls;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Controls.com.magicsoftware.support;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Provides a designer that can design components that extend RadioControl.
   /// </summary>
   class RadioControlDesigner : ControlDesigner
   {
      #region Fields/Properties

      private MgRadioPanel radioPanel;
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

               if (radioPanel.Controls.Count > 0)
               {
                  verbs.Add(new DesignerVerb(Controls.Properties.Resources.ClearSelectedValue_s, new EventHandler(OnSelectItem)));

                  foreach (Control radioControl in radioPanel.Controls)
                  {
                     verbs.Add(new DesignerVerb(Controls.Properties.Resources.SwitchTo_s + ((IDisplayInfo)radioControl).TextToDisplay, new EventHandler(OnSelectItem)));
                  }
               }
            }

            CheckVerbStatus();

            return verbs;
         }
      }

      #endregion

      /// <summary>
      /// Initializes the designer with the component.
      /// </summary>
      /// <param name="component"></param>
      public override void Initialize(System.ComponentModel.IComponent component)
      {
         radioPanel = (MgRadioPanel)component;

         base.Initialize(component);

         ((MgRadioPanel)radioPanel).SelectedIndexChanged += control_SelectedIndexChanged;

         IComponentChangeService service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
         if (service != null)
         {
            service.ComponentChanging += new ComponentChangingEventHandler(service_ComponentChanging);
         }
      }

      void service_ComponentChanging(object sender, ComponentChangingEventArgs e)
      {
         //we need to initialize verbs collection to null because the collection is changed from property grid & form designer separately.
         ResetVerbs();
      }

      void control_SelectedIndexChanged(object sender, EventArgs e)
      {
         CheckVerbStatus();
         RefreshSmartTag();
      }

      internal void ResetVerbs()
      {
         verbs = null;
      }

      private void CheckVerbStatus()
      {
         if (verbs != null)
         {
            for (int index = 0; index < verbs.Count; index++)
               verbs[index].Enabled = (radioPanel.SelectedIndex + 1) != index;
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
      /// Raises event to select the listed item in RadioPanel 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnSelectItem(object sender, System.EventArgs e)
      {
         radioPanel.SelectedIndex = verbs.IndexOf(((DesignerVerb)sender)) - 1;
      }

      protected override void Dispose(bool disposing)
      {
         verbs = null;

         radioPanel.SelectedIndexChanged -= control_SelectedIndexChanged;

         IComponentChangeService service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
         service.ComponentChanging -= service_ComponentChanging;

         base.Dispose(disposing);
      }
   }
}
