using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.recording
{
   
   /// <summary>
   /// recorder for application definition data
   /// </summary>
   internal class ApplicationDefinitionsRecorder : RecorderBase<ApplicationDefinitions>
   {
      public override string FileName
      {
         get
         {
            return serializer.FileName;
         }
         set
         {
            serializer.FileName = value;
         }
      }

      public string XMLString
      {
         get
         {
            return serializer.XMLString;
         }
         set
         {
            serializer.XMLString = value;
         }
      }



      ClassSerializer serializer = new ClassSerializer() ;
      ApplicationDefinitionsData applicationDefinitionData = new ApplicationDefinitionsData();

      protected override void Add(ApplicationDefinitions t)
      {
         applicationDefinitionData.ListDataSourceDefinitions = new List<DataSourceDefinition>();
         applicationDefinitionData.ListDataSourceDefinitions.AddRange(t.DataSourceDefinitionManager.DataSourceDefinitions.Values);
         applicationDefinitionData.ListDataBaseDefinitions = new List<DatabaseDefinition>();
         applicationDefinitionData.ListDataBaseDefinitions.AddRange(t.DatabaseDefinitionsManager.databaseDefinitions.Values);
      }

      /// <summary>
      /// saves data to file
      /// </summary>
      public override void Save()
      {
         serializer.SerializeToFile(applicationDefinitionData);
      }

      public override Object Load()
      {
         applicationDefinitionData = (ApplicationDefinitionsData)serializer.Deserialize(typeof(ApplicationDefinitionsData));
         ApplicationDefinitions applicationDefinitions = new ApplicationDefinitions();
         foreach (var item in applicationDefinitionData.ListDataBaseDefinitions)
         {
            applicationDefinitions.DatabaseDefinitionsManager.Add(item.Name.ToUpper(), item);
         }

         foreach (var datasource in applicationDefinitionData.ListDataSourceDefinitions)
         {
            PrepareDataSourceDefinition(datasource);
            applicationDefinitions.DataSourceDefinitionManager.DataSourceDefinitions.Add(datasource.Id, datasource);
         }
         return applicationDefinitions;

      }

      /// <summary>
      /// prepare datasorce definition to use the same objects
      /// </summary>
      /// <param name="d"></param>
      void PrepareDataSourceDefinition(DataSourceDefinition d)
      {

         foreach (var seg in d.Segments)
         {
            int fieldIndex = seg.Field.IndexInRecord;
            seg.Field = d.Fields[fieldIndex];
         }

         foreach (var item in d.Keys)
         {
            List<DBSegment> segs = item.Segments;
            for (int i = 0; i < segs.Count; i++)
            {
               segs[i] = d.Segments.Find(x => x.Isn == segs[i].Isn);
            }
         }
      }

   }

   /// <summary>
   /// class which holds relevabt ApplicationDefinitionsData
   /// </summary>
   public class ApplicationDefinitionsData
   {
      public List<DataSourceDefinition> ListDataSourceDefinitions { get; set; }
      public List<DatabaseDefinition> ListDataBaseDefinitions { get; set; }

   }
}
