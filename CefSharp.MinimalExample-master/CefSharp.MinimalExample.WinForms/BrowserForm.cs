﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Windows.Forms;
using CefSharp.MinimalExample.WinForms.Controls;
using CefSharp.WinForms;
using System.Threading.Tasks;
using System.Diagnostics;
//using static CefSharp.MinimalExample.WinForms.BrowserForm.BoundObject;

namespace CefSharp.MinimalExample.WinForms
{
   public partial class BrowserForm : Form
   {
      private readonly ChromiumWebBrowser browser;

      public BrowserForm()
      {
         InitializeComponent();

         Text = "CefSharp";
         WindowState = FormWindowState.Maximized;

         browser = new ChromiumWebBrowser("www.google.com")
         {
            Dock = DockStyle.Fill,
         };
         toolStripContainer.ContentPanel.Controls.Add(browser);

         browser.LoadingStateChanged += OnLoadingStateChanged;
         browser.ConsoleMessage += OnBrowserConsoleMessage;
         browser.StatusMessage += OnBrowserStatusMessage;
         browser.TitleChanged += OnBrowserTitleChanged;
         browser.AddressChanged += OnBrowserAddressChanged;
         browser.LoadingStateChanged += (sender, args) =>
         {
               //Wait for the Page to finish loading
               if (args.IsLoading == false)
            {
              // browser.ExecuteScriptAsync("alert('All Resources Have Loaded');");
            }
         };
         browser.RegisterAsyncJsObject("boundAsync", new BoundObject.AsyncBoundObject()); //Standard object rego
                                                                                          //browser.RegisterJsObject("bound", new BoundObject()); //Standard object registration
         browser.RegisterJsObject("bound", new BoundObject(), BindingOptions.DefaultBinder); //Use the default binder to serialize values into complex objects
                                                                                             //browser.RegisterJsObject("bound", new BoundObject(), new BindingOptions { CamelCaseJavascriptNames = false, Binder = new MyCustomBinder() }); //No camelcase of names and specify a default binder
         browser.RegisterJsObject("bound1", new BoundObject1(), BindingOptions.DefaultBinder); //Use the default binder to serialize values into complex objects




         var bitness = Environment.Is64BitProcess ? "x64" : "x86";
         var version = String.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}, Environment: {3}", Cef.ChromiumVersion, Cef.CefVersion, Cef.CefSharpVersion, bitness);
         DisplayOutput(version);
      }


      public class BoundObject
      {
         public class AsyncBoundObject
         {
            //We expect an exception here, so tell VS to ignore
            // [DebuggerHidden]
            public void Error()
            {
               throw new Exception("This is an exception coming from C#");
            }

            //We expect an exception here, so tell VS to ignore
            // [DebuggerHidden]
            public int Div(int divident, int divisor)
            {
               return divident + divisor;
            }
         }
      }
      public class BoundObject1
      {
         public string MyProperty { get; set; }
         public void MyMethod()
         {
            // Do something really cool here.
         }

         public void TestCallback(IJavascriptCallback javascriptCallback)
         {
            const int taskDelay = 1500;

            Task.Run(async () =>
            {
            await Task.Delay(taskDelay);

            using (javascriptCallback)
            {
               //NOTE: Classes are not supported, simple structs are
               //var response = new CallbackResponseStruct("This callback from C# was delayed " + taskDelay + "ms");
               var response = "This callback from C# was delayed " + taskDelay + "ms";
                  await javascriptCallback.ExecuteAsync(response);
               }
            });
         }
      }



      private void OnBrowserConsoleMessage(object sender, ConsoleMessageEventArgs args)
      {
         DisplayOutput(string.Format("Line: {0}, Source: {1}, Message: {2}", args.Line, args.Source, args.Message));
      }

      private void OnBrowserStatusMessage(object sender, StatusMessageEventArgs args)
      {
         this.InvokeOnUiThreadIfRequired(() => statusLabel.Text = args.Value);
      }

      private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
      {
         SetCanGoBack(args.CanGoBack);
         SetCanGoForward(args.CanGoForward);

         this.InvokeOnUiThreadIfRequired(() => SetIsLoading(!args.CanReload));
      }

      private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
      {
         this.InvokeOnUiThreadIfRequired(() => Text = args.Title);
      }

      private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs args)
      {
         this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = args.Address);
      }

      private void SetCanGoBack(bool canGoBack)
      {
         this.InvokeOnUiThreadIfRequired(() => backButton.Enabled = canGoBack);
      }

      private void SetCanGoForward(bool canGoForward)
      {
         this.InvokeOnUiThreadIfRequired(() => forwardButton.Enabled = canGoForward);
      }

      private void SetIsLoading(bool isLoading)
      {
         goButton.Text = isLoading ?
             "Stop" :
             "Go";
         goButton.Image = isLoading ?
             Properties.Resources.nav_plain_red :
             Properties.Resources.nav_plain_green;

         HandleToolStripLayout();
      }

      public void DisplayOutput(string output)
      {
         this.InvokeOnUiThreadIfRequired(() => outputLabel.Text = output);
      }

      private void HandleToolStripLayout(object sender, LayoutEventArgs e)
      {
         HandleToolStripLayout();
      }

      private void HandleToolStripLayout()
      {
         var width = toolStrip1.Width;
         foreach (ToolStripItem item in toolStrip1.Items)
         {
            if (item != urlTextBox)
            {
               width -= item.Width - item.Margin.Horizontal;
            }
         }
         urlTextBox.Width = Math.Max(0, width - urlTextBox.Margin.Horizontal - 18);
      }

      private void ExitMenuItemClick(object sender, EventArgs e)
      {
         browser.Dispose();
         Cef.Shutdown();
         Close();
      }

      private void GoButtonClick(object sender, EventArgs e)
      {
         LoadUrl(urlTextBox.Text);
      }

      private void BackButtonClick(object sender, EventArgs e)
      {
         browser.Back();
      }

      private void ForwardButtonClick(object sender, EventArgs e)
      {
         browser.Forward();
      }

      private void UrlTextBoxKeyUp(object sender, KeyEventArgs e)
      {
         if (e.KeyCode != Keys.Enter)
         {
            return;
         }

         LoadUrl(urlTextBox.Text);
      }

      private void LoadUrl(string url)
      {
         if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
         {
            browser.Load(url);
         }
      }
   }
}
