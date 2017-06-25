using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

namespace RuntimeDesigner
{
   /// <summary>
   /// Hosts the HostSurface which inherits from DesignSurface.
   /// </summary>
   class RuntimeHostControl : System.Windows.Forms.UserControl
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;
      private RuntimeHostSurface _hostSurface;

      IMessageFilter messageFilter;

      internal RuntimeHostControl(RuntimeHostSurface hostSurface)
      {
         // This call is required by the Windows.Forms Form Designer.
         InitializeComponent();
         InitializeHost(hostSurface);

         messageFilter = new RTDesignerMessageFilter((DesignerActionUIService)hostSurface.GetService(typeof(DesignerActionUIService)),
                                              (ISelectionService)(hostSurface.GetService(typeof(ISelectionService))));
         Application.AddMessageFilter(messageFilter);
      }

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (components != null)
               components.Dispose();
         }

         Application.RemoveMessageFilter(messageFilter);

         Control control = _hostSurface.View as Control;
         control.PreviewKeyDown -= control_PreviewKeyDown;

         _hostSurface.Dispose();

         base.Dispose(disposing);
      }

      #region Component Designer generated code
      /// <summary>
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         // 
         // HostControl
         // 
         this.Name = "HostControl";
         this.Size = new System.Drawing.Size(268, 224);
      }
      #endregion

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hostSurface"></param>
      internal void InitializeHost(RuntimeHostSurface hostSurface)
      {
         try
         {
            if (hostSurface == null)
               return;

            _hostSurface = hostSurface;

            Control control = _hostSurface.View as Control;

            control.Parent = this;
            control.Dock = DockStyle.Fill;
            control.Visible = true;
            DesignerActionUIService actionUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
            control.PreviewKeyDown += control_PreviewKeyDown;
         }
         catch (Exception ex)
         {
            Trace.WriteLine(ex.ToString());
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
      {
         _hostSurface.HandleKeyDown(e);
      }

      /// <summary>
      /// 
      /// </summary>
      internal RuntimeHostSurface HostSurface
      {
         get
         {
            return _hostSurface;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      internal IDesignerHost DesignerHost
      {
         get
         {
            return (IDesignerHost)_hostSurface.GetService(typeof(IDesignerHost));
         }
      }

   } // class
}// namespace
