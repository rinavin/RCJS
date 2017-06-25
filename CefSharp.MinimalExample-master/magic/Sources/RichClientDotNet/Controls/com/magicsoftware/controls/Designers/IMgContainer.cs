using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
namespace com.magicsoftware.controls.designers
{
   public delegate void ComponentDroppedDelegate(object sender, ComponentDroppedArgs e);

   public interface IMgContainer
   {
      event ComponentDroppedDelegate ComponentDropped;
      void OnComponentDropped(ComponentDroppedArgs args);
   }

   /// <summary>
   /// arguments of component adding event
   /// </summary>
   public class ComponentDroppedArgs
   {
      public List<IComponent> Components { get; set; }
      public List<Point> Locations { get; set; }
      public List<Point> ControlLocations { get; set; }
      public bool IsDroppedFromToolbox { get; set; }

      public bool IsDroppedFromDocumentOutline { get; set; }
   }

}
