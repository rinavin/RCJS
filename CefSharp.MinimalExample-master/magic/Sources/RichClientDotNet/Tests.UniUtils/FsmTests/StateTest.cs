using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using com.magicsoftware.util.fsm;

namespace Tests.UniUtils
{
    
    
    /// <summary>
    ///This is a test class for StateTest and is intended
    ///to contain all StateTest Unit Tests
    ///</summary>
   [TestClass()]
   public class StateTest
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
      ///A test for Enter
      ///</summary>
      [TestMethod()]
      public void EnterTest()
      {
         State target = StateArtifactsFactories.CreateState("abcd");
         Assert.IsNotNull(target.Transitions);
         var transitions = StateArtifactsFactories.CreateTransitionList("a", "b", "c");
         foreach (var t in transitions)
         {
            t.AddTrigger(StateArtifactsFactories.CreateTrigger());
         }
         target.AddTransitions(transitions);

         target.DefaultTransition = StateArtifactsFactories.CreateTransition("d");

         target.Enter();
         Assert.IsTrue(((TestState)target).OnEnterInvoked, "OnEnter was not called");
         foreach (TestTransition t in transitions)
         {
            Assert.IsTrue(t.SetupInvoked, "Setup was not called for {0}", t);
         }
      }

      /// <summary>
      ///A test for Leave
      ///</summary>
      [TestMethod()]
      public void LeaveTest()
      {
         State target = StateArtifactsFactories.CreateState("abcd");
         var transitions = StateArtifactsFactories.CreateTransitionList("a", "b", "c");
         foreach (var t in transitions)
         {
            t.AddTrigger(StateArtifactsFactories.CreateTrigger());
         }
         target.AddTransitions(transitions);

         target.DefaultTransition = StateArtifactsFactories.CreateTransition("d");

         target.Leave();
         Assert.IsTrue(((TestState)target).OnLeaveInvoked, "OnLeave was not called");
         foreach (TestTransition t in transitions)
         {
            Assert.IsTrue(t.CleanupInvoked, "Cleanup was not called for {0}", t);
         }
      }

      /// <summary>
      ///A test for Id
      ///</summary>
      [TestMethod()]
      [DeploymentItem("FSM.dll")]
      public void IdTest()
      {
         object identifier = new Object();
         State target = StateArtifactsFactories.CreateState(identifier);
         Assert.AreEqual(identifier, target.Id);
      }
   }

}
