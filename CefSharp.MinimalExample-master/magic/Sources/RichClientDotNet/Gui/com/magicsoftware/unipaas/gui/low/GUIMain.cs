using System;
using System.Diagnostics;
using System.Windows.Forms;
using com.magicsoftware.util;
#if !PocketPC
using com.magicsoftware.controls.utils;
using System.Drawing;
#else
using MainForm = com.magicsoftware.richclient.mobile.gui.MainForm;
using com.magicsoftware.richclient;
using com.magicsoftware.richclient.http;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
#if !PocketPC
   /// <summary>
   ///   standard framework version
   /// </summary>
   public sealed class GUIMain : GUIMainBase
   {
      private static GUIMain _instance;
      private readonly ApplicationContext _applicationContext = new ApplicationContext();
      private Filter filter;
      private SplashForm startupScreen;

      /// <summary> expose the FormsEnabled from the filter </summary>
      public bool FormsEnabled { get { return filter.FormsEnabled; } }

      /// <summary>
      /// </summary>
      private GUIMain()
      {
      }

      /// <summary>
      ///   returns the single instance of this class
      /// </summary>
      public static GUIMain getInstance()
      {
         if (_instance == null)
         {
            lock (typeof(GUIMain))
            {
               if (_instance == null)
                  _instance = new GUIMain();
            }
         }
         return _instance;
      }

      /// <summary>startup the GUI layer</summary>
      internal override void init(String startupImageFileName)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (!Initialized)
            init(Manager.UseWindowsXPThemes, startupImageFileName);
      }

      /// <summary>
      ///   startup the GUI layer, forcing a value for 'useWindowsXPThemes' environment variable.
      /// </summary>
      /// <param name = "useWindowsXPThemes"></param>
      internal void init(bool useWindowsXPThemes, String startupImageFileName)
      {
         if (useWindowsXPThemes && Utils.IsXPStylesActive())
            Application.EnableVisualStyles();

         filter = new Filter();

         Console.ConsoleWindow.getInstance();
         base.init();

         if (!String.IsNullOrEmpty(startupImageFileName))
         {
            Image startupImage = ImageLoader.GetImage(startupImageFileName);

            if (startupImage != null)
            {
               startupScreen = new SplashForm(startupImage);
               startupScreen.Show();
               startupScreen.Activate();
            }
         }
      }

      /// <summary>
      ///   The main message loop that reads messages from the queue and dispaches them.
      /// </summary>
      internal void messageLoop()
      {
         Debug.Assert(Misc.IsGuiThread());
         lock (this)
         {
            Application.Run(_applicationContext);
         }
      }

      /// <summary>
      ///   Break the message loop and exit the thread
      /// </summary>
      internal void Exit()
      {
         Events.DisplaySessionStatistics();

         _applicationContext.ExitThread();
      }

      /// <summary>
      ///   Dispose all the open forms.
      /// </summary>
      internal void disposeAllForms()
      {
         for (int i = Application.OpenForms.Count;
              i > 0;
              i--)
         {
            Form form = Application.OpenForms[i - 1];
            form.Dispose();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public void AllowkeyboardBuffering()
      {
         filter.AllowKeyboardBuffering = true;
      }

      /// <summary>
      /// 
      /// </summary>
      public void AllowExtendedKeyboardBuffering()
      {
         filter.AllowExtendedKeyboardBuffering = true;
      }

      /// <summary> Returns whether the Startup screen is open. </summary>
      /// <returns></returns>
      internal bool IsStartupScreenOpen()
      {
         return (startupScreen != null);
      }

      /// <summary> Hides the startup screen. </summary>
      internal void HideStartupScreen()
      {
         Debug.Assert(Initialized && Misc.IsGuiThread());

         if (startupScreen != null && !startupScreen.IsDisposed)
         {
            startupScreen.Dispose();
            startupScreen = null;
         }
      }

      /// <summary> Inner class SplashForm </summary>
      private class SplashForm : Form
      {
         /// <summary> CTOR </summary>
         /// <param name="startupImage"></param>
         internal SplashForm(Image startupImage)
         {
            Debug.Assert(startupImage != null);

            BackgroundImage = startupImage;
            ControlBox = false;
            Text = "StartupScreen";
            FormBorderStyle = FormBorderStyle.None;
            ShowIcon = false;
            ShowInTaskbar = false;
            Width = BackgroundImage.Width;
            Height = BackgroundImage.Height;

            StartPosition = FormStartPosition.CenterScreen;
         }
      }
   }

   /// <summary>
   /// 
   /// </summary>
   internal class MainForm : Form
   {
      /// <summary>
      /// 
      /// </summary>
      internal MainForm()
      {
         Width = 500;
         Height = 50;
         FormBorderStyle = FormBorderStyle.FixedSingle;
         Top = Left = -50000;
         StartPosition = FormStartPosition.Manual;
         this.ControlBox = false;
         Text = "gui.low.MainForm";
         BackColor = System.Drawing.Color.Red;
         this.Activated += new EventHandler(Form1_Activated);

         // Visible should be true, when we call CreateControl() explicitely.
         Visible = true;
         CreateControl();
         Visible = false;
      }

      // need to redo the assignments. in some machines , the main form will not disapear without it.
      void Form1_Activated(object sender, EventArgs e)
      {
         Width = 0;
         Top = Left = -50000;
      }
   }

#else
   /// <summary>
   /// compact framework version
   /// </summary>
   internal sealed class GUIMain : GUIMainBase
   {
      private static GUIMain _instance;
      private bool _mainFormInUse;

      internal bool FormsEnabled { get { return true; } }

      /// <summary> </summary>
      private GUIMain()
      {
         _mainFormInUse = false;
      }

      /// <summary> returns the single instance of this class</summary>
      internal static GUIMain getInstance()
      {
         if (_instance == null)
         {
            lock (typeof(GUIMain))
            {
               if (_instance == null)
                  _instance = new GUIMain();
            }
         }
         return _instance;
      }

      /// <summary>property: is the main (first) form in use by a running task</summary>
      internal bool MainFormInUse
      {
         get
         {
            return _mainFormInUse;
         }

         private set
         {
            _mainFormInUse = value;
         }
      }

      /// <summary>startup the GUI layer</summary>
      internal override void init(String startupImageFileName)
      {
         init();
         MainForm.SetStartupImage(startupImageFileName);
      }

      /// <summary> The main message loop that reads messages from the queue and dispaches them.</summary>
      internal void messageLoop()
      {
         Debug.Assert(Misc.IsGuiThread());
         lock (this)
         {
            Application.Run(MainForm);
         }
      }

      /// <summary>Dispose all the open forms.</summary>
      internal void disposeAllForms()
      {
         /// TODO disposeAllForms()
      }

      /// <summary>Break the message loop and exit the thread</summary>
      internal void Exit()
      {
         if (ClientManager.Instance.getDisplayStatisticInfo())
         {
            // write to the internal log file
            var sessionStatistics = new SessionStatisticsForm();
            sessionStatistics.WriteToInternalLog();
         }
         Application.Exit();
      }
   }
#endif

   /// <summary>
   ///   main GUI class that implements the message loop and creates the Display object
   /// </summary>
   public abstract class GUIMainBase
   {
      ///The main form of the Application Context.
      internal MainForm MainForm { get; private set; }

      /// <summary>
      ///   property: is the current object initialized
      /// </summary>
      internal bool Initialized { get; private set; }

      abstract internal void init(String startupImageFileName);

      /// <summary>
      ///   initializes this static object
      /// </summary>
      protected void init()
      {
         Debug.Assert(!Initialized && Misc.IsGuiThread());

         MainForm = new MainForm();
         Initialized = true;
      }

      /// <summary>
      ///   Execute the method in the context of the GUI thread and return without waiting for the execution to finish
      /// </summary>
      /// <param name = "method">a delegate to a method that takes no parameters</param>
      internal void beginInvoke(Delegate method)
      {
         Debug.Assert(Initialized);

         if (!MainForm.IsDisposed)
            MainForm.BeginInvoke(method);
      }

      /// <summary>
      ///   Execute the method in the context of the GUI thread and wait for its execution to finish
      /// </summary>
      /// <param name = "method">a delegate to a method that takes no parameters</param>
      internal void invoke(Delegate method)
      {
         Debug.Assert(Initialized);

         if (!MainForm.IsDisposed)
            MainForm.Invoke(method);
      }
   }
}
