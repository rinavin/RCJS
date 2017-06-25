using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.magicsoftware.util.Logging;
using Tests.UniUtils.TestUtils;
using System.Reflection;

namespace Tests.UniUtils
{
   /// <summary>
   /// Log writer for some of the logger tests.
   /// </summary>
   class TestLogWriter : ILogWriter
   {
      public StringBuilder Output { get; private set; }

      MethodInvocationFlags<TestLogWriter> invocationFlags = new MethodInvocationFlags<TestLogWriter>(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

      public bool WriteWasInvoked { get { return invocationFlags.MethodWasInvoked("Write"); } }
      public bool WriteLineWasInvoked { get { return invocationFlags.MethodWasInvoked("WriteLine"); } }
      public bool FlushWasInvoked { get { return invocationFlags.MethodWasInvoked("Flush"); } }

      public TestLogWriter()
      {
         Output = new StringBuilder();
      }

      public void Write(string message)
      {
         Output.Append(message);
         invocationFlags.SignalMethodWasInvoked();
      }

      public void WriteLine(string message)
      {
         Output.AppendLine(message);
         invocationFlags.SignalMethodWasInvoked();
      }

      public void Flush()
      {
         invocationFlags.SignalMethodWasInvoked();
      }

      public void ResetInvocationIndicators()
      {
         invocationFlags.Reset();
      }

      internal void ClearOutput()
      {
         Output = new StringBuilder();
      }
   }
}
