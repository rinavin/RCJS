using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using com.magicsoftware.support;
using RuntimeDesigner.Serialization;
using com.magicsoftware.util;
using com.magicsoftware.controls;
 
namespace RuntimeDesigner
{

   public delegate Dictionary<Control, bool> CreateAllOwnerDrawControlsDelegate(Control control);
   public delegate ControlDesignerInfo GetControlDesignerInfoDelegate(object component);

   /// <summary>
   /// Inherits from BasicDesignerLoader. It can persist the HostSurface
   /// to an Xml file and can also parse the Xml file to re-create the
   /// RootComponent and all the components that it hosts.
   /// </summary>
   class RuntimeHostLoader : BasicDesignerLoader
   {
      private bool dirty = true;
      private bool unsaved;
      private string fileName;
      private IDesignerLoaderHost host;
      private static readonly Attribute[] propertyAttributes = new Attribute[] {
			DesignOnlyAttribute.No
		};
      private Type rootComponentType;
      CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControls;

      GetControlDesignerInfoDelegate getControlDesignerInfo;

      RuntimeHostSurface runtimeHostSurface;

      #region Constructors
      Form formToClone;


      /// Empty constructor simply creates a new form.
      internal RuntimeHostLoader(Type rootComponentType, Form formToClone,
                               CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControls,
                               GetControlDesignerInfoDelegate getControlDesignerInfo,
                               RuntimeHostSurface hostSurface)
      {
         this.rootComponentType = rootComponentType;
         this.Modified = true;
         this.formToClone = formToClone;
         this.createAllOwnerDrawControls = createAllOwnerDrawControls;
         this.getControlDesignerInfo = getControlDesignerInfo;
         this.runtimeHostSurface = hostSurface;
      }

      /// <summary>
      /// This constructor takes a file name.  This file
      /// should exist on disk and consist of XML that
      /// can be read by a data set.
      /// </summary>
      internal RuntimeHostLoader(string fileName)
      {
         if (fileName == null)
         {
            throw new ArgumentNullException("fileName");
         }

         this.fileName = fileName;
      }
      #endregion

      #region Overriden methods of BasicDesignerLoader

      // Called by the host when we load a document.
      protected override void PerformLoad(IDesignerSerializationManager designerSerializationManager)
      {
         this.host = this.LoaderHost;

         if (host == null)
         {
            throw new ArgumentNullException("BasicHostLoader.BeginLoad: Invalid designerLoaderHost.");
         }

         // The loader will put error messages in here.
         ArrayList errors = new ArrayList();
         bool successful = true;
         string baseClassName = "Form1";


         if (rootComponentType == typeof(Form))
         {
            //Control control = (Control)host.CreateComponent(typeof(GuiForm));

            ControlFactory factory = new ControlFactory(host, createAllOwnerDrawControls, getControlDesignerInfo, runtimeHostSurface);
            Form form = factory.PrepearForm(formToClone, null) as Form;
            TypeDescriptor.GetProperties(form)["Locked"].SetValue(form, true);
            baseClassName = "Form1";

            SetSerializedValues(form);

            SetVisibility(form);
         }

         // Now that we are done with the load work, we need to begin to listen to events.
         // Listening to event notifications is how a designer "Loader" can also be used
         // to save data.  If we wanted to integrate this loader with source code control,
         // we would listen to the "ing" events as well as the "ed" events.
         IComponentChangeService cs = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService;

         if (cs != null)
         {
            cs.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged);
            cs.ComponentAdded += new ComponentEventHandler(OnComponentAddedRemoved);
            cs.ComponentRemoved += new ComponentEventHandler(OnComponentAddedRemoved);
         }

         // Let the host know we are done loading.
         host.EndLoad(baseClassName, successful, errors);

         // We've just loaded a document, so you can bet we need to flush changes.
         dirty = true;
         unsaved = false;
      }

