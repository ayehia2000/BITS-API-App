namespace BITSBusTimesDataAPI.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    public class LineStopsEntity : TableEntity
    {
        public LineStopsEntity(string lineId, int orderId)
        {
            this.PartitionKey = lineId;
            this.RowKey = orderId.ToString();
        }

        public LineStopsEntity() { }
        public string StopID { get; set; }
    }
    public class BITSBusTime
    {
        public string OriginID { get; set; }
        public string DestinationID { get; set; }
        public string BusID { get; set; }
        public string ArrivalTime { get; set; }
        public string TravelTime { get; set; }
        public string AvailableSeats { get; set; }
    }

    public class BusStatusEntity : TableEntity
    {
        public BusStatusEntity() { }
        public BusStatusEntity(string pKey, string rKey)
        {
            this.PartitionKey = pKey;
            this.RowKey = rKey;
        }

        public DateTime EventProcessedUtcTime { get; set; }
        public Int64 journeyid { get; set; }
        public Int64 NumInSensor { get; set; }
        public Int64 NumOutSensor { get; set; }
        public Int64 busid { get; set; }
        public Int64 stationid { get; set; }
        public Int64 lineid { get; set; }

    }
    public class Lines
    {
        public string LineID;
        public string StopOrder;
        public string StopID;
        public DateTime TimeStarted;
        public int SeatsBooked;
        public string BusID;
    }

    public class StopLocationEntity : TableEntity
    {
        public StopLocationEntity() { }
        public StopLocationEntity(string partId, string stopId)
        {
            this.PartitionKey = partId;
            this.RowKey = stopId;
        }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
    
}
