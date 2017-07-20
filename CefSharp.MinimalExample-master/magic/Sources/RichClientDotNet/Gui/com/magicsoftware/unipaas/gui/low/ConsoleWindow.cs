using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   This class is used for redirecting the output of the System.out and System.err streams to a Java Console
   ///   window
   /// </summary>
   public class Console
   {
      // If we are hooking stdout and stderr, keep the old ones here
      private static StreamWriter _out;
      private static StreamWriter _err;

      /// <summary>
      ///   hook stdout and stderr, and redirect output to the console keep the originals around, of course
      /// </summary>
      internal static void hookStandards()
      {
         lock (typeof(Console))
         {
            if (_out == null)
            {
               _out = new StreamWriter(System.Console.OpenStandardOutput(), System.Console.Out.Encoding);
               _out.AutoFlush = true;

               _err = new StreamWriter(System.Console.OpenStandardError(), System.Console.Error.Encoding);
               _err.AutoFlush = true;

               StreamWriter dwout = new StreamWriter(new ConsoleOutputStream(false, _out));
               System.Console.SetOut(dwout);

               StreamWriter dwerr = new StreamWriter(new ConsoleOutputStream(true, _err));
               System.Console.SetError(dwerr);
            }
         }
      }

      /// <summary>
      ///   undo the hooking of stdout and stderr
      /// </summary>
      public static void unhookStandards()
      {
         lock (typeof(Console))
         {
            if (_out != null)
            {
               System.Console.SetOut(_out);
               System.Console.SetError(_err);
               _out = null;
               _err = null;
            }
         }
      }

      #region Nested type: ConsoleOutputStream

      /// <summary>
      ///   This class provides an output stream that redirects to our console. This is used for hooking stdout and
      ///   stderr.
      /// </summary>
      internal class ConsoleOutputStream : Stream
      {
         private readonly bool _isErrorStream;
         private readonly byte[] _littlebuf = new byte[1];
         private readonly StreamWriter _orgOutStreamWriter;

         /// <summary>
         ///   CTOR
         /// </summary>
         /// <param name = "isErrorStream">if true then this is the Error stream
         /// </param>
         internal ConsoleOutputStream(bool isErrorStream, StreamWriter org)
         {
            _isErrorStream = isErrorStream;
            _orgOutStreamWriter = org;
         }

         public override Boolean CanRead
         {
            get { return false; }
         }

         //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override Boolean CanSeek
         {
            get { return false; }
         }

         public override Boolean CanWrite
         {
            get { return true; }
         }

         //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override Int64 Length
         {
            get { return 0; }
         }

         //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override Int64 Position
         {
            get { return 0; }

            set { }
         }

         // we keep a buffer around for creating 1-char strings, to
         // avoid the potential horror of thousads of array allocations
         // per second

         // Redirect output to the console
         internal void WriteByte(int b)
         {
            _littlebuf[0] = (byte)b;
            String s = Encoding.Default.GetString(_littlebuf, 0, 1);
            print(s);
         }

         //UPGRADE_TODO: The differences in the Expected value  of parameters for method 'WriteByte'  may cause compilation errors.  "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1092'"
         public override void WriteByte(byte b)
         {
            WriteByte(b);
         }

         // Redirect output to the console
         //UPGRADE_NOTE: The equivalent of method 'java.io.OutputStream.write' is not an override method. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1143'"
         internal void write(byte[] b)
         {
            String s = Encoding.Default.GetString(b, 0, b.Length);
            print(s);
         }

         // Redirect output to the console
         public override void Write(Byte[] b, int off, int len)
         {
            String s = Encoding.Default.GetString(b, off, len);
            print(s);
         }

         /// <param name = "s">the string to print
         /// </param>
         private void print(String s)
         {
            if (_isErrorStream)
               ConsoleWindow.getInstance().printErr(s);
            else
               ConsoleWindow.getInstance().printOut(s);

            // print to the original stream too
            try
            {
               _orgOutStreamWriter.Write(s);
               _orgOutStreamWriter.Flush();
            }
            catch (IOException)
            {
            }
         }

         // nothing need be done here
         public override void Flush()
         {
         }

         // nothing need be done here
         public override void Close()
         {
         }

         //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override Int64 Seek(Int64 offset, SeekOrigin origin)
         {
            return 0;
         }

         //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override void SetLength(Int64 value)
         {
         }

         //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
         public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
         {
            return 0;
         }

         //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1232'"
      }

      #endregion

      #region Nested type: ConsoleWindow

      internal class ConsoleWindow : Form
      {
         // RTF constants
         private const String RTF_TOKEN = @"\rtf";
         private const String RTF_BLACK = @"\cf0 ";
         private const String RTF_RED = @"\cf1 ";
         private const String RTF_COLOR_TABLE = @"{\colortbl ;\red255\green0\blue0;}";
         private static ConsoleWindow _instance;
         private bool _rtfHasRed; // Was the color table added to the Rtf
         private RichTextBox _textBox;

         /// <summary>
         ///   CTOR
         /// </summary>
         private ConsoleWindow()
         {
            createContents();
         }

         /// <summary>
         ///   Clear the Rtf buffer and the color table flag
         /// </summary>
         private void ClearRtf()
         {
            // Whenever the window is hidden, we clear the text
            _textBox.Rtf = "";
            _rtfHasRed = false;
         }

         /// <summary>
         ///   Clear the Rtf
         /// </summary>
         /// <param name = "sender"></param>
         /// <param name = "e"></param>
         private void onClickClear(Object sender, EventArgs e)
         {
            ClearRtf();
         }

         /// <summary>
         ///   'closing' the form - only hides it
         /// </summary>
         /// <param name = "sender"></param>
         /// <param name = "e"></param>
         private void onClickClose(Object sender, EventArgs e)
         {
            Visible = false;
            ClearRtf();
         }

         /// <summary>
         ///   Adds text to the Rtf. If needed (1st error, in red)- add color table
         /// </summary>
         /// <param name = "s"></param>
         internal void addText(String s)
         {
            if (_rtfHasRed || !s.StartsWith(RTF_RED))
               _textBox.Rtf = _textBox.Rtf.Insert(_textBox.Rtf.Length - 3, s);
            else
            {
               // We need to insert the color table into the RTF header, then add the colored string and only 
               // after that it can be assigned to the textbox Rtf property
               StringBuilder rtfWithColor = new StringBuilder(_textBox.Rtf);

               // Insert color table before the 1st '{' after the rtf definition
               rtfWithColor.Insert(_textBox.Rtf.IndexOf('{', _textBox.Rtf.IndexOf(RTF_TOKEN)), RTF_COLOR_TABLE);

               // Add the new string in the right place
               rtfWithColor.Insert(_textBox.Rtf.Length - 3, s);

               _textBox.Rtf = rtfWithColor.ToString();
               _rtfHasRed = true;
            }
         }

         /// <summary>
         ///   on closing the form - hide it, don't close it
         /// </summary>
         /// <param name = "e"></param>
         protected override void OnFormClosing(FormClosingEventArgs e)
         {
            Visible = false;
            ClearRtf();
            e.Cancel = true;
            base.OnFormClosing(e);
         }

         /// <summary>
         ///   create the contents of the form
         /// </summary>
         private void createContents()
         {
            Size = new Size(640, 480);
            Text = "RC Console";

            // Text box on form
            _textBox = new RichTextBox();
            _textBox.Font = new Font("Courier New", 10, FontStyle.Regular);
            _textBox.Bounds = ClientRectangle;
            _textBox.Dock = DockStyle.Fill;
            _textBox.ReadOnly = true;
            _textBox.Rtf = "";
            _textBox.ScrollBars = RichTextBoxScrollBars.Both;
            Controls.Add(_textBox);

            // menu bar
            MenuItem[] fileMenuItems = new MenuItem[2];
            fileMenuItems[0] = new MenuItem("Clea&r", onClickClear);
            fileMenuItems[1] = new MenuItem("&Close", onClickClose);

            MenuItem[] fileMenuItem = new MenuItem[1];
            fileMenuItem[0] = new MenuItem("&File", fileMenuItems);

            Menu = new MainMenu(fileMenuItem);
         }

         /// <summary>
         ///   Returns the instance of this singleton class
         /// </summary>
         /// <returns>
         /// </returns>
         internal static ConsoleWindow getInstance()
         {
            if (_instance == null)
            {
               lock (typeof(ConsoleWindow))
               {
                  if (_instance == null)
                  {
                     _instance = new ConsoleWindow();
                     hookStandards();
                  }
               }
            }
            return _instance;
         }

         /// <summary>
         ///   Toggles the console window visibility
         /// </summary>
         internal void toggleVisibility()
         {
            Visible = !Visible;
            // Clear the text if window is hidden
            if (!Visible)
               ClearRtf();
         }

         /// <summary>
         ///   Prints a string to the console
         /// </summary>
         /// <param name = "s">the string to print
         /// </param>
         internal void printOut(string s)
         {
            // Don't update form if window is hidden
            if (Visible && !Disposing)
            {
               Commands.addAsync(CommandType.PROP_SET_TEXT, this, 0, s, 0);
            }
         }

         /// <summary>
         ///   Prints an Error string (red color) to the console
         /// </summary>
         /// <param name = "s">
         /// </param>
         internal void printErr(string s)
         {
            // Don't update form if window is hidden
            if (Visible && !Disposing)
            {
               // Add the 'red' tag before the text, and the 'unred' tag at the end of it. 
               StringBuilder sb = new StringBuilder(RTF_RED);
               sb.Append(s);
               sb.Append(RTF_BLACK);

               Commands.addAsync(CommandType.PROP_SET_TEXT, this, 0, sb.ToString(), 0);
            }
         }
      }

      #endregion
   }
}