using System;
using System.IO;
using com.magicsoftware.httpclient;
using com.magicsoftware.util;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Specialized;

namespace Tester
{
   class Tester
   {
      private HttpClient _httpClient = null;
      private ulong _requestsCnt = 0;

      private static ConcurrentDictionary<String, int> _responses = new ConcurrentDictionary<String, int>();

      private const String REQUEST_URL_KEYWORD        = "request_url";     // e.g. request_url http://193.17.74.169/PHP/echo.php
      private const String REQUEST_BODY_KEYWORD       = "request_body";    // e.g. request_body "appname=GSTest&prgname=BATCH&arguments=-N3,-LT"
      private const String REQUEST_HEADERS_KEYWORD    = "request_headers"; // e.g. request_headers "Content-Type: text/xml"
      private const String APPLICATION_NAME_KEYWORD   = "application_name";
      private const String PROGRAM_NAME_KEYWORD       = "program_name";
      private const String ARGUMENTS_KEYWORD          = "arguments";
      private const String STRESS_KEYWORD             = "stress";
      private const String HTTP_TIMEOUT_KEYWORD       = "http_timeout";
      private const String HTTP_PROXY_ADDRESS_KEYWORD = "http_proxy";

      private static String              REQUEST_URL;
      private static String              REQUEST_BODY;
      private static String              REQUEST_HEADERS;
      private static String              REQUEST_CONTENT_TYPE;
      private static String              APPLICATION_NAME;
      private static String              PROGRAM_NAME;
      private static String              ARGUMENTS = "-A";
      private static ushort              HTTP_TIMEOUT_IN_SECONDS = 120;
      private static String              HTTP_PROXY_ADDRESS;
      private static bool                EXECUTE_GET_METHOD = true;
      private static bool                EXECUTE_POST_METHOD = false;
      private static bool                EXECUTE_HEAD_METHOD = false;

      private static ulong TIME_IN_MS = 1;
      private static ushort THREADS_COUNT = 1;
      private static bool RANDOMIZE_APPLICATION_PROGRAM_NAMES; // by each thread, in addition to (after) each valid request - randomizing the application and program names.

      public Tester()
      {
         _httpClient = new HttpClient();
         _httpClient.AddRequestHeaders(REQUEST_HEADERS);
      }

