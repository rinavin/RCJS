using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using RuntimeDesigner.Serialization;
using com.magicsoftware.util;
using System.ComponentModel;
using System.Collections.Generic;
using com.magicsoftware.support;



namespace RuntimeDesigner
{
   public delegate String GetTranslateStringDelegate(String str);

   /// <summary>
   /// This is the Shell that has the Toolbox, PropertyGrid, hosts Designers, etc.
   /// </summary>
   public partial class MainShell : Form, ITranslate
   {
      internal bool AdminMode;

      private RuntimeHostSurfaceManager _hostSurfaceManager = null;

      HiddenControlsPane hiddenControlsPane;

      /// <summary>
      /// runtime host surface
      /// </summary>
      RuntimeHostSurface RuntimeHostSurface
      {
         get
         {
            return CurrentDocumentsHostControl.HostSurface;
         }
      }
      /// <summary>
      /// RuntimeDesignerHandleState
      /// </summary>
      RuntimeDesignerHandleState RuntimeDesignerHandleState
      {
         get
         {
            return RuntimeHostSurface.RuntimeDesignerHandleState;
         }
      }

      public Form Form
      {
         get
         {
            return CurrentDocumentsHostControl.DesignerHost.RootComponent as Form;
         }
      }


      private GetTranslateStringDelegate TranslateStringDelegate { get; set; }

      public MainShell(GetTranslateStringDelegate translateStringDelegate, Icon icon, bool adminMode)
      {
         AdminMode = adminMode;
         TranslateStringDelegate = translateStringDelegate;

         InitializeComponent();
         CustomInitialize();
         SetProperties(icon);

         FormClosing += MainShell_FormClosing;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MainShell_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (RuntimeDesignerHandleState.OnClose(CurrentDocumentsHostControl.HostSurface, GetFileName()))
            e.Cancel = true;
      }



      /// <summary>
      /// 
      /// </summary>
      /// <param name="title"></param>
      /// <param name="form"></param>
      private void SetProperties(Icon icon)
      {
         MinimizeBox = false; // hide the minimize button and not allow minimize.
         Icon = icon;
      }

      /// <summary>
      /// Adds custom services to the HostManager like TGoolbox, PropertyGrid, 
      /// SolutionExplorer.
      /// It is used by the HostSurfaceManager
      /// to write out to the OutputWindow. You can add any services
      /// you want.
      /// </summary>
      private void CustomInitialize()
      {
         if (!AdminMode)
         {
            splitContainer1.Panel2.Controls.Remove(splitContainer2);
            splitContainer1.Panel2.Controls.Add(splitContainer2.Panel1.Controls[0]);
            splitContainer2.Dispose();
            splitContainer2 = null;

         }
         ReplaceMenuString(mainMenu1);
         Text = TranslateStringDelegate(Text);
         label1.Text = TranslateStringDelegate(label1.Text);
         label2.Text = TranslateStringDelegate(label2.Text);


         _hostSurfaceManager = new RuntimeHostSurfaceManager();
         _hostSurfaceManager.AddService(typeof(System.Windows.Forms.PropertyGrid), this.propertyGrid1);
      }


      void ReplaceMenuString(Menu menu)
      {
         foreach (MenuItem item in menu.MenuItems)
         {
            item.Text = TranslateStringDelegate(item.Text);
            ReplaceMenuString(item);
         }
      }

      public void AddDesigner(Form form, CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControls, GetControlDesignerInfoDelegate getControlDesignerInfo)
      {
         RuntimeHostControl hc = _hostSurfaceManager.GetNewHost(form, createAllOwnerDrawControls, getControlDesignerInfo, AdminMode, this);
         AddNewHost(hc);

         RuntimeDesignerHandleState.SaveState(GetFileName(), RuntimeHostSurface.ComponentsDictionary);

         if (AdminMode)
         {
            FillHiddenControlsPane(RuntimeHostSurface.ComponentsDictionary);

            RuntimeHostSurface.ControlsDeleted += runtimeHostSurface_ControlsHidden;
         }
      }

      /// <summary>
      /// handle hiding of controls on the hidden controls pane
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="canResizeArgs"></param>
      void runtimeHostSurface_ControlsHidden(object sender, ControlsDeletedArgs canResizeArgs)
      {
         if (hiddenControlsPane != null)
            hiddenControlsPane.ControlsDeleted(canResizeArgs.Controls);
      }

      /// <summary>
      /// handle restoring a hidden control
      /// </summary>
      /// <param name="control"></param>
      void ControlRestored(Control control)
      {
         RuntimeHostSurface.RestoreControl(control);
      }


      private RuntimeHostControl CurrentDocumentsHostControl
      {
         get
         {
            return (RuntimeHostControl)this.splitContainer1.Panel1.Controls[0];
         }
      }



