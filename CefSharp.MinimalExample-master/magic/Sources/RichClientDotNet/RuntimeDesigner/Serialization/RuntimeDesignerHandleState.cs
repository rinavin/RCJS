using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RuntimeDesigner.Serialization
{
   internal class RuntimeDesignerHandleState
   {

      /// <summary>
      /// save 
      /// </summary>
      Dictionary<String, String> stateBeforeOpenForm = new Dictionary<string, string>();

      /// <summary>
      /// save state
      /// </summary>
      /// <param name="fileName"></param>
      internal void SaveState(String fileName, Dictionary<object, ComponentWrapper> componentWrapperList)
      {
         DropState(fileName);

         String str = StateAsString(componentWrapperList);
         stateBeforeOpenForm.Add(fileName, str);
      }

      /// <summary>
      /// drop state
      /// </summary>
      /// <param name="fileName"></param>
      internal void DropState(String fileName)
      {
         if (stateBeforeOpenForm.ContainsKey(fileName))
            stateBeforeOpenForm.Remove(fileName);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      internal String StateAsString(Dictionary<object, ComponentWrapper> componentWrapperList)
      {
         return RuntimeDesignerSerializer.SerializeToString(componentWrapperList);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="componentWrapperList"></param>
      /// <returns></returns>
      internal bool HasBeenModified(String fileName, Dictionary<object, ComponentWrapper> componentWrapperList)
      {
         string currentState = StateAsString(componentWrapperList);
         string savedState = stateBeforeOpenForm[fileName];

         return !(savedState.Equals(currentState));
      }
    

      /// <summary>
      /// 
      /// </summary>
      /// <param name="runtimeHostSurface"></param>
      /// <param name="fileName"></param>
      /// <returns></returns>
      internal bool OnClose(RuntimeHostSurface runtimeHostSurface, String fileName)
      {
         bool cancelClose = false;

         if (HasBeenModified(fileName, runtimeHostSurface.ComponentsDictionary))
         {
            string message = RuntimeHostSurface.GetTranslatedString(runtimeHostSurface, "Save changes?");
            string title = RuntimeHostSurface.GetTranslatedString(runtimeHostSurface, "Confirmation");
            DialogResult dr = MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            switch (dr)
            {
               case DialogResult.Yes:
                  RuntimeDesignerSerializer.SerializeToFiles(runtimeHostSurface.ComponentsDictionary);
                  break;
               case DialogResult.No:
                  break;
               case DialogResult.Cancel:
                  cancelClose = true;
                  break;
            }
         }

         if (!cancelClose)
            DropState(fileName);

         return cancelClose;
      }
   }
}
