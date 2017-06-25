
using com.magicsoftware.unipaas.management.gui;
namespace com.magicsoftware.unipaas.management.events
{
   /// <summary>
   /// functionality required by the GUI namespace from the ActionManager class.
   /// </summary>
   public interface IActionManager
   {
      /// <summary>
      ///   sets action enabled or disabled
      /// </summary>
      /// <param name = "act"></param>
      /// <param name = "enable"></param>
      void enable(int act, bool enable);

      /// <param name = "act"></param>
      /// <returns> true if the action is enabled or false if it is not</returns>
      bool isEnabled(int act);

      /// <param name = "act"></param>
      /// <returns> actCount for the specific action</returns>
      int getActCount(int act);

      /// <summary>
      ///   sets action list enabled or disabled.
      ///   onlyIfChanged was added in order to avoid many updates to menus (like in cut/copy),
      ///   especially when the request comes as an internal act from the gui thread.
      /// </summary>
      /// <param name = "act">array of actions</param>
      /// <param name = "enable"></param>
      /// <param name = "onlyIfChanged">: call enable only if the state has changed.</param>
      void enableList(int[] act, bool enable, bool onlyIfChanged);

      /// <summary>
      ///   enable or disable actions for ACT_STT_EDT_EDITING state
      /// </summary>
      /// <param name = "enable"></param>
      void enableEditingActions(bool enable);

      /// <summary>
      ///   enable or disable actions for Multi-line edit
      /// </summary>
      /// <param name = "enable"></param>
      void enableMLEActions(bool enable);

      /// <summary>
      ///   enable or disable actions for rich edit
      /// </summary>
      /// <param name = "enable"></param>
      void enableRichEditActions(bool enable);

      /// <summary>
      ///   enable or disable actions for tree
      /// </summary>
      /// <param name = "enable"></param>
      void enableTreeActions(bool enable);

      /// <summary>
      ///   enable or disable actions for navigation
      /// </summary>
      /// <param name = "enable"></param>
      void enableNavigationActions(bool enable);

      /// <summary>
      ///   This is the work thread method to check if to enable/disable the paste action.
      ///   It is equivalent to the GuiUtils.checkPasteEnable (used by the gui thread).
      /// </summary>
      /// <param name = "ctrl"></param>
      void checkPasteEnable(MgControlBase ctrl);
   }
}