      /// <summary>
      /// Persist the code if the host is loaded using a BasicDesignerLoader
      /// </summary>
      private void saveMenuItem_Click(object sender, System.EventArgs e)
      {
         ExecuteSaveCommand();

      }

      public void ExecuteSaveCommand()
      {
         RuntimeDesignerSerializer.SerializeToFiles(RuntimeHostSurface.ComponentsDictionary);

         RuntimeDesignerHandleState.SaveState(GetFileName(), RuntimeHostSurface.ComponentsDictionary);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public List<ControlItem> ExecuteDeSerializeFromFileCommand()
      {
         return RuntimeDesignerSerializer.DeSerializeFromFile(GetFileName());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal string GetFileName()
      {
         RuntimeHostSurface runtimeHostSurface = this.CurrentDocumentsHostControl.HostSurface;
         Form form = CurrentDocumentsHostControl.DesignerHost.RootComponent as Form;
         String fileName = "";
         if (runtimeHostSurface.ComponentsDictionary.ContainsKey(form))
         {
            ComponentWrapper cw = runtimeHostSurface.ComponentsDictionary[form];
            PropertyDescriptor pd = cw.PropertiesDescriptors[Constants.ConfigurationFilePropertyName];
            if (pd != null) //for tests
               fileName = pd.GetValue(form) as String;
         }

         return fileName;
      }

      private void Save(bool saveAs)
      {
         RuntimeHostControl currentHostControl = CurrentDocumentsHostControl;
         ((RuntimeHostLoader)currentHostControl.HostSurface.Loader).Save(saveAs);
      }

      private void saveAsMenuItem_Click(object sender, System.EventArgs e)
      {
         Save(true);
      }


      void resetToDefault_Click(object sender, System.EventArgs e)
      {
         CurrentDocumentsHostControl.HostSurface.ResetAllControls();

         if (hiddenControlsPane != null)
            hiddenControlsPane.ResetHiddenControls();
      }

      private void exitMenuItem_Click(object sender, System.EventArgs e)
      {
         this.Close();
      }


      private void AddNewHost(RuntimeHostControl hc)
      {
         hc.Parent = splitContainer1.Panel1;
         hc.Dock = DockStyle.Fill;
         _hostSurfaceManager.ActiveDesignSurface = hc.HostSurface;
      }

      /// <summary>
      /// create the hidden controls pane
      /// </summary>
      void FillHiddenControlsPane(Dictionary<object, ComponentWrapper> componentsDictionary)
      {
         hiddenControlsPane = new HiddenControlsPane(componentsDictionary, ControlRestored, RuntimeHostSurface.IsControlAncestorHidden);
         this.tableLayoutPanel2.Controls.Add(hiddenControlsPane, 0, 1);
         //hiddenControlsPane.Parent = splitContainer2.Panel2;
         hiddenControlsPane.Dock = DockStyle.Fill;
         hiddenControlsPane.Margin = new Padding(0);
      }

      /// <summary>
      /// Perform all the Edit menu options using the MenuCommandService
      /// </summary>
      private void PerformAction(string text)
      {

         if (this.CurrentDocumentsHostControl == null)
            return;

         IMenuCommandService ims = this.CurrentDocumentsHostControl.HostSurface.GetService(typeof(IMenuCommandService)) as IMenuCommandService;

         try
         {
            switch (text)
            {
               case "&Lefts":
                  ims.GlobalInvoke(StandardCommands.AlignLeft);
                  break;
               case "&Centers":
                  ims.GlobalInvoke(StandardCommands.AlignHorizontalCenters);
                  break;
               case "&Rights":
                  ims.GlobalInvoke(StandardCommands.AlignRight);
                  break;
               case "&Tops":
                  ims.GlobalInvoke(StandardCommands.AlignTop);
                  break;
               case "&Middles":
                  ims.GlobalInvoke(StandardCommands.AlignVerticalCenters);
                  break;
               case "&Bottoms":
                  ims.GlobalInvoke(StandardCommands.AlignBottom);
                  break;
               default:
                  break;
            }
         }
         catch
         {
         }
      }

      private void ActionClick(object sender, EventArgs e)
      {
         PerformAction((sender as MenuItem).Text);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      public string GetTranslateString(string str)
      {
         return TranslateStringDelegate(str);
      }

      private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
      {

         splitContainer1.Panel1.Focus();
      }

      private void splitContainer1_MouseUp(object sender, MouseEventArgs e)
      {
         splitContainer1.Panel1.Focus();
      }

      private void splitContainer2_MouseUp(object sender, MouseEventArgs e)
      {
         splitContainer2.Panel1.Focus();
      }

      private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
      {
         splitContainer2.Panel1.Focus();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="msg"></param>
      /// <param name="keyData"></param>
      /// <returns></returns>
      protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
      {
         switch (keyData)
         {
            case Keys.Escape:
               {
                  Close();
                  return true;
               }

         }
         return base.ProcessCmdKey(ref msg, keyData);
      }
   }
}
