using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Design;
using System.Data;
using System.Windows.Forms;
using System.IO;

using System.Collections.Generic;


namespace RuntimeDesigner
{


   /// <summary>
   /// Manages numerous HostSurfaces. Any services added to HostSurfaceManager
   /// will be accessible to all HostSurfaces
   /// </summary>
   class RuntimeHostSurfaceManager : DesignSurfaceManager
   {
      internal RuntimeHostSurfaceManager()
         : base()
      {
         this.AddService(typeof(INameCreationService), new NameCreationService());
         AddService(typeof(DesignerOptionService), new MgDesignerOptionService());


      }

      protected override DesignSurface CreateDesignSurfaceCore(IServiceProvider parentProvider)
      {
         return new RuntimeHostSurface(parentProvider, translate);
      }

      ITranslate translate { get; set; }

      /// <summary>
      /// Gets a new HostSurface and loads it with the appropriate type of
      /// root component. Uses the appropriate Loader to load the HostSurface.
      /// </summary>
      internal RuntimeHostControl GetNewHost(Form form, CreateAllOwnerDrawControlsDelegate createAllOwnerDrawControls, 
                                           GetControlDesignerInfoDelegate getControlDesignerInfo, bool adminMode, ITranslate translate)
      {
         this.translate = translate;
         RuntimeHostSurface hostSurface = (RuntimeHostSurface)this.CreateDesignSurface(this.ServiceContainer);
         IDesignerHost host = (IDesignerHost)hostSurface.GetService(typeof(IDesignerHost));
         RuntimeHostLoader basicHostLoader = new RuntimeHostLoader(typeof(Form), form, createAllOwnerDrawControls, getControlDesignerInfo, 
                                                                   hostSurface);
         hostSurface.AdminMode = adminMode;
         hostSurface.BeginLoad(basicHostLoader);
         hostSurface.Loader = basicHostLoader;
         hostSurface.Initialize();
         this.ActiveDesignSurface = hostSurface;
         return new RuntimeHostControl(hostSurface);
      }



      internal void AddService(Type type, object serviceInstance)
      {
         this.ServiceContainer.AddService(type, serviceInstance);
      }


   }// class
}// namespace