      /// <summary>
      /// This method is called by the designer host whenever it wants the
      /// designer loader to flush any pending changes.  Flushing changes
      /// does not mean the same thing as saving to disk.  For example,
      /// In Visual Studio, flushing changes causes new code to be generated
      /// and inserted into the text editing window.  The user can edit
      /// the new code in the editing window, but nothing has been saved
      /// to disk.  This sample adheres to this separation between flushing
      /// and saving, since a flush occurs whenever the code windows are
      /// displayed or there is a build. Neither of those items demands a save.
      /// </summary>
      protected override void PerformFlush(IDesignerSerializationManager designerSerializationManager)
      {
         // Nothing to flush if nothing has changed.
         if (!dirty)
         {
            return;
         }


      }
      public override void Dispose()
      {
         // Always remove attached event handlers in Dispose.
         IComponentChangeService cs = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService;

         if (cs != null)
         {
            cs.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
            cs.ComponentAdded -= new ComponentEventHandler(OnComponentAddedRemoved);
            cs.ComponentRemoved -= new ComponentEventHandler(OnComponentAddedRemoved);
         }
      }

      #endregion

      #region Helper methods


      /// <summary>
      /// As soon as things change, we're dirty, so Flush()ing will give us a new
      /// xmlDocument and codeCompileUnit.
      /// </summary>
      private void OnComponentChanged(object sender, ComponentChangedEventArgs ce)
      {
         PropertyGrid propertyGrid = (PropertyGrid)this.GetService(typeof(PropertyGrid));
         if (propertyGrid != null)
            propertyGrid.Refresh();
         dirty = true;
         unsaved = true;
      }
      private void OnComponentAddedRemoved(object sender, ComponentEventArgs ce)
      {
         dirty = true;
         unsaved = true;
      }

      /// <summary>
      /// This method prompts the user to see if it is OK to dispose this document.  
      /// The prompt only happens if the user has made changes.
      /// </summary>
      internal bool PromptDispose()
      {
         if (dirty || unsaved)
         {
            switch (MessageBox.Show("Save changes to existing designer?", "Unsaved Changes", MessageBoxButtons.YesNoCancel))
            {
               case DialogResult.Yes:
                  Save(false);
                  break;

               case DialogResult.Cancel:
                  return false;
            }
         }

         return true;
      }

      #endregion


      #region DeSerializeFromFile - Load


      internal void Save()
      {
         Save(false);
      }

