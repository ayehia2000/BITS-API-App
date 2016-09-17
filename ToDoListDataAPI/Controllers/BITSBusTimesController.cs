using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using BITSBusTimesDataAPI.Models;
using System.Xml.Linq;
using Microsoft.Azure; // Namespace for CloudConfigurationManager 
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BITSBusTimesDataAPI.Controllers
{
    public class BITSBusTimesController : ApiController
    {
        // Uncomment following lines for service principal authentication
        //private static string trustedCallerClientId = ConfigurationManager.AppSettings["todo:TrustedCallerClientId"];
        //private static string trustedCallerServicePrincipalId = ConfigurationManager.AppSettings["todo:TrustedCallerServicePrincipalId"];

        //private static Dictionary<int, BITSBusTime> mockData = new Dictionary<int, BITSBusTime>();
        // private static Dictionary<int, int, string, int, string> mockDB = new Dictionary<int, BITSBusTime>();

       // Define range of hours - prior to request - to look for buses
        public static int timeSpan = -5; //-72; //Used for simulation

        public static int totalSeats = 60;

        //public static DateTime requestTime = DateTime.UtcNow; // new DateTime(2016, 9, 14, 1, 35, 00);//for debugging

        public static CloudStorageAccount storageAccountBITS;

        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                System.Diagnostics.Debug.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }
        public static List<Lines> LinesServing(string orig, string dest)
        {
            List<Lines> pAvailableLines = new List<Lines>();
            List<Lines> pLinesOrigDest = new List<Lines>();
            List<Lines> pLinesOrig = new List<Lines>();


            //System.Diagnostics.Debug.WriteLine(storageAccount.Credentials.KeyName);

            // Create the table client.
            CloudTableClient tableClient = storageAccountBITS.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("LineStops");

            // Create the table query.
            TableQuery<LineStopsEntity> rangeQuery = new TableQuery<LineStopsEntity>().Where(
                    TableQuery.GenerateFilterCondition("StopID", QueryComparisons.Equal, orig));

            // Loop through the results, displaying information about the entity.
            foreach (LineStopsEntity entity in table.ExecuteQuery(rangeQuery))
            {
                System.Diagnostics.Debug.WriteLine("{0}, {1}\t{2}", entity.PartitionKey, entity.RowKey, entity.StopID);
                Lines pLine = new Lines();
                pLine.LineID = entity.PartitionKey;
                pLine.StopOrder = entity.RowKey;
                pLine.StopID = entity.StopID;
                pLinesOrig.Add(pLine);
            }

            // Check if destination is in route
            foreach (Lines Line in pLinesOrig)
            {
                System.Diagnostics.Debug.WriteLine(Line.LineID);
                rangeQuery = new TableQuery<LineStopsEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Line.LineID),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("StopID", QueryComparisons.Equal, dest)));

                var linesFound = table.ExecuteQuery(rangeQuery);
                if (linesFound != null && linesFound.GetEnumerator().MoveNext())
                {
                    foreach (LineStopsEntity entity in linesFound)
                    {
                        System.Diagnostics.Debug.WriteLine("Dest: {0} in {1}", entity.StopID, entity.PartitionKey);
                        Lines pLine = new Lines();
                        pLine.LineID = entity.PartitionKey;
                        pLine.StopOrder = entity.RowKey;
                        pLine.StopID = entity.StopID;
                        pLinesOrigDest.Add(pLine);
                    }
                }
            }

            //Check if dest StopOrder is lower than orig StopOrder ??

            // Create the CloudTable object that represents the "BusJourney" table.
            CloudTable transTable = tableClient.GetTableReference("BusJourneyInfo");

            foreach (Lines Line in pLinesOrigDest)
            {
                int origStopOrder = 0;
                int lastStopOrder = 0;

                // Get origin stop order
                rangeQuery = new TableQuery<LineStopsEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Line.LineID),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("StopID", QueryComparisons.Equal, orig)));
                foreach (LineStopsEntity entity in table.ExecuteQuery(rangeQuery))
                {
                    origStopOrder = Int32.Parse(entity.RowKey);
                }

                //Get line availablity , all stops during last period
                System.Diagnostics.Debug.WriteLine(Line.LineID);
                TableQuery<BusStatusEntity> transQuery = new TableQuery<BusStatusEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterConditionForLong("lineid", QueryComparisons.Equal, Int64.Parse(Line.LineID)),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForDate("EventProcessedUtcTime", QueryComparisons.GreaterThan, DateTime.UtcNow.AddHours(timeSpan))));

                //Get last stop
                IEnumerable<BusStatusEntity> linesFound = transTable.ExecuteQuery(transQuery);
                if (linesFound != null && linesFound.GetEnumerator().MoveNext())
                {
                    DateTime maxDate = DateTime.MinValue;
                    string lastStopID = "";
                    BusStatusEntity pJourney = new BusStatusEntity();

                    foreach (BusStatusEntity entity in linesFound)
                    {
                        if (entity.EventProcessedUtcTime > maxDate)
                        {
                            maxDate = entity.EventProcessedUtcTime;
                            lastStopID = entity.stationid.ToString();
                            pJourney = entity;
                        }
                    }

                    // Get last stop order
                    rangeQuery = new TableQuery<LineStopsEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Line.LineID),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("StopID", QueryComparisons.Equal, lastStopID)));
                    foreach (LineStopsEntity entity in table.ExecuteQuery(rangeQuery))
                    {
                        lastStopOrder = Int32.Parse(entity.RowKey);
                    }

                    if (lastStopOrder < origStopOrder) //Valid choice
                    {
                        // Get all this-journey records (by Journey ID in same day) , calculate seating
                        long seatsTaken = 0;
                        transQuery = new TableQuery<BusStatusEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterConditionForLong("journeyid", QueryComparisons.Equal, pJourney.journeyid),
                            TableOperators.And,
                            TableQuery.GenerateFilterConditionForDate("EventProcessedUtcTime", QueryComparisons.GreaterThanOrEqual, AbsoluteStart(pJourney.EventProcessedUtcTime))));

                        foreach (BusStatusEntity entity in transTable.ExecuteQuery(transQuery))
                        {
                            System.Diagnostics.Debug.WriteLine("SeatIN: {0} SeatOUT: {1}", entity.NumInSensor, entity.NumOutSensor);
                            seatsTaken = seatsTaken + entity.NumInSensor - entity.NumOutSensor;
                        }

                        Lines pLine = new Lines();
                        pLine.LineID = Line.LineID;
                        pLine.StopOrder = lastStopOrder.ToString();
                        pLine.StopID = lastStopID;
                        pLine.TimeStarted = pJourney.EventProcessedUtcTime;
                        pLine.SeatsBooked = (int)seatsTaken;
                        pLine.BusID = pJourney.busid.ToString();
                        pAvailableLines.Add(pLine);
                    }
                }
            }

            return pAvailableLines; //line, last stop, timeleft, seats

        }

        private static DateTime AbsoluteStart(DateTime dateTime)
        {
            return dateTime.Date;
        }

        private static int CalcTravelTime(string olng, string olat, string dlng, string dlat)
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

        public static List<StopLocationEntity> aStops;
        public static int GetTimes(string orig, string dest)
        {
            // return mockData.Values.Where(m => m.OriginID == orig && m.DestinationID == dist);
            var stopOrig = aStops.Where(a => a.RowKey.Equals(orig)).First();
            var stopDest = aStops.Where(a => a.RowKey.Equals(dest)).First();

            int travelTime = CalcTravelTime(stopOrig.Latitude, stopOrig.Longitude,
                                            stopDest.Latitude, stopDest.Longitude);

            //int travelTime = CalcTravelTime("31.2001", "29.9187", "30.0444", "31.2357");

            return travelTime;
        }
        static BITSBusTimesController()
        {
            /*
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            System.Diagnostics.Debug.WriteLine(storageAccount.Credentials.KeyName);

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
                System.Diagnostics.Debug.WriteLine("{0}, {1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey,
                    entity.NextStopID);
            }

            */

        }

        //private static void CheckCallerId()
        //{
            // Uncomment following lines for service principal authentication
            //string currentCallerClientId = ClaimsPrincipal.Current.FindFirst("appid").Value;
            //string currentCallerServicePrincipalId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            //if (currentCallerClientId != trustedCallerClientId || currentCallerServicePrincipalId != trustedCallerServicePrincipalId)
            //{
            //    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The appID or service principal ID is not the expected value." });
            //}
        //}
        /*
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
        */
        // GET: api/ToDoItemList
        //public IEnumerable<BITSBusTime> Get(int orig, int dist)
        public List<BITSBusTime> Get(int orig, int dest)
        {
            System.Diagnostics.Debug.WriteLine("Bus ITS Monitor\n");


            string oStop = orig.ToString();
            string dStop = dest.ToString();

            List<BITSBusTime> requestBITS = new List<BITSBusTime>();


            storageAccountBITS = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            List<Lines> LinesList = LinesServing(oStop, dStop);

            // Create the table client.
            CloudTableClient client = storageAccountBITS.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable linesTable = client.GetTableReference("LineStops");
            CloudTable stopsTable = client.GetTableReference("StopLocation");

            List<LineStopsEntity> aLines = linesTable.ExecuteQuery(new TableQuery<LineStopsEntity>()).ToList();
            aStops = stopsTable.ExecuteQuery(new TableQuery<StopLocationEntity>()).ToList();

            BITSBusTime resultBITS;

            foreach (Lines line in LinesList)
            {
                System.Diagnostics.Debug.WriteLine("\n Potential Line: \t{0} Stop: {1} Order: {2} Started: {3} Seats: {4}",
                    line.LineID, line.StopID, line.StopOrder, line.TimeStarted, line.SeatsBooked);

                resultBITS = new BITSBusTime();

                resultBITS.OriginID = oStop;
                resultBITS.DestinationID = dStop;
                resultBITS.BusID = line.BusID;
                resultBITS.AvailableSeats = (totalSeats - line.SeatsBooked).ToString();

                List<LineStopsEntity> selectedStops = new List<LineStopsEntity>();

                selectedStops = aLines.Where(a => a.PartitionKey.Equals(line.LineID)).ToList();

                selectedStops = selectedStops.OrderBy(s => int.Parse(s.RowKey)).ToList();

                bool startTimes = false;
                string prevStop = null;
                int arrivalTimeSec = 0;

                //Sum up arrival time
                foreach (LineStopsEntity stop in selectedStops)
                {
                    if (stop.StopID == line.StopID) { startTimes = true; }
                    if (startTimes)
                    {
                        if (prevStop != null)
                        {
                            arrivalTimeSec = arrivalTimeSec + GetTimes(prevStop, stop.StopID);
                            prevStop = stop.StopID;

                            System.Diagnostics.Debug.WriteLine("To {0}: {1} Seconds", stop.StopID, arrivalTimeSec);

                            if (stop.StopID == oStop)
                            {
                                System.Diagnostics.Debug.WriteLine("\nArrival in  {0} Seconds \n", arrivalTimeSec);
                                break;
                            }
                        }
                        prevStop = stop.StopID;
                    }
                }


                //Reset conditions for re-use
                startTimes = false;
                prevStop = null;
                int travelTimeSec = 0;

                //Sum up Travel Time
                foreach (LineStopsEntity stop in selectedStops)
                {
                    if (stop.StopID == oStop) { startTimes = true; }
                    if (startTimes)
                    {
                        if (prevStop != null)
                        {
                            travelTimeSec = travelTimeSec + GetTimes(prevStop, stop.StopID);
                            prevStop = stop.StopID;

                            System.Diagnostics.Debug.WriteLine("To {0}: {1} Seconds", stop.StopID, travelTimeSec);

                            if (stop.StopID == dStop)
                            {
                                System.Diagnostics.Debug.WriteLine("\nTravel in  {0} Seconds \n", travelTimeSec);
                                break;
                            }
                        }
                        prevStop = stop.StopID;
                    }
                }

                //Deduct time consumed from leaving last stop till this second, just before sending to user

                //requestTime = DateTime.UtcNow;
                // For testing we set requestTime = new DateTime(2016, 9, 14, 1, 35, 00);

                arrivalTimeSec = arrivalTimeSec - (int)(DateTime.UtcNow - line.TimeStarted).TotalSeconds;

                TimeSpan t = TimeSpan.FromSeconds(arrivalTimeSec);
                string answer = string.Format("{0:D1} hours and {1:D1} minutes",
                                t.Hours,
                                t.Minutes);
                resultBITS.ArrivalTime = answer;

                t = TimeSpan.FromSeconds(travelTimeSec);
                answer = string.Format("{0:D1} hours and {1:D1} minutes",
                                t.Hours,
                                t.Minutes);
                resultBITS.TravelTime = answer;

                requestBITS.Add(resultBITS);
            }

            var jsonString = JsonConvert.SerializeObject(
                       requestBITS, Formatting.Indented,
                       new JsonConverter[] { new StringEnumConverter() });

            System.Diagnostics.Debug.WriteLine(jsonString);
            //Console.Read();

            return requestBITS;

            //CheckCallerId();

            // return mockData.Values.Where(m => m.OriginID == orig && m.DestinationID == dist);
            /*
            int travelTime = CalcTravelTime("31.2001", "29.9187", "30.0444", "31.2357");

            TimeSpan t = TimeSpan.FromSeconds(travelTime);

            string answer = string.Format("{0:D1} hours and {1:D1} minutes",
                            t.Hours,
                            t.Minutes);

            return answer; */
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

