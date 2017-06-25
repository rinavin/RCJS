using System;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

#if PocketPC
using com.magicsoftware.mobilestubs;
#endif

namespace com.magicsoftware.httpclient
{
   /// <summary>
   /// Represents a special kind of BrowserWindow, a browser control displaying authentication forms,
   /// and allowing the end user to authenticate with a security server (e.g. RSA).
   /// Uses the object deriving from ISpecialAuthenticationHandler 
   /// (stored in property SpecialAuthenticationHandler) to analyze server responses and determine whether
   /// the authentication was in fact given or denied.
   /// </summary>
   internal class AuthenticationBrowserWindow : BrowserWindow
   {
      //describes the current state of this browser window
      private bool isAuthHandlerBrowserWindowOpen = false;

      private const string AUTHENTICATION_FORM_STATE_ID = "AuthenticationForm";

      //unless null, this handler for special request authentications is activated on each HTML document completion
      internal ISpecialAuthenticationHandler SpecialAuthenticationHandler { get; set; }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="rect">MgRectangle object describing the window boundaries</param>
      internal AuthenticationBrowserWindow(MgRectangle rect) : base(rect)
      {
      }

      /// <summary>
      /// Called by BrowserWindowLoading event handler, and handles the event raised by WebBrowser
      /// </summary>
      /// <param name="sender">WebBrowser</param>
      /// <param name="e"></param>
      protected override void BrowserWindowLoading_specific(object sender, EventArgs e)
      {
         Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowLoading_specific() started");

         isAuthHandlerBrowserWindowOpen = true;

         MgRectangle authFormRect = FormUserState.GetInstance().GetFormBounds(AUTHENTICATION_FORM_STATE_ID);
         if (authFormRect != null && !authFormRect.IsEmpty)
         {
            Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowLoading_specific(): read form bounds. X = " + authFormRect.x + ", Y = " + authFormRect.y + ", Width X Height " + authFormRect.width + "X" + authFormRect.height);
            FormRect = authFormRect;
         }
         else
            Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowLoading_specific(): could not read form bounds. Using the default values.");
         //else, e.g. the first time, the window won't be resized

         Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowLoading_specific() finished");
      }

      /// <summary>
      /// Called by BrowserWindowClosing event handler, and handles the event raised by WebBrowser
      /// </summary>
      /// <param name="sender">WebBrowser</param>
      /// <param name="e">FormClosingEventArgs</param>
      protected override void BrowserWindowClosing_specific(object sender, FormClosingEventArgs e)
      {
         if (isAuthHandlerBrowserWindowOpen)
         {
            Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowClosing_specific() started");
            try
            {
               FormUserState.GetInstance().SaveFormBounds(AUTHENTICATION_FORM_STATE_ID, ((Form)sender).Bounds);
            }
            catch (System.Exception ex)
            {
               Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowClosing_specific() EXCEPTION: " + ex.Message);
            }
            finally
            {
               isAuthHandlerBrowserWindowOpen = false;
               Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.BrowserWindowClosing_specific() finished");
            }
         }
      }

      /// <summary>
      /// Called by DocumentCompleted event handler, and handles the event raised by WebBrowser
      /// </summary>
      /// <param name="sender">WebBrowser</param>
      /// <param name="e">WebBrowserDocumentCompletedEventArgs</param>
      protected override void DocumentCompleted_specific(object sender, EventArgs e)
      {
         if (isAuthHandlerBrowserWindowOpen)
         {
            Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.DocumentCompleted_specific() started");

            //The main frame has finished loading
            if (SpecialAuthenticationHandler != null && ((WebBrowserDocumentCompletedEventArgs)e).Url == ((WebBrowser)sender).Url)
            {
               Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.DocumentCompleted_specific(): main frame loaded");

               if (SpecialAuthenticationHandler.IsPermissionGranted(sender))
               {
                  Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.DocumentCompleted_specific(): permission was granted");
                  _form.Close();
               }
               else
                  Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.DocumentCompleted_specific(): permission was NOT granted");
            }

            Logger.Instance.WriteServerToLog("AuthenticationBrowserWindow.DocumentCompleted_specific() finished");
         }
      }

   }
}
