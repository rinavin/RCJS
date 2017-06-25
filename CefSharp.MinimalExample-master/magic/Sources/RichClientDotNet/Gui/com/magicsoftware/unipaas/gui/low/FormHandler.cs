using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using com.magicsoftware.controls;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using com.magicsoftware.win32;
#if !PocketPC
using System.Diagnostics;
#else
using com.magicsoftware.richclient;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Class implements handlers for the shells, the class is a singleton
   /// </summary>
   /// <author>  rinav</author>
   class FormHandler : HandlerBase
   {
      private static FormHandler _instance;
      internal static FormHandler getInstance()
      {
         if (_instance == null)
            _instance = new FormHandler();
         return _instance;
      }

      private FormHandler()
      {
      }

      /// <summary>
      /// Adds closing handler to the form. Since a .NET control may hook closing event of its container form,
      /// this handler should be added after creating all controls in the form so that the handler will be
      /// the last one to execute.
      /// </summary>
      /// <param name="control">form to which handlers are attached.</param>
      internal void addClosingHandler(GuiForm form)
      {
         form.Closing += ClosingHandler;
      }

      /// <summary>
      /// Register Dectivate Event Handler.
      /// </summary>
      /// <param name="form"></param>
      internal void addDeActivatedHandler(GuiForm form)
      {
         form.Deactivate += DeActivatedHandler;
      }

      /// <summary>
      /// Register Activate Event Handler for Main Program Form.
      /// </summary>
      /// <param name="form"></param>
      internal void addMainProgramMDIFormActivatedHandler(GuiForm form)
      {
         // Avoid getting the form to foreground (hiding other MDI children)
         form.Activated += sendToBack;
      }

      /// <summary>
      // Event handler for sending the form to backgroud
      /// </summary>
      void sendToBack(object sender, EventArgs e)
      {
         OldZorderManager.getInstance().DisableSpecialZorderSetting = true;
         ((Form)sender).SendToBack();
         OldZorderManager.getInstance().DisableSpecialZorderSetting = false;
         OnFormActivate(((Form)sender));
        
      }

      /// <summary>
      /// adds events for form
      /// </summary>
      /// <param name="form">gui form to which events are attached.</param>
      /// <param name="isHelpWindow">flag indicating window/form is help window or not.</param>
      internal void addHandler(GuiForm form)
      {
         form.Activated += ActivatedHandler;
         form.MouseDown += MouseDownHandler;
         form.MouseUp += MouseUpHandler;
         form.Closed += ClosedHandler;

#if PocketPC
         form.Resize += ResizeHandler;
#endif
         form.Disposed += DisposedHandler;
         form.KeyDown += KeyDownHandler;
         form.GotFocus += GotFocusHandler;

#if !PocketPC
         form.Shown += ShownHandler;
         form.ResizeBegin += ResizeBeginHandler;
         form.ResizeEnd += ResizeEndHandler;
         form.Sizing += SizingHandler;
         form.Move += MoveHandler;
         form.Layout += LayoutHandler;
         form.MdiChildActivate += MdiChildActivatedHandler;
         form.DragOver += DragOverHandler;
         form.DragDrop += DragDropHandler;
         form.GiveFeedback += GiveFeedBackHandler;
         form.NCMouseDown += NCMouseDownHandler;
         form.NCActivate += NCActivateHandler;
         form.CopyData += CopyDataHandler;
         form.CanReposition += CanRepositionHandler;
         form.KeyPress += KeyPressHandler;
#endif
         form.Load += LoadHandler;
         form.WMActivate += WMActivateHandler;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         Control clientPanel;
         MapData mapData;
         GuiMgForm guiMgForm;

         // When modal window is opened and if we close the form Modal form using external event (i.e. Stop RTE from studio / Exit System event)
         // We are getting closed event 2 times for a modal window : 
         //          1) First time from GuiCommandsQueue.closeForm() due to form.close() and
         //          2) We are not able to figure out from where we are getting the second closed event.
         // When we come here to process closed event second time the object is already disposed, hence we should not process any events.
         if (GuiUtils.isDisposed((Control)sender))
            return;

         clientPanel = ((TagData)((Control)sender).Tag).ClientPanel;
         if (clientPanel == null)
            clientPanel = (Control)sender;
         mapData = ControlsMap.getInstance().getMapData(clientPanel);
         guiMgForm = mapData.getForm();
         GuiForm form = (GuiForm)sender;


         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgForm));
         try
         {
            switch (type)
            {
               case EventType.LOAD:
#if !PocketPC
                  // #919192: Icon displayed for an maximised MDI Child is not the one set in 
                  // form's Icon property before loading the form.
                  // This is a framework bug. The workaround is to set the icon for Maximised MDI Child again in load handler.
                  if (form.IsMdiChild && form.WindowState == FormWindowState.Maximized)
                  {
                     Icon originalIcon = form.Icon;
                     form.Icon = null;
                     form.Icon = originalIcon;
                  }
                  ContextForms.AddForm(form);
#endif
                  form.Activate();
                  break;

               case EventType.CAN_REPOSITION:

                  if (form.IsMdiChild && OldZorderManager.getInstance().UseOldZorderAlgorithm)
                  {
                     if (!ContextForms.IsLastForm(form))
                     { 
                        GuiForm nextForm = ContextForms.GetNextForm(form);
                        if (nextForm.Handle != ((RepositionEventArgs)e).HwndInsertAfter)
                           ((RepositionEventArgs)e).CanReposition = false;
                     }
                  }
                  break;

               case EventType.ACTIVATED:
                  //ClientManager.Instance.RefreshMenu(mgForm.getTask().getMgdID());
                  OnFormActivate(form);
                  //Defect 124155 - if form is ancestor to blocking batch form - return activation to the batch
                  if (form.IsMdiChild && ContextForms.IsBlockedByMdiForm(form))
                  {
                     GuiForm formToActivate = ContextForms.GetBlockingFormToActivate(form);
                     if (formToActivate != null && !formToActivate.IsClosing)
                        formToActivate.Activate();

                  }
                 
                  break;
               case EventType.WMACTIVATE:
                  OnWmActivate(form, e);

                  break;

#if !PocketPC
               case EventType.SHOWN:
                  if (form.WindowState == FormWindowState.Normal &&
                     ((TagData)form.Tag).WindowType != WindowType.FitToMdi &&
                     ((TagData)form.Tag).WindowType != WindowType.Sdi &&
                     ((TagData)form.Tag).WindowType != WindowType.MdiFrame)
                  {
                     Rectangle? savedbounds = GuiUtils.getSavedBounds(form);
                     if (savedbounds != null)
                     {
                        Rectangle rect = (Rectangle)savedbounds;

                        if (rect.Size != form.ClientSize)
                           GuiUtils.setBounds(form, rect);
                     }
                  }

                  GuiUtils.saveFormBounds(form);
                  form.Resize += FormHandler.getInstance().ResizeHandler;

                  // form is shown, so set the flag as false
                  PrintPreviewFocusManager.GetInstance().IsInModalFormOpening = false;
                  ((TagData)form.Tag).IsShown = true;
                  break;
#endif
               case EventType.MDI_CHILD_ACTIVATED:
                 
                  Events.OnFormActivate(guiMgForm);
                 

                  break;

               case EventType.RESIZE:
                  if (((TagData)form.Tag).IgnoreWindowResizeAndMove)
                     return;
                  onResize(form, guiMgForm);
                  break;

               case EventType.RESIZE_BEGIN:
                  OnResizeBegin(form);
                  break;

               case EventType.RESIZE_END:
                  OnResizeEnd(form);
                  break;

               case EventType.LAYOUT:
#if !PocketPC
                  if (GuiUtils.IsFormMinimized(form))
                     ((TagData)form.Tag).Minimized = true;
                  else if (!((TagData)form.Tag).IsShown)
                        ((TagData)form.Tag).Minimized = false;
#endif
                  if (((TagData)form.Tag).WindowType == WindowType.Sdi)
                     SDIFormLayout(form);
                  return;

               case EventType.SIZING:
                  OnSizing(form, (SizingEventArgs)e);
                  break;

#if !PocketPC //tmp
               
               case EventType.COPY_DATA:
                  Events.OnCopyData(guiMgForm, ((CopyDataEventArgs)e).Copydata);
                  return;

               case EventType.MOVE:
                  if (((TagData)form.Tag).WindowType == WindowType.ChildWindow)
                  {
                     
                     Control parent = form.Parent;

                      
                     Debug.Assert(parent is Panel);
                     Form parentform = GuiUtils.FindForm(parent);
                     if (GuiUtils.IsFormMinimized(parentform))
                        return;

                     EditorSupportingPlacementLayout placementLayout = ((TagData)parent.Tag).PlacementLayout;
                     if (placementLayout != null)
                     {
                        //TODO: If the child window is moved due to scrolling of the parent window,
                        //computeAndUpdateLogicalSize() should not be called.
                        placementLayout.computeAndUpdateLogicalSize(parent);
                     }
                  }

                  if (((TagData)form.Tag).IgnoreWindowResizeAndMove)
                     return;
                  onMove(form, guiMgForm);
                  break;
#endif
               case EventType.CLOSING:
                  //handle the event only if it was not canceled.
                  if (((CancelEventArgs)e).Cancel == false)
                  {
                     bool clrHandledEvent = false;
#if !PocketPC 
                     //When MDI Frame is closing, We should not put ACT_EXIT on each it's child windows.
                     //This causes invokation of confirmation dialog, which should be avoided.
                     WindowType windowType = ((TagData)form.Tag).WindowType;
                     if (((FormClosingEventArgs)e).CloseReason == System.Windows.Forms.CloseReason.MdiFormClosing
                        && (windowType == WindowType.MdiChild || windowType == WindowType.FitToMdi))
                        return;
#endif
                     clrHandledEvent = Events.OnFormClose(guiMgForm);


#if !PocketPC //tmp
                     // If CloseReason is UserClosing, then only set Cancel.
                     if (((FormClosingEventArgs)e).CloseReason == System.Windows.Forms.CloseReason.UserClosing)
#endif
                        //If clrHandledEvent is true then 'Cancel' should be false else true.
                        ((CancelEventArgs)e).Cancel = !clrHandledEvent;
                  }
                  return;

               case EventType.CLOSED:
#if PocketPC
                    GUIMain.getInstance().MainForm.closeSoftKeyboard();
#endif
                  break;

               case EventType.NCMOUSE_DOWN:
                  if (!IsClickOnCloseButton((NCMouseEventArgs)e))
                  {
#if !PocketPC
                     // QCR #414516. Click on title bar mustn't move cursor to the parent task.
                     Form previousActiveForm = Form.ActiveForm ?? lastActiveTopLevelForm;

                     //defect 120508 : if the form was already active - we should not process the mouse down
                     if (GuiUtils.FindTopLevelForm(form) != previousActiveForm)

#endif
                        Events.OnMouseDown(guiMgForm, null, null, true, 0, true, true);

                  }
                  break;

               case EventType.NCACTIVATE:
                  Events.OnNCActivate(guiMgForm);
                  break;

               case EventType.DISPOSED:
                  Events.OnDispose(guiMgForm);
                  ContextForms.RemoveForm(form);

                  clientPanel.Tag = null;
                  form.Tag = null;

                  return;

               case EventType.DEACTIVATED:
                  Events.OnCloseHelp(guiMgForm);
                  break;
#if PocketPC
                case EventType.KEY_DOWN:
                    // Key event preview - KeyDown with 'tab' key is usually used by the system to move the focus 
                    // between controls. We want to do it ourselves, so we intercept it here and pass it to the control 
                    // in focus.
                    if (((KeyEventArgs)e).KeyCode == Keys.Tab)
                    {
                        // get the tagdata and look for the control that has the focus
                        TagData tagData = (TagData)((Control)sender).Tag;

                        // If the control is one of those for which we need to raise the event, aise it and mark the 
                        // event as handled.
                        if (tagData.LastFocusedControl is MgTextBox)
                        {
                            ((MgTextBox)tagData.LastFocusedControl).CallKeyDown((KeyEventArgs)e);
                            ((KeyEventArgs)e).Handled = true;
                        }
                        else if (tagData.LastFocusedControl is MgCheckBox)
                        {
                            ((MgCheckBox)tagData.LastFocusedControl).CallKeyDown((KeyEventArgs)e);
                            ((KeyEventArgs)e).Handled = true;
                        }
                        else if (tagData.LastFocusedControl is MgComboBox)
                        {
                            ((MgComboBox)tagData.LastFocusedControl).CallKeyDown((KeyEventArgs)e);
                            ((KeyEventArgs)e).Handled = true;
                        }
                        else if (tagData.LastFocusedControl is MgButtonBase)
                        {
                            ((MgButtonBase)tagData.LastFocusedControl).CallKeyDown((KeyEventArgs)e);
                            ((KeyEventArgs)e).Handled = true;
                        }
                        else if (tagData.LastFocusedControl is MgTabControl)
                        {
                            ((MgTabControl)tagData.LastFocusedControl).CallKeyDown((KeyEventArgs)e);
                            ((KeyEventArgs)e).Handled = true;
                        }
                    }
                    return;
#endif
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }
         DefaultHandler.getInstance().handleEvent(type, sender, e, mapData);
      }
