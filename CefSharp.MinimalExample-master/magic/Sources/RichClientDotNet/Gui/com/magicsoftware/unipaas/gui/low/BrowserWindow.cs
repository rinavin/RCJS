using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;

#if PocketPC
using FormClosingEventArgs = com.magicsoftware.mobilestubs.FormClosingEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// </summary>
   /// <author>SushantR</author>
   public class BrowserWindow
   {
      protected Form _form;
      protected WebBrowser _webBrowser;
      protected String _currUrl;
      protected MgRectangle _formRect;
      private BrowserInteractive _browserInteractive;

      //Functions to be overridden in the derived implementations of this class:

      protected virtual void BrowserWindowLoading_specific(object sender, EventArgs e)
      {
      }

      protected virtual void BrowserWindowClosing_specific(object sender, FormClosingEventArgs e)
      {
      }

      protected virtual void DocumentCompleted_specific(object sender, EventArgs e)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      public MgRectangle FormRect
      {
         get { return _formRect; }
         set
         {
            _formRect = value;
            if (_browserInteractive == null)
            {
               _browserInteractive = new BrowserInteractive(this);
               _browserInteractive.CreateWindow(_formRect);
            }
            else
               _browserInteractive.ResizeWindow(_formRect);
         }
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "rect">dimensions of browser window</param>
      public BrowserWindow(MgRectangle rect)
      {
         FormRect = rect;
      }

      /// <summary>
      ///   Shows content of the document located by URL inside a browser control
      /// </summary>
      /// <param name = "url">document path</param>
      /// <param name = "openAsApplicationModal">display the form modally</param>
      public void ShowContentFromURL(string url, bool openAsApplicationModal)
      {
         _browserInteractive.ShowContentFromURL(url, openAsApplicationModal);
      }

      /// <summary>
      ///   Shows content inside a browser control
      /// </summary>
      /// <param name = "content"></param>
      public void ShowContent(String content)
      {
         _browserInteractive.ShowContent(content);
      }

      /// <summary>
      ///   event handler that handles closing the help url window
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      private void BrowserWindowClosing(object sender, FormClosingEventArgs e)
      {
         _form.Text = "";
         _currUrl = null;
         _webBrowser.Stop();
         _webBrowser.DocumentText = "";
         _form.Hide();
         e.Cancel = true;

         BrowserWindowClosing_specific(sender, e);
      }

#if !PocketPC
      /// <summary>
      ///   event handler that set the windowtitle with the Url title
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      private void BrowserWindowDocumentTitleChanged(object sender, EventArgs e)
      {
         _form.Text = _webBrowser.DocumentTitle;
      }

      private void DocumentCompleted(object sender, EventArgs e)
      {
         DocumentCompleted_specific(sender, e);
      }

      private void BrowserWindowLoading(object sender, EventArgs e)
      {
         BrowserWindowLoading_specific(sender, e);
      }
#endif

      /// <summary>
      ///   Inner class that handles the Help Url Window
      /// </summary>
      private class BrowserInteractive
      {
         private readonly BrowserWindow _browserWindow;
         private COMMAND _commandType;
         private String _content;
         private MgRectangle _formRect;
         private bool _openAsApplicationModal;
         private String _url;

         /// <summary>
         ///   CTOR
         /// </summary>
         /// <param name = "browserWindowVal">reference to browser window</param>
         internal BrowserInteractive(BrowserWindow browserWindowVal)
         {
            _browserWindow = browserWindowVal;
         }

         /// <summary>
         ///   set the command to create form for browser control
         /// </summary>
         /// <param name = "formRectVal">dimensions of the form to create</param>
         internal void CreateWindow(MgRectangle formRectVal)
         {
            _commandType = COMMAND.CREATE_WINDOW;
            _formRect = formRectVal;

            GUIMain.getInstance().invoke(new BrowserInteractiveDelegate(Run));
         }

         /// <summary>
         ///   set the command to resize form of browser control
         /// </summary>
         /// <param name = "formRectVal">new dimensions of the form</param>
         internal void ResizeWindow(MgRectangle formRectVal)
         {
            _commandType = COMMAND.RESIZE_WINDOW;
            _formRect = formRectVal;

            GUIMain.getInstance().invoke(new BrowserInteractiveDelegate(Run));
         }

         /// <summary>
         ///   sets the command and values to show document from the specified url, inside a browser control 
         ///   on a separate form
         /// </summary>
         /// <param name = "url">document path</param>
         /// <param name = "openAsApplicationModal">open form modally</param>
         internal void ShowContentFromURL(string url, bool openAsApplicationModal)
         {
            _commandType = COMMAND.SHOW_CONTENT_FROM_URL;
            _url = url;
            _openAsApplicationModal = openAsApplicationModal;

            GUIMain.getInstance().invoke(new BrowserInteractiveDelegate(Run));
         }

         /// <summary>
         ///   sets the command and values to show content inside a browser control
         /// </summary>
         /// <param name = "content">content to show</param>
         internal void ShowContent(String content)
         {
            _commandType = COMMAND.SHOW_CONTENT;
            _content = content;

            GUIMain.getInstance().invoke(new BrowserInteractiveDelegate(Run));
         }

         /// <summary>
         ///   thread that takes action to the current commandType
         /// </summary>
         private void Run()
         {
            switch (_commandType)
            {
               case COMMAND.CREATE_WINDOW:
                  OnCreateWindow();
                  break;
               case COMMAND.RESIZE_WINDOW:
                  OnResizeWindow();
                  break;
               case COMMAND.SHOW_CONTENT_FROM_URL:
                  OnShowContentFromURL();
                  break;
               case COMMAND.SHOW_CONTENT:
                  OnShowContent();
                  break;
               default:
                  Debug.Assert(false);
                  break;
            }
         }

         /// <summary>
         ///   creates Form to display browser control content
         /// </summary>
         private void OnCreateWindow()
         {
            _browserWindow._form = new Form();
            _browserWindow._form.SuspendLayout();

            _browserWindow._form.Bounds = new Rectangle(_formRect.x, _formRect.y, _formRect.width, _formRect.height);
            _browserWindow._webBrowser = new WebBrowser {Dock = DockStyle.Fill};
#if !PocketPC
            _browserWindow._form.StartPosition = FormStartPosition.Manual;
            _browserWindow._form.FormClosing += _browserWindow.BrowserWindowClosing;
            _browserWindow._form.Load += _browserWindow.BrowserWindowLoading;
            _browserWindow._webBrowser.DocumentTitleChanged += _browserWindow.BrowserWindowDocumentTitleChanged;
            _browserWindow._webBrowser.DocumentCompleted += _browserWindow.DocumentCompleted;
#endif
            _browserWindow._form.Controls.Add(_browserWindow._webBrowser);

            _browserWindow._form.ResumeLayout();
         }

         /// <summary>
         ///   resize form
         /// </summary>
         private void OnResizeWindow()
         {
            _browserWindow._form.Bounds = new Rectangle(_formRect.x, _formRect.y, _formRect.width, _formRect.height);
         }

         /// <summary>
         ///   Shows content of the document located by URL inside a browser control
         /// </summary>
         private void OnShowContentFromURL()
         {
            Boolean isSameUrl = _url.Equals(_browserWindow._currUrl);
            Boolean isSameWindowType = false;

#if !PocketPC
            isSameWindowType = (_browserWindow._form.Modal == _openAsApplicationModal);
#endif

            // no need to handle url if form is visible and url is same as existing and windowtype is also same
            if (_browserWindow._form.Visible && isSameUrl && isSameWindowType)
               return;

            if (_browserWindow._webBrowser != null && !isSameUrl)
            {
               //trim the url
               _url = _url.Trim();

               try
               {
                  if (_url.StartsWith("/"))
                  {
                     // load the document through Cache Manager
                     byte[] response = Events.GetContent(_url, false);
                     String helpContent = Encoding.UTF8.GetString(response, 0, response.Length);
                     _browserWindow._webBrowser.DocumentText = helpContent;
                  }
                  else
                     // If url string doesn't contain the protocol name then prepend the protocol name to url String
                     _browserWindow._webBrowser.Navigate(Misc.createURI(_url));
               }
               catch (Exception ex)
               {
                  Events.WriteExceptionToLog(ex);
               }

               _browserWindow._currUrl = _url;
            }

            if (_browserWindow._form.Visible && !isSameWindowType)
               _browserWindow._form.Hide();

            // disable form locking mechanism to avoid disabling of browser form
            Events.AllowFormsLock(false);
            if (_openAsApplicationModal)
               _browserWindow._form.ShowDialog();
            else
               _browserWindow._form.Show();
            Events.AllowFormsLock(true);
         }

         /// <summary>
         ///   Show content inside a browser control
         /// </summary>
         private void OnShowContent()
         {
            if (_browserWindow._webBrowser != null)
               _browserWindow._webBrowser.DocumentText = _content;
            // disable form locking mechanism to avoid disabling of browser form
            Events.AllowFormsLock(false);
            _browserWindow._form.ShowDialog();
            Events.AllowFormsLock(true);
         }

         #region Nested type: BrowserInteractiveDelegate

         private delegate void BrowserInteractiveDelegate();

         #endregion

         #region Nested type: COMMAND

         private enum COMMAND
         {
            CREATE_WINDOW,
            RESIZE_WINDOW,
            SHOW_CONTENT_FROM_URL,
            SHOW_CONTENT
         }

         #endregion
      }
   }
}