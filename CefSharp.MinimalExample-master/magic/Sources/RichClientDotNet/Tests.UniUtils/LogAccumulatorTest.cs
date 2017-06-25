using com.magicsoftware.util.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Tests.UniUtils
{
    
    
    /// <summary>
    ///This is a test class for LogAccumulatorTest and is intended
    ///to contain all LogAccumulatorTest Unit Tests
    ///</summary>
   [TestClass()]
   public class LogAccumulatorTest
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
      ///A test for Discard
      ///</summary>
      [TestMethod()]
      public void DiscardTest()
      {
         var supersededWriter = new TestLogWriter();
         LogAccumulator.DisposalCallback disposalCallback = null;
         LogAccumulator target = new LogAccumulator(supersededWriter, disposalCallback);

         string message = "Hello";
         target.WriteLine(message);

         target.Discard();

         var actual = supersededWriter.Output.ToString().Replace(Environment.NewLine, "$n");
         Assert.AreEqual("", actual, "(Note: $n replaces new line characters)");

         PrivateObject privateObj = new PrivateObject(target);
         Assert.AreEqual("", privateObj.GetFieldOrProperty("accumulatedMessages").ToString());
      }

      /// <summary>
      ///A test for Dispose
      ///</summary>
      [TestMethod()]
      public void DisposeTest()
      {
         var supersededWriter = new TestLogWriter();
         bool callbackCalled = false;
         LogAccumulator.DisposalCallback disposalCallback = () => { callbackCalled = true; };
         LogAccumulator target = new LogAccumulator(supersededWriter, disposalCallback);

         target.Dispose();

         Assert.IsTrue(callbackCalled);

         callbackCalled = false;

         target.Dispose();
         Assert.IsFalse(callbackCalled);
      }

      /// <summary>
      ///A test for Flush
      ///</summary>
      [TestMethod()]
      public void FlushTest()
      {
         var supersededWriter = new TestLogWriter();
         LogAccumulator.DisposalCallback disposalCallback = null;
         LogAccumulator target = new LogAccumulator(supersededWriter, disposalCallback);

         string message = "Hello";
         target.WriteLine(message);
         
         target.Flush();
         var expected = message + "$n";
         var actual = supersededWriter.Output.ToString().Replace(Environment.NewLine, "$n");
         Assert.AreEqual(expected, actual, "(Note: $n replaces new line characters)");

         PrivateObject privateObj = new PrivateObject(target);
         Assert.AreEqual("", privateObj.GetFieldOrProperty("accumulatedMessages").ToString());
      }

      /// <summary>
      ///A test for WriteLine
      ///</summary>
      [TestMethod()]
      public void WriteLineTest()
      {
         ILogWriter supersededWriter = new TestLogWriter();
         LogAccumulator.DisposalCallback disposalCallback = null;
         LogAccumulator target = new LogAccumulator(supersededWriter, disposalCallback);
         string message = "Hello";
         target.WriteLine(message);

         PrivateObject privateObj = new PrivateObject(target);
         Assert.AreEqual(message + Environment.NewLine, privateObj.GetFieldOrProperty("accumulatedMessages").ToString());

         target.WriteLine(message);
         Assert.AreEqual(message + Environment.NewLine + message + Environment.NewLine, privateObj.GetFieldOrProperty("accumulatedMessages").ToString());
      }

      /// <summary>
      ///A test for SupersededWriter
      ///</summary>
      [TestMethod()]
      public void SupersededWriterTest()
      {
         var logWriter = new TestLogWriter();
         var accumulator = new LogAccumulator(logWriter, null);
         Assert.AreSame(logWriter, accumulator.SupersededWriter);
      }

   }
}
