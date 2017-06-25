namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   This class was created only in order to validate objects which are placed in the ControlsMap.
   ///   Since we have an Object which is created only for reference to a spcific menu of a form+style,
   ///   we created this class, which will allow validation of the type of the object added to the map.
   /// 
   ///   The MenuReference will save the MgForm the item is on. It will be used by the handlers in order to:
   ///   1. Find the form. 2. Use the MgForm in the dispose since the mgForm is the key of the hash tables on the menu's instantiations.
   ///   *** Toolbar will not save the MgForm, it is not needed and we don't want dengling refs. Toolbar items will have it.
   /// </summary>
   /// <author>ilanab</author>
   public class MenuReference
   {
      private readonly GuiMgForm _mgForm;

      /// <summary>
      ///   Ctor
      /// </summary>
      /// <param name = "mgForm">MgForm that the instance is on</param>
      public MenuReference(GuiMgForm mgForm)
      {
         _mgForm = mgForm;
      }

      /// <summary>
      ///   Get the MgForm
      /// </summary>
      /// <returns></returns>
      internal GuiMgForm GetMgForm()
      {
         return _mgForm;
      }
   }
}