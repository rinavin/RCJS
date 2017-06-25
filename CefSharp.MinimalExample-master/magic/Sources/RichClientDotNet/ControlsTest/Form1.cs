using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ContolsTest.TableTest;
using com.magicsoftware.support;
using com.magicsoftware.util;
using ControlsTest.ShapeControlTest;

namespace ControlsTest
{
   public partial class Form1 : Form
   {
      public Form1()
      {
         InitializeComponent();
      }

      private void Form1_Load(object sender, EventArgs e)
      {

      }

      private void button1_Click(object sender, EventArgs e)
      {
         ListApp.HeaderTest headerTest = new ListApp.HeaderTest();
         headerTest.Show();

      }

      private void button2_Click(object sender, EventArgs e)
      {
         TableTestFrm tableTestFrm = new TableTestFrm();
         tableTestFrm.Show();
      }

      private void button3_Click(object sender, EventArgs e)
      {
         Form f = new SampleForm();
         f.Show();
         RuntimeDesigner.MainShell s = new RuntimeDesigner.MainShell(TranslateString, this.Icon, true);
         s.AddDesigner(f, CreateAllOwnerDrawControlsDefault, GetControlDesigner);
         s.Show();
         f.Dispose();
      
      }
      public String TranslateString(String str)
      {
         return str;
      }

      Dictionary<Control, bool> CreateAllOwnerDrawControlsDefault(Control control)
      {
         Dictionary<Control, bool> dict = new Dictionary<Control, bool>();
         foreach (Control item in control.Controls)
         {
            dict[item] = false;
         }

         return dict;
      }

      ControlDesignerInfo GetControlDesigner(object component)
      {
         ControlDesignerInfo c = new ControlDesignerInfo();
         
         c.FileName = @"c:\temp\testfile.xml";
         c.Properties = new Dictionary<string, DesignerPropertyInfo>();
         if (component is Form)
            c.Properties.Add(Constants.ConfigurationFilePropertyName, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = c.FileName, IsNativeProperty = false });
         else if (component is Control)
         {
            Control control = ((Control)component);
            c.Properties.Add(Constants.WinPropLeft, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Left });
            c.Properties.Add(Constants.WinPropWidth, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Width });
            c.Properties.Add(Constants.WinPropTop, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Top });
            c.Properties.Add(Constants.WinPropHeight, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Height });
            c.Properties.Add(Constants.WinPropVisible, new DesignerPropertyInfo());
            c.Properties.Add(Constants.WinPropName, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Name });
            c.Isn = component.GetHashCode(); 
            Control parent = control.Parent;
            if (parent != null && ! (parent is Form))
            {
               c.ParentId = parent.GetHashCode();
            }
         }
        //TODO : link Isn

         c.Id = c.Isn;
         return c;
      }

      private void button4_Click(object sender, EventArgs e)
      {
         ShapeTestForm form = new ShapeTestForm();
         form.Show();
      }
   }
}
