using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
#if !PocketPC
using System.Windows.Forms.Layout;
#else
using LayoutEngine = com.magicsoftware.mobilestubs.LayoutEngine;
using Splitter = com.magicsoftware.mobilestubs.MgSplitter;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   The MgSplitContainer is a composite control that lays out its children in a row or column arrangement (as
   ///   specified by the orientation) and places a splitter between each child.
   /// </summary>
   public class MgSplitContainer : Panel
   {
      internal const int SPLITTER_STYLE_VERTICAL = 1;
      internal const int SPLITTER_STYLE_HORIZONTAL = 2;
      internal const int SPLITTER_WIDTH = 4;

      internal List<Splitter> Splitters { get; private set; }
      internal Rectangle LastClientArea { get; set; }
      internal int SplitterStyle { get; private set; }
      internal bool IgnoreLayout { get; set; }

      private MgSplitContainerLayout _layoutEngine;
      private readonly List<Control> _controlTable;

      /// <summary>
      ///   Constructs a new instance of this class given its parent and a style value describing its behavior and
      ///   appearance.
      ///   <p>
      ///     The style value is either one of the style constants defined in class <code>SWT</code> which is
      ///     applicable to instances of this class, or must be built by <em>bitwise OR</em>'ing together (that is,
      ///     using the <code>int</code> "|" operator) two or more of those <code>SWT</code> style constants. The
      ///     class description lists the style constants that are applicable to the class. Style bits are also
      ///     inherited from superclasses.
      ///   </p>
      /// </summary>
      internal MgSplitContainer()
      {
         SuspendLayout();
         GuiUtilsBase.CreateTagData(this);

         Splitters = new List<Splitter>();
         _controlTable = new List<Control>();

         // if ((style & Styles.PROP_STYLE_NO_BACKGROUND) != 0)
         // swtStyle |= SWT.NO_BACKGROUND;

         LastClientArea = new Rectangle(0, 0, 0, 0);
      }

      /// <summary>
      /// </summary>
      /// <param name = "child"></param>
      internal void addChild(Control child)
      {
         //add & save cell for the new child
         setDockProperty(child);
         _controlTable.Add(child);
      }

      /// <summary>
      ///   set Dock property to Top\Left if it is the first control that is added
      ///   otherwise set it to Fill
      /// </summary>
      /// <param name = "parent"></param>
      /// <param name = "control"></param>
      private void setDockProperty(Control control)
      {
         DockStyle setDock = DockStyle.Fill;

         if (_controlTable.Count == 0)
         {
            if (SplitterStyle == SPLITTER_STYLE_HORIZONTAL)
               setDock = DockStyle.Top;
            else
               setDock = DockStyle.Left;
         }
         control.Dock = setDock;
      }

      /// <summary>
      ///   find the first left\top control
      /// </summary>
      /// <returns></returns>
      internal Control getLeftOrTopControl()
      {
         Control controlLeftTop = null;

         foreach (Control c in Controls)
         {
            if ((c is Panel) && (c.Dock == DockStyle.Left || c.Dock == DockStyle.Top))
            {
               controlLeftTop = c;
               break;
            }
         }

         return controlLeftTop;
      }

      /// <summary>
      ///   add the control in the correct order,
      ///   the fill control must be added first and then the splitter must be added
      /// </summary>
      internal void orderChildren()
      {
         Control controlFill = getFillControl();
         Controls.Add(controlFill);

         //add splitter control
         addSplitterControl();

         //add other contorls that are not fill
         foreach (Control c in _controlTable)
         {
            if (c != controlFill)
               Controls.Add(c);
         }
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      private Control getFillControl()
      {
         Control controlFill = null;
         // find the Fill control, must be added first
         foreach (Control c in _controlTable)
         {
            if (c.Dock == DockStyle.Fill)
            {
               controlFill = c;
               break;
            }
         }
         return controlFill;
      }

#if !PocketPC
      /// <summary>
      ///   Gets a cached instance of the control's layout engine.
      /// </summary>
      /// <returns>
      ///   The <see cref = "T:System.Windows.Forms.Layout.LayoutEngine" /> for the control's contents.
      /// </returns>
      public override LayoutEngine LayoutEngine
#else
      internal LayoutEngine LayoutEngine
#endif

      {
         get
         {
            if (_layoutEngine == null)
            {
               _layoutEngine = new MgSplitContainerLayout();
            }

            return _layoutEngine;
         }
      }

      /// <summary>
      ///   Returns SWT.HORIZONTAL if the controls in the MgSplitContainer are laid out side by side or SWT.VERTICAL if
      ///   the controls in the MgSplitContainer are laid out top to bottom.
      /// </summary>
      /// <returns> SWT.HORIZONTAL or SWT.VERTICAL
      /// </returns>
      internal int getOrientation()
      {
         return (SplitterStyle); // == SPLITTER_STYLE_VERTICAL ? SPLITTER_STYLE_VERTICAL : SPLITTER_STYLE_HORIZONTAL);
      }

      /// <summary>
      ///   get the controls of the MgSplitContainer
      /// </summary>
      /// <param name = "onlyVisible">
      /// </param>
      /// <returns>
      /// </returns>
      internal Control[] getControls(bool onlyVisible)
      {
         int i;
         int j;

         Control[] result = new Control[0];
         for (i = 0;
              i < Controls.Count;
              i++) // i = 0; i < children.Length; i++)
         {
            Control c = Controls[i];

            if (c is Splitter)
               continue;

            if (onlyVisible && !c.Visible)
               continue;

            Control[] newResult = new Control[result.Length + 1];
            Array.Copy(result, 0, newResult, 0, result.Length);
            newResult[result.Length] = c;
            result = newResult;
         }

         Control[] newResultReverseControl = new Control[result.Length];
         for (j = result.Length - 1, i = 0;
              j >= 0;
              j--, i++)
            newResultReverseControl[i] = result[j];

         return newResultReverseControl;
      }

      /// <summary>
      ///   handle the resize of the splitter, need to re paint the 4 prev pix
      /// </summary>
      /// <param name = "event">
      /// </param>
      internal void onResize()
      {
         MgSplitContainer mgSahsForm = this;
         TagData tagData = null;

         for (int i = 0;
              i < mgSahsForm.Splitters.Count;
              i++)
         {
            Splitter splitter = mgSahsForm.Splitters[i];
            tagData = ((TagData) splitter.Tag);

            if (tagData.RepaintRect != null)
            {
               Rectangle paintRect = (Rectangle) tagData.RepaintRect;
               Rectangle drawRect = new Rectangle();

               if (mgSahsForm.getOrientation() == SPLITTER_STYLE_VERTICAL)
                  drawRect = new Rectangle(paintRect.Width - 3, 0, 3, paintRect.Height);
               else
                  drawRect = new Rectangle(0, paintRect.Height - 3, paintRect.Width, 3);

               splitter.Invalidate(drawRect, true);
               tagData.RepaintRect = null;
            }
         }

         if (((TagData) mgSahsForm.Tag).RepaintRect != null)
         {
            tagData = ((TagData) mgSahsForm.Tag);
            Rectangle paintRect = (Rectangle) tagData.RepaintRect;
            paintRect.Width = Math.Min(paintRect.Width, mgSahsForm.Bounds.Width);
            paintRect.Height = Math.Min(paintRect.Height, mgSahsForm.Bounds.Height);
            Rectangle drawRect = new Rectangle();

            drawRect = new Rectangle(paintRect.Width - 3, 0, 3, paintRect.Height);
            mgSahsForm.Invalidate(drawRect, true);

            drawRect = new Rectangle(0, paintRect.Height - 3, paintRect.Width, 3);
            mgSahsForm.Invalidate(drawRect, true);

            tagData.RepaintRect = null;
         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "SplitContainer"></param>
      private void addSplitterControl()
      {
         Splitter newSplitter = null;

         newSplitter = new Splitter();

         //newSplitter.MouseDown += new MouseEventHandler(newSplitter_MouseDown);
         GuiUtilsBase.CreateTagData(newSplitter);

         if (SplitterStyle == SPLITTER_STYLE_HORIZONTAL)
         {
#if !PocketPC
            newSplitter.Cursor = Cursors.VSplit;
#endif
            newSplitter.Dock = DockStyle.Left;
         }
         else
         {
#if !PocketPC
            newSplitter.Cursor = Cursors.HSplit;
#endif
            newSplitter.Dock = DockStyle.Top;
         }
         SplitterHandler.getInstance().addHandler(newSplitter);
         newSplitter.MinExtra = 0;
         newSplitter.MinSize = 0;

         Controls.Add(newSplitter);
         Splitters.Add(newSplitter);
      }

      /// <summary>
      /// Fixed bug #:160305 & 427851, while control is child of frame set force refresh for w\h
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void newSplitter_MouseDown(object sender, MouseEventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();

         foreach (var ctrl in Controls)
         {
            //            Control ctrl = (Control)Controls[0];
            MapData mapData = controlsMap.getMapData(ctrl);
            if (mapData != null)
            {
               GuiMgControl guiMgControl = mapData.getControl();
               if (SplitterStyle == SPLITTER_STYLE_HORIZONTAL)
                  guiMgControl.ForceRefreshPropertyWidth = true;
               else if (SplitterStyle == SPLITTER_STYLE_VERTICAL)
                  guiMgControl.ForceRefreshPropertyHight = true;
            }
         }
      }
      

      /// <summary>
      ///   set the splitter style of the splitter container
      /// </summary>
      /// <param name = "style"></param>
      internal void SetSplitterStyle(int style)
      {
         // set the splitter set style
         if ((style & Styles.PROP_STYLE_VERTICAL) != 0)
            SplitterStyle = SPLITTER_STYLE_VERTICAL;
         else if ((style & Styles.PROP_STYLE_HORIZONTAL) != 0)
            SplitterStyle = SPLITTER_STYLE_HORIZONTAL;
      }

#if PocketPC
      internal void Invalidate(Rectangle rc, bool invalidateChildren)
      {
         Invalidate(rc);
      }

      internal void PerformLayout()
      {
      }
#endif
   }
}