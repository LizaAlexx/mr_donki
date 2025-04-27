using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CouchDB.Driver.Types;

namespace first_attempt.Models
{
    public class CMEEvent : CouchDocument
    {
        public string activityID { get; set; }
        public string catalog { get; set; }
        public string startTime { get; set; }
        public List<Instrument> instruments { get; set; }
        public string sourceLocation { get; set; }
        public int? activeRegionNum { get; set; }
        public string note { get; set; }
        public string submissionTime { get; set; }
        public int versionId { get; set; }
        public string link { get; set; }

        public List<CmeAnalysis>? cmeAnalyses { get; set; }
        public List<LinkedEvent>? linkedEvents { get; set; }
    }

    public class Instrument
    {
        public string displayName { get; set; }
    }
    public class CmeAnalysis
    {
        public bool isMostAccurate { get; set; }
        public string time21_5 { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public double? halfAngle { get; set; }
        public double? speed { get; set; }
        public string type { get; set; }
        public string featureCode { get; set; }
        public string? imageType { get; set; }
        public string measurementTechnique { get; set; }
        public string note { get; set; }
        public int levelOfData { get; set; }
        public double? tilt { get; set; }
        public double? minorHalfWidth { get; set; }
        public double? speedMeasuredAtHeight { get; set; }
        public string submissionTime { get; set; }
        public string link { get; set; }

        public List<EnlilEntry>? enlilList { get; set; }
    }

    public class EnlilEntry
    {
        public string modelCompletionTime { get; set; }
        public double au { get; set; }
        public string? estimatedShockArrivalTime { get; set; }
        public string? estimatedDuration { get; set; }
        public double? rmin_re { get; set; }
        public double? kp_18 { get; set; }
        public double? kp_90 { get; set; }
        public double? kp_135 { get; set; }
        public double? kp_180 { get; set; }
        public bool isEarthGB { get; set; }
        public string link { get; set; }
        public List<ImpactEntry>? impactList { get; set; }
        public List<string> cmeIDs { get; set; }
    }

    public class ImpactEntry
    {
        public bool isGlancingBlow { get; set; }
        public string location { get; set; }
        public string arrivalTime { get; set; }
    }

    public class LinkedEvent
    {
        public string activityID { get; set; }
        //public string activityType { get; set; }
    }
    
}