      static void Main(string[] args)
      {
         String command = null;
         String value;
         foreach (var item in args)
         {
            if (command == null)
            {
               command = item;
               continue;
            }
            else
               value = item;

            switch (command.ToLower())
            {
               case REQUEST_URL_KEYWORD:
                  REQUEST_URL = value;
                  break;

               case REQUEST_BODY_KEYWORD:
                  REQUEST_BODY = value;
                  break;

               case HTTP_PROXY_ADDRESS_KEYWORD:
                  HTTP_PROXY_ADDRESS = value;
                  break;

               case HTTP_TIMEOUT_KEYWORD:
                  ushort.TryParse(value, out HTTP_TIMEOUT_IN_SECONDS);
                  break;

               case REQUEST_HEADERS_KEYWORD:
                  REQUEST_HEADERS = value;
                  String[] headersAndValuesArray = REQUEST_HEADERS.Split(';');
                  foreach (var headerAndValueString in headersAndValuesArray)
                  {
                     String[] headerAndValueArray = headerAndValueString.Split(':');
                     if (headerAndValueArray.Length == 2)
                     {
                        if (headerAndValueArray[0].Equals("Content-type", StringComparison.CurrentCultureIgnoreCase))
                        {
                           REQUEST_CONTENT_TYPE = headerAndValueArray[1].Trim();
                           break;
                        }
                     }
                     //else
                     //   Logger.Instance.WriteErrorToLog(String.Format("Invalid header {0} should be formatted 'name: value'", headerAndValueString));
                  }

                  break;

               case "method": // internal/optional.
                  EXECUTE_GET_METHOD = value.ToUpper().Contains("GET");
                  EXECUTE_POST_METHOD = value.ToUpper().Contains("POST");
                  EXECUTE_HEAD_METHOD = value.ToUpper().Contains("HEAD");
                  break;

               case APPLICATION_NAME_KEYWORD:
               case "appname":
                  APPLICATION_NAME = value;
                  break;

               case PROGRAM_NAME_KEYWORD:
               case "prgname":
                  PROGRAM_NAME = value;
                  break;

               case ARGUMENTS_KEYWORD:
                  ARGUMENTS = value;
                  break;

               case STRESS_KEYWORD:
                  String[] commandOptions = value.Split(',');

                  TIME_IN_MS = (ulong)(Math.Max(TIME_IN_MS, ulong.Parse(commandOptions[0]) * 1000));

                  if (commandOptions.Length >= 2)
                  {
                     THREADS_COUNT = Math.Max(THREADS_COUNT, ushort.Parse(commandOptions[1]));

                     if (commandOptions.Length == 3)
                        RANDOMIZE_APPLICATION_PROGRAM_NAMES = (commandOptions[2].ToUpper() == "Y");
                  }
                  break;
            }

            command = value = null;
         }

         // -form- ==> POST only
         if (REQUEST_CONTENT_TYPE != null && REQUEST_CONTENT_TYPE.Equals("application/x-www-form-urlencoded", StringComparison.CurrentCultureIgnoreCase))
         {
            EXECUTE_POST_METHOD = true;
            EXECUTE_GET_METHOD = false;
         }

         com.magicsoftware.httpclient.HttpClientEvents.GetExecutionProperty_Event += GetExecutionProperty;

         if (String.IsNullOrEmpty(REQUEST_URL))
            Console.WriteLine(string.Format("{0} value [{1} value] [{2} value] [{3} value] [{4} value] [{5} value] [{6} time in seconds,threads count]",
                              REQUEST_URL_KEYWORD, REQUEST_BODY_KEYWORD, REQUEST_HEADERS_KEYWORD, APPLICATION_NAME_KEYWORD, PROGRAM_NAME_KEYWORD, ARGUMENTS_KEYWORD, STRESS_KEYWORD));
         else
            ExecuteNThreads();
         Console.WriteLine();
      }

      /// <summary>
      /// </summary>
      /// <param name="propName"></param>
      /// <returns>execution property's value.</returns>
      public static string GetExecutionProperty(string propName)
      {
         return (propName == "UseHighestSecurityProtocol"
                     ? "N"
                     : null);
      }

      /// <summary>
      /// </summary>
      private static void ExecuteNThreads()
      {
         ulong requestsCnt = 0;

         Tester[] bombers = new Tester[THREADS_COUNT];
         Thread[] workerThreads = new Thread[THREADS_COUNT];

         // start the work threads (bombers):
         for (ushort i = 0; i < THREADS_COUNT; ++i)
         {
            bombers[i] = new Tester();
            workerThreads[i] = new Thread(bombers[i].BomberThreadMethod);
            workerThreads[i].Start();
         }

         // wait for the bombers to complete:
         for (ushort i = 0; i < THREADS_COUNT; ++i)
         {
            workerThreads[i].Join();

            requestsCnt += bombers[i]._requestsCnt;
         }

         if (requestsCnt == 1)
         {
            foreach (var item in _responses)
               Console.WriteLine(string.Format("response body: {0}", item.Key));
            Console.WriteLine(string.Format("response headers: {0}", bombers[0]._httpClient.GetLastResponseHeaders()));
         }
         else
         {

            Console.WriteLine(string.Format("{0:#,###,###} requests, over {1} seconds, using {2} threads: throughput = {3:#,###}", 
                              requestsCnt, TIME_IN_MS / 1000, THREADS_COUNT, requestsCnt * 1000 / TIME_IN_MS));
            foreach (var item in _responses)
            {
               Console.WriteLine(string.Format("{0:#,###,###} requests: response body: ", item.Value));
               Console.WriteLine(item.Key);
            }
         }
      }

