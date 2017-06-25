using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using com.magicsoftware.util.fsm;

namespace Tests.UniUtils
{
    
    
    /// <summary>
    ///This is a test class for StateTransitionTest and is intended
    ///to contain all StateTransitionTest Unit Tests
    ///</summary>
   [TestClass()]
   public class StateTransitionTest
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
      ///A test for StateTransition Constructor
      ///</summary>
      [TestMethod()]
      public void StateTransitionConstructorTest()
      {
         object nextStateId = new Object();
         StateTransition target = new StateTransition(nextStateId);
         Assert.AreEqual(nextStateId, target.NextStateId);
         Assert.IsNotNull(target.TransitionTriggers);
      }

      /// <summary>
      ///A test for AttachToTriggers
      ///</summary>
      [TestMethod()]
      [DeploymentItem("FSM.dll")]
      public void TriggerEventsTest()
      {
         object nextStateId = new Object();
         StateTransition target;
         PrivateObject privateObj;
         StateArtifactsFactories.CreateTransitionPrivateObject(nextStateId, out target, out privateObj);

         var triggers = StateArtifactsFactories.CreateTriggerList(3);

         target.AddTriggers(triggers);

         bool eventRaised;

         target.TransitionTriggered += new EventHandler<StateTransitionTriggeredEventArgs>((sender, args) =>
         {
            eventRaised = true;
         });

         target.Setup();

         foreach (TestTrigger trigger in triggers)
         {
            eventRaised = false;
            trigger.TriggerNow();
            Assert.IsTrue(eventRaised, "'Triggered' event not raised.");
         }

         target.Cleanup();

         foreach (TestTrigger trigger in triggers)
         {
            eventRaised = false;
            try
            {
               trigger.TriggerNow();
               Assert.Fail("The test trigger did not raise an exception although it should have.");
            }
            catch (AssertFailedException)
            {
               // This is the correct path of execution.
               // the Test Triggers throw an assertion if you try to trigger them before
               // setup.
            }
            finally
            {
               // Now, just make sure nothing triggered the event.
               Assert.IsFalse(eventRaised, "'Triggered' event not raised.");
            }
         }
      }
   }
}
