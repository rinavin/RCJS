using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui
{
   /// <summary>
   /// manage the show and hide of the splash screen
   /// </summary>
   public class SpashScreenManager
   {
      Form form;
      Thread thread;
      Image image;
      public bool IsSplashOpen { get; set; }

      /// <summary>
      /// CTOR - singleton
      /// </summary>
      private SpashScreenManager()
      { }

      static SpashScreenManager instance;
      /// <summary>
      /// 
      /// </summary>
      public static SpashScreenManager Instance
      {
         get
         {
            if(instance == null)
            {
               lock (typeof(SpashScreenManager))
               {
                  if (instance == null)
                     instance = new SpashScreenManager();
               }
            }
            return instance;
         }
      }



      public void ShowSplashForm(string fileName)
      {
         try
         {
            image = Bitmap.FromFile(fileName);
         }
         catch
         {
            return;
         }
         thread = new Thread(new ThreadStart(ShowSplashFormInternal));
         thread.SetApartmentState(ApartmentState.STA);
         thread.Start();
      }

      /// <summary>
      /// create and show the form
      /// </summary>
      void ShowSplashFormInternal()
      {
         form = new Form();
         form.FormBorderStyle = FormBorderStyle.None;
         form.StartPosition = FormStartPosition.CenterScreen;
         form.BackgroundImage = image;
         form.ClientSize = form.BackgroundImage.Size;
         form.ControlBox = false;
         form.MaximizeBox = false;
         form.MinimizeBox = false;
         form.ShowIcon = false;
         form.Show();
         IsSplashOpen = true;
         Application.Run(form);
      }


      private delegate void CloseDelegate();

      /// <summary>
      /// close the form
      /// </summary>
      public void CloseSplashWindow()
      {
         if (form != null && !form.IsDisposed)
         {
            form.Invoke(new CloseDelegate(CloseSplashWindowInternal));
            thread.Abort();
         }
      }

      /// <summary>
      /// close the form in its own thread
      /// </summary>
      void CloseSplashWindowInternal()
      {
         if (IsSplashOpen)
         {
            IsSplashOpen = false;
            form.Close();
            form.Dispose();
         }
      }
   }
}