      /// <summary>
      /// Save the current state of the loader. If the user loaded the file
      /// or saved once before, then he doesn't need to select a file again.
      /// Unless this is being called as a result of "Save As..." being clicked,
      /// in which case forceFilePrompt will be true.
      /// </summary>
      internal void Save(bool forceFilePrompt)
      {
         try
         {
            Flush();

            int filterIndex = 3;

            if ((fileName == null) || forceFilePrompt)
            {
               SaveFileDialog dlg = new SaveFileDialog();

               dlg.DefaultExt = "xml";
               dlg.Filter = "XML Files|*.xml";
               if (dlg.ShowDialog() == DialogResult.OK)
               {
                  fileName = dlg.FileName;
                  filterIndex = dlg.FilterIndex;
               }
            }

            if (fileName != null)
            {
               switch (filterIndex)
               {
                  case 1:
                     {
                        // Write out our xmlDocument to a file.
                        StringWriter sw = new StringWriter();
                        XmlTextWriter xtw = new XmlTextWriter(sw);

                        xtw.Formatting = Formatting.Indented;
                        //xmlDocument.WriteTo(xtw);

                        // Get rid of our artificial super-root before we save out
                        // the XML.
                        //
                        string cleanup = sw.ToString().Replace("<DOCUMENT_ELEMENT>", "");

                        cleanup = cleanup.Replace("</DOCUMENT_ELEMENT>", "");
                        xtw.Close();

                        StreamWriter file = new StreamWriter(fileName);

                        file.Write(cleanup);
                        file.Close();
                     }
                     break;
               }
               unsaved = false;
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show("Error during save: " + ex.ToString());
         }
      }

      #endregion

      /// <summary>
      /// Set the values from a previously serialized file on the controls
      /// </summary>
      /// <param name="form"></param>
      private void SetSerializedValues(Form form)
      {

         List<string> fileNames = GetXMLFileNames(form);
         runtimeHostSurface.ResetAllControls();
         foreach (string fileName in fileNames)
         {
            DeserializeXMLfile(fileName);
         }
     }
      

      /// <summary>
      /// check if the 
      /// </summary>
      /// <param name="controlItemsList"></param>
      /// <returns></returns>
      private bool IsControlItemsListValid(string fileName, List<ControlItem> controlItemsList)
      {
         if (controlItemsList != null)
         {
            // loop on all components
            foreach (var keyValue in runtimeHostSurface.ComponentsDictionary) // key - component, value - ComponentWrapper
            {
               Control control = (Control)keyValue.Key;
               ControlDesignerInfo controlDesignerInfo = ((ControlDesignerInfo)control.Tag);
               if (fileName.Equals(controlDesignerInfo.FileName))
               {
                  int isn = controlDesignerInfo.Isn;
                  // try and get the info for this component from the file info
                  ControlItem controlItem = controlItemsList.Find(x => x.Isn == isn);
                  if (controlItem != null && controlItem.Properties != null)
                  {
                     if (!controlDesignerInfo.ControlType.ToString().Equals(controlItem.ControlType))
                        return false;
                  }                 
               }
            }
         }


         return true;
      }

      private void DeserializeXMLfile(String fileName)
      {
         // get the file info
         List<ControlItem> controlItemsList = RuntimeDesignerSerializer.DeSerializeFromFile(fileName);
         if (controlItemsList != null && IsControlItemsListValid(fileName, controlItemsList))
         {            
            // loop on all components
            foreach (var keyValue in runtimeHostSurface.ComponentsDictionary) // key - component, value - ComponentWrapper
            {
               Control control = (Control)keyValue.Key;
               ComponentWrapper cw = keyValue.Value;
               ControlDesignerInfo controlDesignerInfo = ((ControlDesignerInfo)control.Tag);
               if (fileName.Equals(controlDesignerInfo.FileName))
               {
                  int isn = controlDesignerInfo.Isn;
                  // try and get the info for this component from the file info
                  ControlItem controlItem = controlItemsList.Find(x => x.Isn == isn);
                  if (controlItem != null && controlItem.Properties != null)
                  {
                     if (!controlDesignerInfo.ControlType.ToString().Equals(controlItem.ControlType))
                        return;

                     // set the value for and every each property
                     foreach (var item in controlItem.Properties)
                     {
                        object value = item.GetValue();
                        if (ComponentWrapper.IsCoordinateProperty(item.Key))
                           value = ((int)value) + controlDesignerInfo.GetPlacementForProp(item.Key);
                        cw.PropertiesDescriptors[item.Key].SetValue(keyValue.Key, value);
                        if (item.Key == Constants.WinPropVisible)
                           control.Visible = false;
                     }
                  }
               }
            }
         }
      }

      private List<string> GetXMLFileNames(Form form)
      {
         string filename;
         List<string> fileNames = new List<string>();
         // get the form's file name
         if (((ControlDesignerInfo)form.Tag).Properties != null) //for tests
         {
            filename = (string)((ControlDesignerInfo)form.Tag).Properties[Constants.ConfigurationFilePropertyName].Value;
            fileNames.Add(filename);
         }

         //look for subforms
         foreach (var keyValue in runtimeHostSurface.ComponentsDictionary)
         {
            Control control = (Control)keyValue.Key;
            fileName = ((ControlDesignerInfo)control.Tag).FileName;
            if (fileName != null && !fileNames.Contains(fileName))
               fileNames.Add(fileName);
         }
         return fileNames;
      }

      /// <summary>
      /// set the visibility of controls according to their layer
      /// </summary>
      /// <param name="form"></param>
      private void SetVisibility(Form form)
      {
         MgPanel mgPanel = null;
         // get the MgPanel on the form
         for (int i = 0; i < form.Controls.Count && mgPanel == null; i++)
            mgPanel = form.Controls[i] as MgPanel;

         // pass all controls on the form and handle visibility recursive 
         if (mgPanel != null)
         {
            foreach (var item in mgPanel.Controls)
               runtimeHostSurface.HandleVisibility(item);
         }
      }

   }// class
}// namespace