#if !PocketPC
      static Form lastActiveTopLevelForm = null;
#endif
      private void OnResizeEnd(Form form)
      {
         List<Form> forms;
         forms = GetFormAndMaximizedMdiChildren(form);
         foreach (var item in forms)
         {
#if !PocketPC
            ((TagData)item.Tag).IgnoreWindowResizeAndMove = false;
            //Once the form resizing is over, handle the resize of all the 
            //table controls on the form.
            //Note that resizing of table control was ignored while handling 
            //LAYOUT event in TableHandler.
            TagData tagData = ((TagData)item.Tag);
            foreach (TableControl tableControl in tagData.TableControls)
               GuiUtils.getTableManager(tableControl).resize();
#endif
            onWindowResizeEnd(item);
         }
      }

      /// <summary>
      /// invoked on resize begin event
      /// </summary>
      /// <param name="form"></param>
      private void OnResizeBegin(Form form)
      {
         List<Form> forms = GetFormAndMaximizedMdiChildren(form);
         foreach (var item in forms)
         {
            //calulate correct rectangle after dragging considering minimum height/width from mdi children
            ((TagData)item.Tag).IgnoreWindowResizeAndMove = true;
         }

      }

      /// <summary> Handle form activation. </summary>
      /// <param name="form">the form being activated.</param>
      internal void OnFormActivate(Form form)
      {
#if PocketPC
         // if forms were hidden, we need to show all the forms
         if(!form.Visible)
            GUIManager.Instance.restoreHiddenForms();
#else
         if (PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo)
         {
            PrintPreviewFocusManager.GetInstance().ShouldPrintPreviewBeFocused = false;
            PrintPreviewFocusManager.GetInstance().PrintPreviewFormHandle = IntPtr.Zero;
         }

         // #943264 & 942768. Fixed a .net Framework issue.
         // Suppose, we have a window with a user control (with a child control inside it).
         // When parking on this child control, we open another window.
         // Now, if we close the new window, the focus should be back on the last
         // focused control i.e. the child control in this case.
         // But, this happens only if the widows are opened outside the MDI frame.
         // If they are opened inside the MDI frame, the focus is not set on the child control.
         // So, we need to explicitly set the focus on the last focused control.
         Form activeForm = ((TagData)form.Tag).ActiveChildWindow ?? form;

         if (activeForm.IsMdiContainer)
         {
            Form activeMDIChild = GuiUtils.GetActiveMDIChild(activeForm);
            if (activeMDIChild != null)
               activeForm = activeMDIChild;
         }

         GuiUtils.restoreFocus(activeForm);
         lastActiveTopLevelForm = GuiUtils.FindTopLevelForm(activeForm);

            Control clientPanel = ((TagData)form.Tag).ClientPanel;
            if (((TagData)form.Tag).IsMDIClientForm)
               clientPanel = ((TagData)form.MdiParent.Tag).ClientPanel;


            MapData mapData = ControlsMap.getInstance().getMapData(clientPanel);
            Events.OnFormActivate(mapData.getForm());

            if (PrintPreviewFocusManager.GetInstance().IsInModalFormOpening)
            {
               PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = true;
            }
#endif
      }

      private void OnWmActivate(GuiForm form, EventArgs e)
      {
         GuiForm lastForm = lastActiveTopLevelForm as GuiForm;
         System.TimeSpan timeFromActivation = DateTime.Now - form.ActivateAppTime;
         System.TimeSpan timeFromMouseActivation = DateTime.Now - form.MouseActivateAppTime;
         ActivateArgs args = (ActivateArgs)e;
         if (timeFromActivation.TotalMilliseconds < 200 && args.WmParam == NativeWindowCommon.MA_ACTIVATE && (timeFromMouseActivation.Milliseconds > 200)) // we are moving from another application
                                                                                                                                                           //there was no click on the application

            if (lastForm != null && lastForm != form && !lastForm.IsClosing &&
               !ContextForms.IsLastForm(form) && //new from is opening
               ContextForms.BelongsToCurrentContext(lastForm)) //we are in the same context
            {
               if (!lastForm.IsMdiChild && !lastForm.IsMdiContainer)
               {//the solution is not for MDI windows
                  args.StopActivation = true;
                  lastForm.Activate();
               }
            }
      }

      /// <summary>
      /// return true if it is left click on close button 
      /// </summary>
      /// <param name="nCMouseEventArgs"></param>
      /// <returns></returns>
      private bool IsClickOnCloseButton(NCMouseEventArgs nCMouseEventArgs)
      {
         return (nCMouseEventArgs.HitTest == NativeWindowCommon.HTCLOSE && nCMouseEventArgs.Button == MouseButtons.Left);
      }
      /// <summary>
      /// handle WM_SIZING to support correct minimun windth/height behavior
      /// origin : OnSizing from main_menu.cpp( for mdi frame), gui.cpp (for form itself)
      /// </summary>
      /// <param name="form"></param>
      /// <param name="se"></param>
      private static void OnSizing(Form form, SizingEventArgs se)
      {
#if !PocketPC
         NativeWindowCommon.RECT rect = se.DragRect;
         if (form.IsMdiContainer)
         {
            //calulate correct rectangle after dragging considering minimum height/width from mdi children
            foreach (var item in form.MdiChildren)
            {

               if (GuiUtils.isMaximizedInMDIFrame(item)) //this chid affects minimum height/width
                  GuiUtils.UpdateByMinimun(se.SizingEdge, ref rect, item);

            }
         }

         //consider minimin height/width of mdi frame itself

         GuiUtils.UpdateByMinimun(se.SizingEdge, ref rect, form);
         se.DragRect = rect;
#endif
      }

      /// <summary> </summary>
      /// <param name="form"></param>
      /// <param name="mgForm"></param>
      private void onWindowResizeEnd(Form form)
      {
         // ((TagData)form.Tag).Bounds saves X and Y with respect to the Form,
         // but Width and Height are with respect to Client area. So, compare them
         // accordingly.
         Point location = form.Location;
         Size size = form.ClientSize;
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getFormMapData(form);

         GuiMgForm mgForm = mapData.getForm();

         Rectangle ?prevRectPtr = GuiUtils.getSavedBounds(form);
         Rectangle prevRect = Rectangle.Empty;

         if (prevRectPtr != null)
            prevRect = (Rectangle)prevRectPtr;

         if (location.X != prevRect.X || location.Y != prevRect.Y)
            onMove(form, mgForm);
         if (size.Width != prevRect.Width || size.Height != prevRect.Height)
            onResize(form, mgForm);
      }

      /// <summary>
      /// resize form
      /// </summary>
      /// <param name="form"></param>
      /// <param name="mgForm"></param>
      private void onResize(Form form, GuiMgForm guiMgForm)
      {
#if !PocketPC
         GuiUtils.saveFormBounds(form);
         if (GuiUtils.getActiveForm() == form)
         {
            ((GuiForm)form).RaiseMouseDownIfNeeded();
            Events.OnWindowResize(guiMgForm);
         }

         if (!GuiUtils. IsFormMinimized(form))
         {
            if (((TagData)form.Tag).Minimized)
            {
               ((TagData)form.Tag).Minimized = false;
               form.PerformLayout(form, "Size");
            }
         }
#else
         // Perform the layout now, as we don't get the layout event.
         if (((TagData)form.Tag).WindowType == WindowType.Sdi)
           SDIFormLayout(form);
#endif
      }

      /// <summary> Move form</summary>
      /// <param name="form"></param>
      /// <param name="mgForm"></param>
      private void onMove(Form form, GuiMgForm mgForm)
      {
#if !PocketPC
         ((GuiForm)form).RaiseMouseDownIfNeeded();
#endif
         GuiUtils.saveFormBounds(form);
         if (GuiUtils.getActiveForm() == form)
            Events.OnWindowMove(mgForm);
      }

      /// <summary>set the bounds of the SDI Client Area Panel</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal static void SDIFormLayout(object sender)
      {
         Form form = (Form)sender;
         TagData td = (TagData)form.Tag;

         if (((TagData)form.Tag).Minimized)
            return;

         if (td.ClientPanel != null)
         {
            int clientHeight = form.ClientSize.Height;
            // reduce the height of all the controls (except of the SDI Client Area Panel) from the client area height
            foreach (Control control in form.Controls)
            {
               if (((TagData)control.Tag) != null && (!((TagData)control.Tag).IsClientPanel))
                  if (((TagData)control.Tag).Visible)
                     clientHeight -= control.Height;
            }
            td.ClientPanel.Height = clientHeight;
         }
      }

      /// <summary>
      /// return list which includes:
      /// form
      /// mdi children of the form
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal List<Form> GetFormAndMaximizedMdiChildren(Form form)
      {
         List<Form> list = new List<Form>();
         list.Add(form);
#if !PocketPC
         if (form.IsMdiContainer)
         {
            foreach (Form item in form.MdiChildren)
            {
               if (GuiUtils.isMaximizedInMDIFrame(item))
                  list.Add(item);
            }
         }
#endif
         return list;
      }
   }
}
