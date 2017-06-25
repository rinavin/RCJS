////////////////////////////////////////////////////////////////////////////////////
//  File:   NativeWrappers.cs
//  Author: Sergei Pavlovsky
//
//  Copyright (c) 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
//	This file is provided "as is" with no expressed or implied warranty.
//	The author accepts no liability if it causes any damage whatsoever.
// 
//  This code is free and may be used in any way you desire. If the source code in 
//  this file is used in any commercial application then a simple email would be 
//	nice.
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace com.magicsoftware.win32
{
   public partial class user32
   {
      public struct TRIVERTEX
      {
         public int x;
         public int y;
         public ushort Red;
         public ushort Green;
         public ushort Blue;
         public ushort Alpha;
         public TRIVERTEX(int x, int y, Color color)
            : this(x, y, color.R, color.G, color.B, color.A)
         {
         }
         public TRIVERTEX(
             int x, int y,
             ushort red, ushort green, ushort blue,
             ushort alpha)
         {
            this.x = x;
            this.y = y;
            this.Red = (ushort)(red << 8);
            this.Green = (ushort)(green << 8);
            this.Blue = (ushort)(blue << 8);
            this.Alpha = (ushort)(alpha << 8);
         }
      }
      public struct GRADIENT_RECT
      {
         public uint UpperLeft;
         public uint LowerRight;
         public GRADIENT_RECT(uint ul, uint lr)
         {
            this.UpperLeft = ul;
            this.LowerRight = lr;
         }
      }


      [DllImport("coredll.dll", SetLastError = true, EntryPoint = "GradientFill")]
      public extern static bool GradientFill(
          IntPtr hdc,
          TRIVERTEX[] pVertex,
          uint dwNumVertex,
          GRADIENT_RECT[] pMesh,
          uint dwNumMesh,
          uint dwMode);

      public const int GRADIENT_FILL_RECT_H = 0x00000000;
      public const int GRADIENT_FILL_RECT_V = 0x00000001;

   }
}
