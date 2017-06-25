using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   ///  This is a splitter control which has button control which acts as expander
   /// </summary>
   [Designer(typeof(SplitterWithExpanderDesigner))]
   public class SplitterWithExpander : Splitter, INotifyPropertyChanged
   {
      #region private members

      private ExpanderButton button;
      private Label label;
      public static int MARGIN = 2;
      #endregion private members

      /// <summary>
      /// denotes the normal font
      /// </summary>
      public static Font NormalFont { get; set; }

      /// <summary>
      /// denotes the Bold font
      /// </summary>
      public static Font BoldFont { get; set; }

      #region ctor

      public SplitterWithExpander()
      {
      }

      public void Initialize(int splitterHeight, string name)
      {
         if (Controls.Count == 0)
         {
            button = new ExpanderButton();

            button.BackColor = Color.Transparent;
            button.BackgroundImageLayout = ImageLayout.Center;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = Color.Transparent;
            button.Size = new Size(splitterHeight, splitterHeight);
            button.Dock = DockStyle.Left;
            button.Cursor = Cursors.Arrow;

            button.Click += Button_Click;

            // Create label control to display the  text
            label = new Label();
            label.Text = name;
            label.BackColor = Color.Transparent;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(MARGIN, 0, 0, 0);

            // Add label to splitter
            Controls.Add(label);

            // Add button (will be added on left of label as dockstyle is left)
            Controls.Add(button);
            IsExpanded = true;
         }
      }

      /// <summary>
      /// Toggle the state of expander
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Button_Click(object sender, EventArgs e)
      {
         this.IsExpanded = !IsExpanded;
      }

      #endregion

      #region properties
      /// <summary>
      /// denotes if the expander is expanded / collapsed
      /// </summary>
      public bool IsExpanded
      {
         get
         {
            return button.IsExpanded; // get value from button
         }
         set
         {
            if (button.IsExpanded != value)
            {
               button.IsExpanded = value; // set value of button
               OnPropertyChanged("IsExpanded");
            }
         }
      }

      private bool isActive;
      /// <summary>
      ///  Denotes if the splitter is Active
      /// </summary>
      public bool IsActive
      {
         get
         {
            return isActive;
         }
         set
         {
            if(isActive != value)
            {
               isActive = value;
               Font = isActive ? BoldFont : NormalFont; // set the text of active splitter in bold
            }
         }
      }

      #endregion

      public SplitterHitTestArea HitTest(Point pt)
      {
         if (this.Controls.Count > 0)
         {
            if (this.button.DisplayRectangle.Contains(pt))
               return SplitterHitTestArea.Expander;
         }

         return SplitterHitTestArea.Splitter;
      }

      #region private members



      internal void UpdateText(string text)
      {
         label.Text = text;
      }
    
      #endregion


      #region INotifyPropertyChanged

      public event PropertyChangedEventHandler PropertyChanged;

      public void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

      #endregion INotifyPropertyChanged
   }
}