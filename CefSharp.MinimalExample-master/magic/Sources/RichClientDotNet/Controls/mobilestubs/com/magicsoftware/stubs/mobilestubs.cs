using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;

namespace com.magicsoftware.mobilestubs
{
   public enum TabAlignment
   {
      Top = 0,
      Bottom = 1,
      Left = 2,
      Right = 3,
   }

   // Summary:
   //     Specifies a value indicating whether the text appears from right to left,
   //     such as when using Hebrew or Arabic fonts.
   public enum RightToLeft
   {
      // Summary:
      //     The text reads from left to right. This is the default.
      No = 0,
      //
      // Summary:
      //     The text reads from right to left.
      Yes = 1,
      //
      // Summary:
      //     The direction the text read is inherited from the parent control.
      Inherit = 2,
   }

   // Summary:
   //     We are using FlatStyle enum for Button control.
   //     In CF it is not supported, hence defined enum for it.
   public enum FlatStyle
   {
      Flat  =  0,
      Popup =  1,
      Standard =  2,
      System   =  3
   }

   public class PointF
   {
      private float x;
      private float y;

      public PointF()
      {            
      }
      public PointF(float x, float y)
      {
         this.x = x;
         this.y = y;
      }


      public float X
      {
         get
         {
            return this.x;
         }
         set
         {
            this.x = value;
         }
      }
      public float Y
      {
         get
         {
            return this.y;
         }
         set
         {
            this.y = value;
         }
      }
             
   }
   public class FlatButtonAppearance
   {
      public Color BorderColor { get; set; }
      public int BorderSize { get; set; }
      public Color CheckedBackColor { get; set; }
      public Color MouseDownBackColor { get; set; }
      public Color MouseOverBackColor { get; set; }
   }

   public enum ImageLayout
   {
      None = 0,
      Tile = 1,
      Center = 2,
      Stretch = 3,
      Zoom = 4
   }

   public enum Appearance
   {
      Normal = 0,
      Button = 1
   }

   /// <summary>
   /// 
   /// </summary>
   public class Message
   {
      public IntPtr HWnd { get; set; }
      public IntPtr LParam { get; set; }
      public int Msg { get; set; }
      public IntPtr WParam { get; set; }
   }

   public sealed class Brushes
   {
      static Brushes()
      {
         White = new SolidBrush(Color.White);
      }
      public static Brush White { get; set; }
   }

   /// <summary>
   /// 
   /// </summary>
   public class SplitterEventArgs : EventArgs
   {
      public int SplitX { get; set; }
      public int SplitY { get; set; }
      public SplitterEventArgs()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class NodeLabelEditEventArgs : EventArgs
   {
      public NodeLabelEditEventArgs()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class TreeNodeMouseHoverEventArgs : EventArgs
   {
      public TreeNodeMouseHoverEventArgs()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class FormClosingEventArgs : EventArgs
   {
      public Boolean Cancel;

      public FormClosingEventArgs()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class PreviewKeyDownEventArgs : EventArgs
   {
      public PreviewKeyDownEventArgs()
      {
         throw new NotSupportedException();
      }
   }

   public class HandledMouseEventArgs : EventArgs
   {
      public HandledMouseEventArgs()
      {
         throw new NotSupportedException();
      }

      public bool Handled { get; set; }
   }
   /// <summary>
   /// 
   /// </summary>
   public class LayoutEngine
   {
      public LayoutEngine()
      {
      }

      public virtual bool Layout(object container, LayoutEventArgs layoutEventArgs) { return false; }
   }

   /// <summary>
   /// 
   /// </summary>
   public class LayoutEventArgs : EventArgs
   {
      public LayoutEventArgs()
      {
      }
      public LayoutEventArgs(Control affectedControl, string affectedProperty)
      {
      }
      public Control AffectedControl { get; set; }
   }

   /// <summary>
   /// 
   /// </summary>
   // Summary:
   //     Wraps a managed object holding a handle to a resource that is passed to unmanaged
   //     code using platform invoke.
   public struct HandleRef
   {
      private IntPtr handle;
      private object wrapper;

      public HandleRef(object wrapper, IntPtr handle)
      {
         this.handle = handle;
         this.wrapper = wrapper;
      }

      //public static explicit operator IntPtr(HandleRef value);

      public IntPtr Handle { get { return handle; } }
      public object Wrapper { get { return Wrapper; } }

      //public static IntPtr ToIntPtr(HandleRef value);
   }

   /// <summary>
   /// 
   /// </summary>
   public enum LeftRightAlignment
   {
      Left = 0,
      Right = 1,
   }

   /// <summary>
   /// 
   /// </summary>
   public class CreateParams
   {
      public CreateParams()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class GroupBox
   {
      public GroupBox()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class StatusStrip
   {
      public StatusStrip()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStrip
   {
      public object Tag;

      public ToolStrip()
      {
         throw new NotSupportedException();
      }

      public void Dispose()
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStripItem
   {
      public object Tag;

      public ToolStripItem()
      {
         throw new NotSupportedException();
      }

      public void Dispose()
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStripMenuItem
   {
      public ToolStripMenuItem()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStripButton
   {
      public ToolStripButton()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStripLabel
   {
      public ToolStripLabel()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolStripStatusLabel
   {
      public ToolStripStatusLabel()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ContextMenuStrip
   {
      public ContextMenuStrip()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class ToolTip
   {
      public ToolTip()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class RichTextBox: System.Windows.Forms.Control
   {
      public String Rtf;

      public RichTextBox()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class MgSplitContainerData
   {
      public MgSplitContainerData()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class MgSplitter : Splitter
   {
      public Cursor Cursor { get; set; }
      public void Invalidate(Rectangle rc, bool invalidateChildren)
      {
         Invalidate(rc);
      }
   }

   /// <summary>
   /// 
   /// </summary>
   abstract public class LinkLabelLinkClickedEventArgs : EventArgs
   {
      // Summary:
      //     Gets the mouse button that causes the link to be clicked.
      //
      // Returns:
      //     One of the System.Windows.Forms.MouseButtons values.
      abstract public MouseButtons Button { get; }
   }

   /// <summary>
   /// 
   /// </summary>
   public class TreeManager
   {
      public TreeManager()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class MinSizeInfo
   {
      public MinSizeInfo()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class FormStartPosition
   {
      public FormStartPosition()
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class StaticControlsManager
   {
      public StaticControlsManager()
      {
         throw new NotSupportedException();
      }
   }

   public class MgArgumentOutOfRangeException : ArgumentOutOfRangeException
   {
      public MgArgumentOutOfRangeException() 
         : base() 
      {
      }
      public MgArgumentOutOfRangeException(string paramName) 
         : base(paramName) 
      {
      }
      public MgArgumentOutOfRangeException(string paramName, string message) 
         : base(paramName, message) 
      {
      }
      public MgArgumentOutOfRangeException(string paramName, object actualValue, string message)
         : base(paramName, message)
      {
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public sealed class TypeConverterAttribute : Attribute
   {
      public TypeConverterAttribute(Type type)
      {
         throw new NotSupportedException();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   public class LocalizationEnumConverter
   {
      public LocalizationEnumConverter()
      {
         throw new NotSupportedException();
      }
   }
}

