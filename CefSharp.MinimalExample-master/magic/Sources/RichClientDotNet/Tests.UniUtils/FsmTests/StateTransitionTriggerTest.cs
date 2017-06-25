using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using com.magicsoftware.util.fsm;

namespace Tests.UniUtils
{
    
    
    /// <summary>
    ///This is a test class for StateTransitionTriggerTest and is intended
    ///to contain all StateTransitionTriggerTest Unit Tests
    ///</summary>
   [TestClass()]
   public class StateTransitionTriggerTest
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
      ///A test for Cleanup
      ///</summary>
      [TestMethod()]
      public void CleanupTest()
      {
         StateTransitionTrigger target = StateArtifactsFactories.CreateTrigger();
         target.Cleanup();
         Assert.IsTrue(((TestTrigger)target).CleanupInvoked);
      }

      /// <summary>
      ///A test for OnTriggered
      ///</summary>
      [TestMethod()]
      [DeploymentItem("FSM.dll")]
      public void TriggersTest()
      {
         StateTransitionTrigger trigger;
         PrivateObject privateObj;
         StateArtifactsFactories.CreateStateTransitionTriggerPrivateObject(out trigger, out privateObj);

         var target = (TestTrigger)trigger;

         bool eventRaised;

         target.Triggered += (sender, args) => 
            {
               eventRaised = true;
            };

         target.Setup();
         Assert.IsTrue(target.SetupInvoked);

         eventRaised = false;
         privateObj.Invoke("OnTriggered");
         Assert.IsTrue(eventRaised);

         target.Cleanup();
         Assert.IsTrue(target.CleanupInvoked);
      }
   }
}
