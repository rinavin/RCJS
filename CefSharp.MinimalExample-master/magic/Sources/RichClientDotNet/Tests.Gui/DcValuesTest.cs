using com.magicsoftware.unipaas.management.data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.env;
using com.magicsoftware.unipaas;

namespace Tests.Gui
{
    
    
    /// <summary>
    ///This is a test class for DcValuesTest and is intended
    ///to contain all DcValuesTest Unit Tests
    ///</summary>
   [TestClass()]
   public class DcValuesTest
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
      ///A test for DcValues Constructor
      ///</summary>
      [TestMethod()]
      public void DcValuesConstructorTest()
      {
         DcValues target = new DcValues(false, false);
         Assert.AreEqual(0, target.getId());
         target = new DcValues(true, false);
         Assert.AreEqual(DcValues.EMPTY_DCREF, target.getId());
      }

      /// <summary>
      ///A test for SetDisplayValues
      ///</summary>
      [TestMethod()]
      public void DisplayValuesTest()
      {
         DcValues target = new DcValues(false, false);
         Assert.IsNull(target.getDispVals());
         string[] displayValues = { "DV1", "DV2", "DV3" };
         target.SetDisplayValues(displayValues);
         Assert.IsNotNull(target.getDispVals());
         Assert.AreNotSame(displayValues, target.getDispVals());
         CompareArrays(displayValues, target.getDispVals());
      }


      /// <summary>
      ///A test for SetLinkValues
      ///</summary>
      [TestMethod()]
      public void LinkValuesTest()
      {
         Manager.Environment = new TestEnvironment();
         
         DcValues target = new DcValues(false, false);
         PrivateObject privateObj = new PrivateObject(target);
         Assert.IsNull(target.GetLinkVals());
         
         string[] strValues = new string[] { "A", "B", "C", "D" };
         TestLinkValues(target, strValues, StorageAttribute.ALPHA);
         Assert.IsNull(privateObj.GetField("_numVals"));

         string[] intValues = new string[] { "00000000000000000001", "0000000000000000002A", "0000000000000000003B", "00000000000000000014" };
         TestLinkValues(target, intValues, StorageAttribute.NUMERIC);
         Assert.IsNotNull(privateObj.GetField("_numVals"));
      }

      void TestLinkValues(DcValues target, string[] linkValues, StorageAttribute type)
      {
         target.setType(type);
         target.SetLinkValues(linkValues);
         Assert.IsNotNull(target.GetLinkVals());
         Assert.AreNotSame(linkValues, target.GetLinkVals());
         CompareArrays(linkValues, target.GetLinkVals());
      }

      /// <summary>
      ///A test for decRefCount
      ///</summary>
      [TestMethod()]
      [ExpectedException(typeof(ApplicationException))]
      public void RefCountTest()
      {
         DcValues target = new DcValues(false, false);
         Assert.IsFalse(target.HasReferences);
         target.AddReference();
         Assert.IsTrue(target.HasReferences);
         target.AddReference();
         Assert.IsTrue(target.HasReferences);
         target.RemoveReference();
         Assert.IsTrue(target.HasReferences);
         target.RemoveReference();
         Assert.IsFalse(target.HasReferences);

         // Should throw an exception.
         target.RemoveReference();
      }


      /// <summary>
      ///A test for getIndexOf
      ///</summary>
      [TestMethod()]
      public void getIndexOfTest()
      {
         Manager.Environment = new TestEnvironment();

         DcValues target = new DcValues(false, false);

         string[] strValues = new string[] { "A", "B", "C", "D" };
         target.setType(StorageAttribute.ALPHA);
         target.SetLinkValues(strValues);
         int[] indice;
         indice = target.getIndexOf("A", false, false, null, null, true);
         Assert.AreEqual(1, indice.Length);
         Assert.AreEqual(0, indice[0]);
         
         indice = target.getIndexOf("B,E,D", false, false, null, null, true);
         Assert.AreEqual(3, indice.Length);
         Assert.AreEqual(1, indice[0]);
         Assert.AreEqual(DcValues.NOT_FOUND, indice[1]);
         Assert.AreEqual(3, indice[2]);

         indice = target.getIndexOf("B,E,D", false, false, new String[] {"E"}, null, true);
         Assert.AreEqual(3, indice.Length);
         Assert.AreEqual(2, indice[0]);
         Assert.AreEqual(0, indice[1]);
         Assert.AreEqual(4, indice[2]);

         //string[] intValues = new string[] { "00000000000000000001", "0000000000000000002A", "0000000000000000003B", "00000000000000000014" };
      }

      /// <summary>
      ///A test for getLinkValue
      ///</summary>
      [TestMethod()]
      public void getLinkValueTest()
      {
         DcValues target = new DcValues(false, false);
         string actual = target.getLinkValue(0);
         Assert.AreEqual(null, actual);
      }

      /// <summary>
      ///A test for isNull
      ///</summary>
      [TestMethod()]
      [ExpectedException(typeof(IndexOutOfRangeException))]
      public void NullFlagsTest()
      {
         DcValues target = new DcValues(false, false);
         Assert.IsFalse(target.isNull(0));
         Assert.IsFalse(target.isNull(4));
         Assert.IsFalse(target.isNull(60));

         bool[] nullFlags = new bool[] { true, false, false, true, false, false };
         target.setNullFlags(nullFlags);

         Assert.IsTrue(target.isNull(0));
         Assert.IsFalse(target.isNull(4));
         Assert.IsFalse(target.isNull(5));

         // Should throw an exception.
         Assert.IsFalse(target.isNull(55));
      }

      /// <summary>
      ///A test for setType
      ///</summary>
      [TestMethod()]
      public void SetTypeTest()
      {
         DcValues target = new DcValues(false, false);
         StorageAttribute type = StorageAttribute.ALPHA;
         target.setType(type);
         Assert.AreEqual(type, target.GetAttr());
         type = StorageAttribute.NUMERIC;
         target.setType(type);
         Assert.AreEqual(type, target.GetAttr());
      }


      private void CompareArrays(Array a1, Array a2)
      {
         Assert.AreEqual(a1.Length, a2.Length);
         for (int i = 0; i < a1.Length; i++)
         {
            Assert.AreEqual(a1.GetValue(i), a2.GetValue(i), "Items at position " + i + " do not match.");
         }
      }

   }
}
