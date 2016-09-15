using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens;
using System.Diagnostics;
using BITSBusTimesDataAPI.Models;
using System.Configuration;
using System.Xml.Linq;
using Microsoft.Azure; // Namespace for CloudConfigurationManager 
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types

namespace BITSBusTimesDataAPI.Controllers
{
    public class BITSBusTimesController : ApiController
    {
        // Uncomment following lines for service principal authentication
        //private static string trustedCallerClientId = ConfigurationManager.AppSettings["todo:TrustedCallerClientId"];
        //private static string trustedCallerServicePrincipalId = ConfigurationManager.AppSettings["todo:TrustedCallerServicePrincipalId"];

        private static Dictionary<int, BITSBusTime> mockData = new Dictionary<int, BITSBusTime>();
        // private static Dictionary<int, int, string, int, string> mockDB = new Dictionary<int, BITSBusTime>();

        public class LineStopsEntity : TableEntity
        {
            public LineStopsEntity(string lineId, string stopId)
            {
                this.PartitionKey = lineId;
                this.RowKey = stopId;
            }

            public LineStopsEntity() { }
            public string NextStopID { get; set; }
        }
        static BITSBusTimesController()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            Console.WriteLine(storageAccount.Credentials.KeyName);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("LineStops");

            // Create the table query.
            TableQuery<LineStopsEntity> rangeQuery = new TableQuery<LineStopsEntity>().Where(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "5555"));

            // Loop through the results, displaying information about the entity.
            foreach (LineStopsEntity entity in table.ExecuteQuery(rangeQuery))
            {
                Console.WriteLine("{0}, {1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey,
                    entity.NextStopID);
            }

            // mockDB.add 

        }

        private static void CheckCallerId()
        {
            // Uncomment following lines for service principal authentication
            //string currentCallerClientId = ClaimsPrincipal.Current.FindFirst("appid").Value;
            //string currentCallerServicePrincipalId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            //if (currentCallerClientId != trustedCallerClientId || currentCallerServicePrincipalId != trustedCallerServicePrincipalId)
            //{
            //    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The appID or service principal ID is not the expected value." });
            //}
        }

        private static int CalcTravelTime (string olng, string olat, string dlng, string dlat)
        {
          
            var requestUri = string.Format("https://maps.googleapis.com/maps/api/distancematrix/xml?origins=" +
            olng + "," + olat + "&destinations=" + dlng + "," + dlat + "&key=AIzaSyDx9OepXgiWTUy-3pnH00y-obS71q3b_A4");

            var request = WebRequest.Create(requestUri);
            var response = request.GetResponse();
            var xdoc = XDocument.Load(response.GetResponseStream());

            var result = xdoc.Element("DistanceMatrixResponse").Element("row").Element("element");
            var duration = result.Element("duration");
            var sec = Convert.ToInt16(duration.Element("value").Value);
            var str = duration.Element("text").Value;
            return sec;
        }

        // GET: api/ToDoItemList
        //public IEnumerable<BITSBusTime> Get(int orig, int dist)
        public string Get(int orig, int dist)
        {
            CheckCallerId();

            // return mockData.Values.Where(m => m.OriginID == orig && m.DestinationID == dist);

            int travelTime = CalcTravelTime("31.2001", "29.9187", "30.0444", "31.2357");

            TimeSpan t = TimeSpan.FromSeconds(travelTime);

            string answer = string.Format("{0:D1} hours and {1:D1} minutes",
                            t.Hours,
                            t.Minutes);

            return answer;
        }

        // GET: api/ToDoItemList/5
    //    public IEnumerable<BITSBusTime> GetById(int orig, int dist)
    //    {
    //        CheckCallerId();
    //
    //        return mockData.Values.Where(m => m.OriginID == orig && m.DestinationID == dist);
    //    }

        // POST: api/ToDoItemList


     //   public void Put(BITSBusTime BusTime)
     //   {
     //       CheckCallerId();
     //
     //       BITSBusTime xBusTime = mockData.Values.First(a => (a.TravelTime == BusTime.TravelTime || BusTime.TravelTime == "*") && a.OriginID == BusTime.OriginID);
     //       if (BusTime != null && xBusTime != null)
     //       {
     //           xBusTime.ArrivalTime = BusTime.ArrivalTime;
     //       }
     //   }

        // DELETE: api/ToDoItemList/5
     //   public void Delete(int orig, int dest)
     //   {
     //       CheckCallerId();
     //
     //       BITSBusTime BusTime = mockData.Values.First(a => (a.TravelTime == owner || owner == "*") && a.OriginID == dest);
     //       if (BusTime != null)
     //       {
     //           mockData.Remove(BusTime.OriginID);
     //       }
     //   }

    }
}

