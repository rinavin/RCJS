using System;

namespace com.magicsoftware.util
{
   [Flags]
   public enum ShapeTypes
   {
      Rect = 0x0001,             /* old CTRL_STATIC_TYPE_RECT        */
      RoundRect = 0x0002,        /* old CTRL_STATIC_TYPE_ROUND_RECT  */
      Ellipse = 0x0004,          /* old CTRL_STATIC_TYPE_ELLIPSE     */
      NWSELine = 0x0008,         /* old CTRL_STATIC_TYPE_NWSE_LINE   */
      NESWLine = 0x0010,         /* old CTRL_STATIC_TYPE_NESW_LINE   */
      HorizontalLine = 0x0020,   /* old CTRL_STATIC_TYPE_HORI_LINE   */
      VerticalLine = 0x0040,     /* old CTRL_STATIC_TYPE_VERT_LINE   */
      Group = 0x0080,            /* old CTRL_STATIC_TYPE_GROUP       */
   }
}