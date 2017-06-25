using com.magicsoftware.unipaas.management.data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests.Gui
{
    
    
    /// <summary>
    ///This is a test class for ObjectReferenceBaseTest and is intended
    ///to contain all ObjectReferenceBaseTest Unit Tests
    ///</summary>
   [TestClass()]
   public class ObjectReferenceBaseTest
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
      ///A test for Dispose
      ///</summary>
      [TestMethod()]
      public void SuccessfulDisposeTest()
      {
         TestItem item = new TestItem();
         Assert.IsFalse(item.HasReferences);
         TestObjectReference ref1 = new TestObjectReference(item);
         Assert.IsTrue(item.HasReferences);
         TestObjectReference ref2 = new TestObjectReference(item);
         Assert.IsTrue(item.HasReferences);
         ref1.Dispose();
         Assert.IsTrue(item.HasReferences);
         ref2.Dispose();
         Assert.IsFalse(item.HasReferences);
      }

      /// <summary>
      ///A test for Finalize
      ///</summary>
      [TestMethod()]
      [Priority(-999)]
      public void SuccessfulFinalizeTest()
      {
         TestItem item = new TestItem();
         Assert.IsFalse(item.HasReferences);
         TestObjectReference ref1 = new TestObjectReference(item);
         Assert.IsTrue(item.HasReferences);
         TestObjectReference ref2 = new TestObjectReference(item);
         Assert.IsTrue(item.HasReferences);
         ref1 = null;
         GC.Collect();
         GC.WaitForPendingFinalizers();
         Assert.IsTrue(item.HasReferences);
         ref2 = null;
         GC.Collect();
         GC.WaitForPendingFinalizers();
         Assert.IsFalse(item.HasReferences);
      }
   }

   class TestObjectReference : ObjectReferenceBase
   {
      public TestObjectReference(IReferencedObject referent): base(referent)
      {

      }

      public override ObjectReferenceBase Clone()
      {
         return new TestObjectReference(Referent);
      }
   }

   class TestItem : IReferencedObject
   {
      int refCount = 0;

      public void AddReference()
      {
         refCount++;
      }

      public void RemoveReference()
      {
         refCount--;
      }

      public bool HasReferences
      {
         get { return refCount > 0; }
      }
   }

}
