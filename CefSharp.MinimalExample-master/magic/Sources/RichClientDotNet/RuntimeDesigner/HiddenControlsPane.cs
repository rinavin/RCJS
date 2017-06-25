using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using com.magicsoftware.controls;
using System.Collections;
using com.magicsoftware.util;
using com.magicsoftware.support;

namespace RuntimeDesigner
{
   delegate void ControlRestoredDelegate(Control control);
   delegate bool IsControlAncestorHiddenDelegate(Control control);

   /// <summary>
   /// pane for displaying the hidden controls
   /// </summary>
   public class HiddenControlsPane : UserControl
   {
      static Font font = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.World);
      static SolidBrush brush = new SolidBrush(Color.White);
 
      ListBox controlsListBox;

      /// <summary>
      /// callback to tell the designer a control should be restored to its place
      /// </summary>
      ControlRestoredDelegate controlRestored;
      
      IsControlAncestorHiddenDelegate isControlAncestorHidden;

      /// <summary>
      /// hidden controls which do not appear in the listbox
      /// </summary>
      List<Control> hiddenControls = new List<Control>();

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="controlRestored"></param>
      internal HiddenControlsPane(Dictionary<object, ComponentWrapper> componentsDictionary, ControlRestoredDelegate controlRestored, IsControlAncestorHiddenDelegate isControlAncestorHidden)
      {
         this.Dock = DockStyle.Fill;

         InitListBox();
         InitContextMenu();

         this.controlRestored = controlRestored;
         this.isControlAncestorHidden = isControlAncestorHidden;

         foreach (var item in componentsDictionary)
         {
            PropertyDescriptor prop = item.Value.PropertiesDescriptors[Constants.WinPropVisible];
            if (prop != null && !(bool)prop.GetValue(item.Key))
               hiddenControls.Add((Control)item.Key);
         }

         RecalculateListboxItems();
      }

      /// <summary>
      /// initialize the listbox
      /// </summary>
      private void InitListBox()
      {
         controlsListBox = new ListBox();
         controlsListBox.DrawItem += ItemsListBox_DrawItem;
         controlsListBox.DrawMode = DrawMode.OwnerDrawFixed;
         controlsListBox.BorderStyle = BorderStyle.None;
         controlsListBox.ItemHeight = 20;
         controlsListBox.Dock = DockStyle.Fill;
         controlsListBox.SelectedIndexChanged += ItemsListBox_SelectedIndexChanged;
         Controls.Add(controlsListBox);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
      {
         controlsListBox.Invalidate();
      }

      /// <summary>
      /// initialize the context menu
      /// </summary>
      private void InitContextMenu()
      {
         ContextMenuStrip = new ContextMenuStrip();
         ContextMenuStrip.Opening += ContextMenuStrip_Opening;
         
         ToolStripButton restoreMenuItem = new ToolStripButton("Add to form", null, RestoreMenuItem_Click);
         restoreMenuItem.Width = 100;
         ContextMenuStrip.Items.Add(restoreMenuItem);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
      {
         if (controlsListBox.SelectedIndex == -1)
         {
            e.Cancel = true;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="controls"></param>
      internal void ControlsDeleted(ICollection controls)
      {
         foreach (Control item in controls)
         {
            hiddenControls.Add(item);
         }

         RecalculateListboxItems();
      }

      /// <summary>
      /// recalculate which deleted controls should be seen in the listbox, and which should remain in the otherHiddenControls list.
      /// Controls should not appear in the listbox if they are descendants of other hidden controls
      /// </summary>
      void RecalculateListboxItems()
      {
         List<Control> removeFromListbox = new List<Control>();
         List<Control> addToListbox = new List<Control>();

         // go over listbox, check if a control has a hidden ancestor
         foreach (Control item in controlsListBox.Items)
         {
            if (isControlAncestorHidden(item))
               removeFromListbox.Add(item);
         }

         // go over other controls, see if control has no hidden ancestor
         foreach (Control item in hiddenControls)
         {
            if (!isControlAncestorHidden(item))
               addToListbox.Add(item);
         }

         // remove from listbox, add to otherHiddenControls
         foreach (Control item in removeFromListbox)
         {
            controlsListBox.Items.Remove(item);
         }
         hiddenControls.AddRange(removeFromListbox);

         // remove from otherHiddenControls and add to listbox
         foreach (Control item in addToListbox)
         {
            hiddenControls.Remove(item);
         }
         controlsListBox.Items.AddRange(addToListbox.ToArray());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void RestoreMenuItem_Click(object sender, EventArgs e)
      {
         if (controlsListBox.SelectedItem is Control)
         {
            controlRestored((Control)controlsListBox.SelectedItem);
            controlsListBox.Items.Remove(controlsListBox.SelectedItem);

            RecalculateListboxItems();
         }
      }
      
      /// <summary>
      /// paint the listbox items
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ItemsListBox_DrawItem(object sender, DrawItemEventArgs e)
      {
         ListBox listbox = sender as ListBox;

         if (e.Index == -1)
            return;
 
         Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, Width, e.Bounds.Height);
         Brush b = brush;
         
         // If this item is the currently selected item, draw it with a highlight.
         if (listbox.SelectedIndex == e.Index)
            b = Brushes.Moccasin;

         e.Graphics.FillRectangle(b, rect);

         Control control = listbox.Items[e.Index] as Control;

         Control controlForBitmap = control is DotNetComponentWrapper ? ((DotNetComponentWrapper)control).WrappedControl : control;
         // get the bitmap
         ToolboxBitmapAttribute tba = TypeDescriptor.GetAttributes(controlForBitmap)[typeof(ToolboxBitmapAttribute)] as ToolboxBitmapAttribute;
         Bitmap bitmap = (Bitmap)tba.GetImage(controlForBitmap);

         Rectangle bitmapBounds = new Rectangle(rect.Location.X + 20, rect.Location.Y + 2, 16, 16);
         Rectangle stringBounds = new Rectangle(rect.Location.X + bitmapBounds.Width + 25, rect.Location.Y, 145, rect.Height);

         StringFormat format = new StringFormat();
         format.LineAlignment = StringAlignment.Center;
         format.Alignment = StringAlignment.Near;
         format.FormatFlags = StringFormatFlags.NoWrap;
         format.Trimming = StringTrimming.EllipsisCharacter;

         e.Graphics.DrawImage(bitmap, bitmapBounds);

         e.Graphics.DrawString((string)((ControlDesignerInfo)control.Tag).Properties[Constants.WinPropName].Value, font, Brushes.Black, stringBounds, format);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void ResetHiddenControls()
      {
         controlsListBox.Items.Clear();
         hiddenControls.Clear();
         // TODO: show the hidden controls
      }
   }
}
