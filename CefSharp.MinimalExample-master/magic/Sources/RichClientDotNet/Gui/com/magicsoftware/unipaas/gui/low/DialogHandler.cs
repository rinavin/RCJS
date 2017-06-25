using System;
using System.Reflection;
using System.Windows.Forms;
using com.magicsoftware.util;

#if PocketPC
using com.magicsoftware.richclient.mobile.gui;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   public class DialogHandler
   {
      private Form _form;

      /// <summary>
      ///   Creates dialog for passed type of object by calling objects constructor matching the parameters
      /// </summary>
      /// <param name = "objType">type of object whose constructor to be invoked</param>
      /// <param name = "parameters">parameters to be passed to objects constructor</param>
      public void createDialog(Type objType, Object[] parameters)
      {
         if (!Misc.IsGuiThread())
         {
            // always execute dialog creation code in GUI thread
            GuiInteractive guiUtils = new GuiInteractive();
            guiUtils.createDialog(this, objType, parameters);
         }
         else
         {
            ConstructorInfo[] constructors = objType.GetConstructors();

            foreach (ConstructorInfo constructor in constructors)
            {
               // TODO : We actually need to check the parameter type also
               // but currently there is no such situation so lets not 
               // make this code more complex. 
               if (constructor.GetParameters().Length == parameters.Length)
               {
                  _form = (Form) constructor.Invoke(parameters);
                  break;
               }
            }
         }
      }

      /// <summary>
      ///   Show Dialog
      /// </summary>
      public virtual DialogResult openDialog()
      {
         DialogResult result = DialogResult.None;

         if (!Misc.IsGuiThread())
         {
            // always execute dialog opening code in GUI thread
            GuiInteractive guiUtils = new GuiInteractive();
            result = guiUtils.openDialog(this);
         }
         else
         {
#if !PocketPC
            //Create dummy form and pass it as owner, 
            //otherwise while running through F7 dialog is not displayed
            var dummyForm = GuiUtilsBase.createDummyForm();
            dummyForm.Visible = false;
            GuiCommandQueue.getInstance().GuiThreadIsAvailableToProcessCommands = false; 
            result = _form.ShowDialog(dummyForm);
            dummyForm.Close();
#else
            GuiCommandQueue.getInstance().GuiThreadIsAvailableToProcessCommands = false;
            result = _form.ShowDialog();
#endif
         }

         return result;
      }

      /// <summary>
      ///   dispose(and close) the dialog
      /// </summary>
      public virtual void closeDialog()
      {
         if (!Misc.IsGuiThread())
         {
            // always execute dialog closing code in GUI thread
            GuiInteractive guiUtils = new GuiInteractive();
            guiUtils.closeDialog(this);
         }
         else
            _form.Close();
      }
   }
}
