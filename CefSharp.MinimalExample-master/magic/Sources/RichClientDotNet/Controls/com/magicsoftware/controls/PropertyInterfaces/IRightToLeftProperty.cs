using System.Windows.Forms;
#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
#endif

namespace com.magicsoftware.controls
{
   interface IRightToLeftProperty
   {
      RightToLeft RightToLeft { get; set; }
   }
}
