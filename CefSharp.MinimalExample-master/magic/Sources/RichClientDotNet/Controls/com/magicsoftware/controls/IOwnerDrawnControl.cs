using System.Drawing;

namespace com.magicsoftware.controls
{
   internal interface IOwnerDrawnControl
   {
      void Draw(Graphics graphics, Rectangle rectangle);
   }
}
