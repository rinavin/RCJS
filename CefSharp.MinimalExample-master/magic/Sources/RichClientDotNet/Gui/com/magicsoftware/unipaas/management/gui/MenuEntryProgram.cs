using System;
using System.Collections.Generic;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// 
   /// </summary>
   public class MenuEntryProgram : MenuEntry
   {
      public bool CopyGlobalParameters { get; internal set; } // Global parameters should be copied or not (applicable for paralle progs) 
      public List<String> MainProgVars { get; internal set; } // argument from the main program
      public String Description { get; internal set; }
      public char Flow { get; internal set; } // RC / batch / BC / on-line
      public int Comp { get; set; } // program's component
      public int Idx { get; set; } // program index
      public String PublicName { get; internal set; } // internal name of the called program
      internal String Prompt { get; set; } // prompt
      public SrcContext SourceContext { get; internal set; } // Main/Current
      public int ReturnCtxIdVee { get; internal set; } // Main Prg var to receive ctx id
      public bool IsParallel { get; set; } // Is Program parallel.
      public int ProgramIsn { get; set; } // program ISN, to be used in offline mode
      public int CtlIndex { get; set; } // CTL index, to be used in offline mode

      public enum SrcContext
      {
         Main = 'M',
         Current = 'C'
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgMenu"></param>
      public MenuEntryProgram(MgMenu mgMenu) : base(MenuType.PROGRAM, mgMenu)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="isModal"></param>
      /// <param name="mainContextIsModal"></param>
      internal override bool ShouldSetModal (bool isModal, bool mainContextIsModal)
      {
         bool setModal = true;
         if (IsParallel)
            setModal = false;
         else
         {
            if (isModal)
            {
               // If MainContext is not Modal(i.e. batch is not running in MainContext), then MenuEntryProgram should not be set to Modal.
               if (SourceContext == SrcContext.Main)
               {
                  if (!mainContextIsModal)
                     setModal = false;
               }
            }
         }
         return setModal;
      }
   }
}