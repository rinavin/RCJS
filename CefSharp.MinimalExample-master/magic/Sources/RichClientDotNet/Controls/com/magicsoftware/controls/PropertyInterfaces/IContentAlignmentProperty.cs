using System.Drawing;
#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.controls
{
   internal interface IContentAlignmentProperty
   {
      ContentAlignment TextAlign { get; set; }
   }
}
