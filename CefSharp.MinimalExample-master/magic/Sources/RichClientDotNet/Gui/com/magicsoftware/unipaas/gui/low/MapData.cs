using System;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   The class is used for identifications of controls from widgets. It will be set/get from widget using
   ///   setData()/getData() methods
   /// </summary>
   /// <author>  rinav</author>
   internal class MapData
   {
      private readonly MenuReference _menuRef; // for menus
      private readonly GuiMgControl _control; // for controls
      private readonly GuiMgForm _form; // for forms
      private int _idx; // for multiline controls

      /// <summary>
      ///   Form constructor
      /// </summary>
      internal MapData(GuiMgForm form)
      {
         _form = form;
      }

      /// <summary>
      ///   Mulriline control constructor
      /// </summary>
      /// <param name = "control">magic control
      /// </param>
      /// <param name = "idx">for multiline controls - index of the control
      /// </param>
      internal MapData(GuiMgControl control, int idx)
      {
         _control = control;
         _idx = idx;
      }

      /// <summary>
      ///   Magic control constructor
      /// </summary>
      /// <param name = "control">magic control
      /// </param>
      internal MapData(GuiMgControl control)
      {
         _control = control;
      }

      /// <summary>
      /// </summary>
      /// <param name = "MenuRef">menu reference</param>
      internal MapData(MenuReference MenuRef)
      {
         _menuRef = MenuRef;
      }

      /// <summary>
      /// </summary>
      /// <returns> control
      /// </returns>
      internal GuiMgControl getControl()
      {
         return _control;
      }

      /// <summary>
      /// </summary>
      /// <returns> form
      /// </returns>
      internal GuiMgForm getForm()
      {
         return _form;
      }

      /// <summary>
      /// </summary>
      /// <returns> multiline index
      /// </returns>
      internal int getIdx()
      {
         return _idx;
      }

      /// <summary>
      /// </summary>
      /// <returns>The menu reference</returns>
      internal MenuReference getMenuReference()
      {
         return _menuRef;
      }

      /// <summary>
      ///   compares MapData
      /// </summary>
      /// <returns>
      /// </returns>
      public override bool Equals(Object obj)
      {
         MapData mapData = (MapData) obj;
         if (mapData == this)
            return true;
         if (mapData != null && mapData._control == _control && mapData._idx == _idx && mapData._form == _form)
            return true;
         return false;
      }

      /// <summary>
      ///   set index
      /// </summary>
      /// <param name = "idx">
      /// </param>
      internal void setIdx(int idx)
      {
         _idx = idx;
      }

      /// <summary>
      ///   Overridden Object.GetHashCode()
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         return base.GetHashCode();
      }
   }
}