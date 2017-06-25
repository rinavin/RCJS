using com.magicsoftware.util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using com.magicsoftware.util.Xml;

namespace Tests.UniUtils
{


   /// <summary>
   ///This is a test class for XMLparserTest and is intended
   ///to contain all XMLparserTest Unit Tests
   ///</summary>
   [TestClass()]
   public class XMLparserTest
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
      ///A test for RemoveXmlElements
      ///</summary>
      public void TestRemoveXmlElements(string[] elementTagsToRemove, string expectedXML)
      {
         string xmlBuffer = originalXMl;
         string actual;
         actual = XmlParser.RemoveXmlElements(xmlBuffer, elementTagsToRemove);
         Assert.AreEqual(expectedXML, actual);
      }

      // xml to remove parts of
      string originalXMl =
        "<top attr1=\"1\", attr2=\"b\">" +
           "<simpleinner attr1=\"1\"/>" +
           "<complexinner attr1=\"1\">" +
           "</complexinner>" +
           "<simpleinner attr1=\"1\"/>" +
           "<complexinner attr1=\"1\">" +
              "<nested> </nested>" +
           "</complexinner>" +
        "</top>";

      [TestMethod]
      public void RemoveEmptyElementTest()
      {
         TestRemoveXmlElements(new string[0], originalXMl);
      }

      [TestMethod]
      public void RemoveNonExistingElementTest()
      {
         TestRemoveXmlElements(new string[] { "aa" }, originalXMl);
      }

      [TestMethod]
      public void RemoveAttributeTag()
      {
         TestRemoveXmlElements(new string[] { "attr1" }, originalXMl);
      }

      [TestMethod]
      public void RemoveNestedElementTest()
      {
         TestRemoveXmlElements(new string[] { "nested" }, "<top attr1=\"1\", attr2=\"b\">" +
        "<simpleinner attr1=\"1\"/>" +
        "<complexinner attr1=\"1\">" +
        "</complexinner>" +
        "<simpleinner attr1=\"1\"/>" +
        "<complexinner attr1=\"1\">" +
        "</complexinner>" +
        "</top>");
      }

      [TestMethod]
      public void RemoveSimpleInnerElement()
      {
         TestRemoveXmlElements(new string[] { "simpleinner" }, "<top attr1=\"1\", attr2=\"b\">" +
              "<complexinner attr1=\"1\">" +
              "</complexinner>" +
              "<complexinner attr1=\"1\">" +
              "<nested> </nested>" +
              "</complexinner>" +
              "</top>");
      }

      [TestMethod]
      public void RemoveComplexInnerElement()
      {
         TestRemoveXmlElements(new string[] { "complexinner" }, "<top attr1=\"1\", attr2=\"b\">" +
                 "<simpleinner attr1=\"1\"/>" +
                 "<simpleinner attr1=\"1\"/>" +
                 "</top>");
      }

      [TestMethod]
      public void RemoveTheDocumentElementTest()
      {
         TestRemoveXmlElements(new string[] { "top" }, "");
      }

      [TestMethod]
      public void RemoveTwoElementsTest()
      {
         TestRemoveXmlElements(new string[] { "simpleinner", "complexinner" }, "<top attr1=\"1\", attr2=\"b\">" + "</top>");
         TestRemoveXmlElements(new string[] { "complexinner", "simpleinner" }, "<top attr1=\"1\", attr2=\"b\">" + "</top>");
      }

      [TestMethod]
      public void RemoveNonExistingElementAfterExistingElement()
      {
         TestRemoveXmlElements(new string[] { "complexinner", "notfound" }, "<top attr1=\"1\", attr2=\"b\">" +
        "<simpleinner attr1=\"1\"/>" +
        "<simpleinner attr1=\"1\"/>" +
        "</top>");
      }
   }
}
