using com.magicsoftware.util.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Tests.UniUtils.TestUtils;
using System.Reflection;

namespace Tests.UniUtils
{
    
    
    /// <summary>
    ///This is a test class for ConsoleLogWriterTest and is intended
    ///to contain all ConsoleLogWriterTest Unit Tests
    ///</summary>
   [TestClass()]
   public class ConsoleLogWriterTest
   {


      private TestContext testContextInstance;

      /// <summary>
      ///Gets or sets the test context which provides
      ///information about and functionality for the current test run.
      ///</summary>
      public TestContext TestContext
      {
         get
         {
            return testContextInstance;
         }
         set
         {
            testContextInstance = value;
         }
      }

      #region Additional test attributes
      // 
      //You can use the following additional attributes as you write your tests:
      //
      //Use ClassInitialize to run code before running the first test in the class
      //[ClassInitialize()]
      //public static void MyClassInitialize(TestContext testContext)
      //{
      //}
      //
      //Use ClassCleanup to run code after all tests in a class have run
      //[ClassCleanup()]
      //public static void MyClassCleanup()
      //{
      //}
      //
      //Use TestInitialize to run code before running each test
      //[TestInitialize()]
      //public void MyTestInitialize()
      //{
      //}
      //
      //Use TestCleanup to run code after each test has run
      //[TestCleanup()]
      //public void MyTestCleanup()
      //{
      //}
      //
      #endregion


      /// <summary>
      ///A test for Flush
      ///</summary>
      [TestMethod()]
      public void FlushTest()
      {
         ConsoleLogWriter target = new ConsoleLogWriter(); 
         var consoleOut = new TestTextWriter();
         Console.SetOut(consoleOut);
         target.Flush();
         Assert.IsTrue(consoleOut.FlushWasInvoked);
      }

      /// <summary>
      ///A test for Write
      ///</summary>
      [TestMethod()]
      public void WriteTest()
      {
         ConsoleLogWriter target = new ConsoleLogWriter(); 
         var consoleOut = new TestTextWriter();
         Console.SetOut(consoleOut);
         target.Write("Hello");
         Assert.IsTrue(consoleOut.WriteWasInvoked);
      }

      /// <summary>
      ///A test for WriteLine
      ///</summary>
      [TestMethod()]
      public void WriteLineTest()
      {
         ConsoleLogWriter target = new ConsoleLogWriter();
         var consoleOut = new TestTextWriter();
         Console.SetOut(consoleOut);
         target.WriteLine("Hello");
         Assert.IsTrue(consoleOut.WriteLineWasInvoked);
      }
   }

   class TestTextWriter : TextWriter
   {
      MethodInvocationFlags<TestTextWriter> invocationFlags = new MethodInvocationFlags<TestTextWriter>(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

      public bool WriteWasInvoked { get { return invocationFlags.MethodWasInvoked("Write"); } }
      public bool WriteLineWasInvoked { get { return invocationFlags.MethodWasInvoked("WriteLine"); } }
      public bool FlushWasInvoked { get { return invocationFlags.MethodWasInvoked("Flush"); } }


      public override void Close()
      {
         invocationFlags.SignalMethodWasInvoked();
      }

      public override void Write(string value)
      {
         invocationFlags.SignalMethodWasInvoked();
      }

      public override void WriteLine(string value)
      {
         invocationFlags.SignalMethodWasInvoked();
      }

      public override void Flush()
      {
         invocationFlags.SignalMethodWasInvoked();
      }

      public override System.Text.Encoding Encoding
      {
         get { return System.Text.Encoding.UTF32; }
      }
   }
}