      /// <summary>
      /// </summary>
      internal void BomberThreadMethod()
      {
         long startTime = Misc.getSystemMilliseconds();

         if (TIME_IN_MS == 1)
            ExecuteHttpRequestBothMethods(APPLICATION_NAME, PROGRAM_NAME, ARGUMENTS);
         else
         {
            while ((ulong)(Misc.getSystemMilliseconds() - startTime) < TIME_IN_MS)
            {
               ExecuteHttpRequestBothMethods(APPLICATION_NAME, PROGRAM_NAME, ARGUMENTS);

               if (RANDOMIZE_APPLICATION_PROGRAM_NAMES)
                  ExecuteHttpRequestBothMethods(RandomizeCharacters(100), RandomizeCharacters(100), RandomizeCharacters(100));
            }
         }
      }

      /// <summary>
      /// </summary>
      /// <param name="maxCharactersCnt"></param>
      /// <returns></returns>
      private static String RandomizeCharacters(ushort maxCharactersCnt)
      {
         Random randomizer = new Random();
         StringBuilder output = new StringBuilder();

         for (int i = 0; i < randomizer.Next(maxCharactersCnt); i++)
            output.Append(randomizer.Next(256));

         return output.ToString();
      }

      /// <summary>
      /// </summary>
      private void ExecuteHttpRequestBothMethods(String applicationName, String programName, String arguments)
      {
         if (EXECUTE_GET_METHOD)
            ExecuteHttpRequest("Get", applicationName, programName, arguments);

         if (EXECUTE_POST_METHOD)
            ExecuteHttpRequest("PoSt", applicationName, programName, arguments);

         if (EXECUTE_HEAD_METHOD)
            ExecuteHttpRequest("HEAD", applicationName, programName, arguments);
      }

      /// <summary>
      /// </summary>
      /// <param name="httpMethod">GET / POST / ..</param>
      /// <param name="applicationName"></param>
      /// <param name="programName"></param>
      /// <param name="arguments"></param>
      /// <returns></returns>
      private byte[] ExecuteHttpRequest(String httpVerb, String applicationName, String programName, String arguments)
      {
         byte[] response = null;

         if (String.IsNullOrEmpty(REQUEST_BODY)
             && !String.IsNullOrEmpty(applicationName) 
             && !String.IsNullOrEmpty(programName))
            REQUEST_BODY = String.Format("appname={0}&prgname={1}&arguments={2}", applicationName, programName, arguments);

         try
         {
            _requestsCnt++;

            StringBuilder responseString = new StringBuilder();

            response = (httpVerb.Equals("GET", StringComparison.CurrentCultureIgnoreCase) || httpVerb.Equals("HEAD", StringComparison.CurrentCultureIgnoreCase)
                           ? _httpClient.ExecuteRequest(httpVerb, REQUEST_URL, REQUEST_BODY, HTTP_TIMEOUT_IN_SECONDS, HTTP_PROXY_ADDRESS)
                           : _httpClient.ExecuteRequest(httpVerb, REQUEST_URL, REQUEST_BODY, HTTP_TIMEOUT_IN_SECONDS, HTTP_PROXY_ADDRESS));

            if (!httpVerb.Equals("HEAD", StringComparison.CurrentCultureIgnoreCase))
               responseString.Append(System.Text.Encoding.UTF8.GetString(response));

            AddOrUpdateResponse(responseString.ToString());
         }
         catch (System.Exception ex)
         {
            //Console.WriteLine(ex);
            AddOrUpdateResponse(ex.Message);
         }

         return response;
      }

      /// <summary>
      /// </summary>
      /// <param name="response"></param>
      private static void AddOrUpdateResponse(String response)
      {
         _responses.AddOrUpdate(response, 1, (k, v) => v + 1);
      }
   }
}
