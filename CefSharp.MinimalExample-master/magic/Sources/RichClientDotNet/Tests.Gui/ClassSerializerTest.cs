using util.com.magicsoftware.util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.util;
using System.Collections.Generic;

namespace Tests.Gui
{
    
    
    /// <summary>
    ///This is a test class for ClassSerializerTest and is intended
    ///to contain all ClassSerializerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ClassSerializerTest
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


        public class SampleClass
        {
            public string A { get; set; }
            public int B { get; set; }
        }


        [TestMethod()]
        public void SerializeToStringTest()
        {
            ClassSerializer target = new ClassSerializer(); // TODO: Initialize to an appropriate value
            SampleClass s = new SampleClass() { A = "value a", B = 5};
            string result = target.SerializeToString(s);

            string expected = "<?xml version=\"1.0\"?>\r\n<SampleClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <A>value a</A>\r\n  <B>5</B>\r\n</SampleClass>";
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        ///A test for SerializeFieldToStringTest
        ///</summary>
        [TestMethod()]
        public void SerializeFieldToStringTest()
        {
            ClassSerializer target = new ClassSerializer(); // TODO: Initialize to an appropriate value
            DBField field = new DBField();
            field.DbName = "myname";
            string result = target.SerializeToString(field);
            string expected = @"<?xml version=""1.0""?>
<DBField xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Isn>0</Isn>
  <IndexInRecord>0</IndexInRecord>
  <Attr>0</Attr>
  <AllowNull>false</AllowNull>
  <DefaultNull>false</DefaultNull>
  <Length>0</Length>
  <DiffUpdate>0</DiffUpdate>
  <Dec>0</Dec>
  <Whole>0</Whole>
  <PartOfDateTime>0</PartOfDateTime>
  <DefaultStorage>false</DefaultStorage>
  <DbName>myname</DbName>
</DBField>";
            Assert.AreEqual(expected, result);
        }

         [TestMethod()]
        public void SerializeDatabaseToStringTest()
        {
            ClassSerializer target = new ClassSerializer(); // TODO: Initialize to an appropriate value
            DataSourceDefinition d = new DataSourceDefinition();
            d.Name = "tablename";
            d.Fields = new List<DBField>();
            d.Fields.Add(new DBField() {DbName = "Name1", Storage=FldStorage.AlphaLString });
            d.Fields.Add(new DBField() {DbName = "Name2", Storage=FldStorage.NumericCharDec });
            d.Keys = new List<DBKey>();
            d.Keys.Add(new DBKey() { KeyDBName = "Key1" });
            string result = target.SerializeToString(d);
            string expected = @"<?xml version=""1.0""?>
<DataSourceDefinition xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Fields>
    <DBField>
      <Isn>0</Isn>
      <IndexInRecord>0</IndexInRecord>
      <Attr>0</Attr>
      <AllowNull>false</AllowNull>
      <DefaultNull>false</DefaultNull>
      <Storage>AlphaLString</Storage>
      <Length>0</Length>
      <DiffUpdate>0</DiffUpdate>
      <Dec>0</Dec>
      <Whole>0</Whole>
      <PartOfDateTime>0</PartOfDateTime>
      <DefaultStorage>false</DefaultStorage>
      <DbName>Name1</DbName>
    </DBField>
    <DBField>
      <Isn>0</Isn>
      <IndexInRecord>0</IndexInRecord>
      <Attr>0</Attr>
      <AllowNull>false</AllowNull>
      <DefaultNull>false</DefaultNull>
      <Storage>NumericCharDec</Storage>
      <Length>0</Length>
      <DiffUpdate>0</DiffUpdate>
      <Dec>0</Dec>
      <Whole>0</Whole>
      <PartOfDateTime>0</PartOfDateTime>
      <DefaultStorage>false</DefaultStorage>
      <DbName>Name2</DbName>
    </DBField>
  </Fields>
  <Keys>
    <DBKey>
      <Segments />
      <KeyDBName>Key1</KeyDBName>
      <Isn>0</Isn>
      <Flags>0</Flags>
    </DBKey>
  </Keys>
  <Segments />
  <Id CtlIdx=""0"" Isn=""0"" />
  <Name>tablename</Name>
  <Flags>0</Flags>
  <PositionIsn>0</PositionIsn>
  <ArraySize>0</ArraySize>
  <RowIdentifier>0</RowIdentifier>
  <CheckExist>0</CheckExist>
  <DelUpdMode>0</DelUpdMode>
</DataSourceDefinition>";
            Assert.AreEqual(expected, result);
        }


            
    }
}
