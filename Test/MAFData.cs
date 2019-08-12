using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class MAFData
    {
        private const char sep = ';';

        public string ActivityID { get; set; }
        public string Athlete { get; set; }
        public DateTime Timestamp { get; set; }
        public float TotalDistance { get; set; }
        public float avgCadence { get; set; }
        public float avgHeartrate { get; set; }
        public float avgSpeed { get; set; }
        public float avgTemperature { get; set; }
        public float Calories { get; set; }
        public int SufferScore { get; set; }
        public TimeSpan Elapsed { get; set; }

        public List<MAFLap> Laps { get; set; }
        public List<MAFStream> StreamPoints { get; set; }

        /// <summary>
        /// Print activity as string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Timestamp.ToString() + sep + ActivityID + sep + Athlete + sep + TotalDistance + sep +
                avgCadence + sep + avgHeartrate + avgSpeed + sep + avgTemperature + sep + Calories + sep + SufferScore + sep + Elapsed + sep + LapCount;
        }


        /// <summary>
        /// Return Lap-count
        /// </summary>
        public int LapCount
        {
            get
            {
                if (Laps != null)
                    return Laps.Count();

                return 0;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ActivityID"></param>
        /// <param name="Timestamp"></param>
        /// <param name="TotalDistance"></param>
        public MAFData(string ActivityID, DateTime Timestamp, float TotalDistance)
        {
            this.ActivityID = ActivityID;
            this.Timestamp = Timestamp;
            this.TotalDistance = TotalDistance;

            Laps = new List<MAFLap>();
            StreamPoints = new List<MAFStream>();
        }

        /// <summary>
        /// Set stream data points from Strava ActivityStream
        /// </summary>
        public Strava.Streams.ActivityStream SetStreamData
        {
            set
            {
                // Check input validity
                if (value != null && value.Data.Count() > 0)
                {
                    float prev = 0;
                    // Loop through whole data set
                    for (int ind = 0; ind < value.Data.Count(); ind++)
                    {
                        // Check if this sample is already created
                        var s = StreamPoints.ElementAtOrDefault(ind);
                        
                        if(s == null)
                        {
                            // There was no existing data, create new one

                            // Sanity check
                            if (ind > StreamPoints.Count() + 1)
                                Console.WriteLine(" * Incoming data is not in order?");

                            // Add new
                            StreamPoints.Add(new MAFStream());

                            // Get last one of list, should be the same as requested right?
                            s = StreamPoints.Last();
                        }

                        if (s != null)
                        {
                            switch (value.StreamType)
                            {
                                case Strava.Streams.StreamType.Altitude:
                                    s.Altitude = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Cadence:
                                    s.Cadence = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Distance:
                                    s.Distance = ((float)Convert.ToDecimal(value.Data[ind]) - prev) / 1000.0f;
                                    prev = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Grade_Smooth:
                                    s.Grade = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Heartrate:
                                    s.Heartrate = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.LatLng:
                                    // Skip
                                    break;

                                case Strava.Streams.StreamType.Moving:
                                    s.Moving = Convert.ToBoolean(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Temp:
                                    s.Temperature = (float)Convert.ToDecimal(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Time:
                                    s.Time = TimeSpan.FromSeconds(Convert.ToInt32(value.Data[ind]) - prev);
                                    prev = Convert.ToInt32(value.Data[ind]);
                                    break;

                                case Strava.Streams.StreamType.Velocity_Smooth:
                                    s.Velocity = (float)Convert.ToDecimal(value.Data[ind]) * 3.6f;
                                    break;

                                case Strava.Streams.StreamType.Watts:
                                    // Skip
                                    break;

                                default:
                                    Console.WriteLine(" * Unknown Strava StreamType!" + value.StreamType.ToString());
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get custom lengths of split data
        /// </summary>
        /// <param name="SplitDistance">in kilometers as distance is!</param>
        /// <returns></returns>
        public List<MAFStream> GetCustomSplits(float SplitDistance)
        {
            List<MAFStream> data = new List<MAFStream>();
            MAFStream split = new MAFStream();

            for(int ind = 0; ind < StreamPoints.Count(); ind++)
            {
                // Get instance of current sample
                MAFStream c = StreamPoints[ind];

                // Skip empty data sets                    
                if (c.Distance <= 0 || c.Time < new TimeSpan(0,0,1))
                    continue;

                // Need to split from this data sample
                if (split.Distance + c.Distance >= SplitDistance)
                {
                    // Calculate distance to take into account
                    var dist = SplitDistance - split.Distance;

                    // Sanity checking    
                    if (c.Distance < 0 || c.Time < new TimeSpan(0, 0, 0) || dist > c.Distance)
                    {
                        int k = 2 + 2;
                    }
                    if (dist < 0)
                        dist = SplitDistance;
                   
                    // Add to current split
                    split.Altitude = (c.Altitude * dist + split.Altitude * split.Distance) / (dist + split.Distance);
                    split.Cadence = (c.Cadence * dist + split.Cadence * split.Distance) / (dist + split.Distance);
                    split.Grade = (c.Grade * dist + split.Grade * split.Distance) / (dist + split.Distance);
                    split.Heartrate = (c.Heartrate * dist + split.Heartrate * split.Distance) / (dist + split.Distance);
                    split.Temperature = (c.Temperature * dist + split.Temperature * split.Distance) / (dist + split.Distance);
                    split.Velocity = (c.Velocity * dist + split.Velocity * split.Distance) / (dist + split.Distance);

                    if (c.Moving == true)
                        split.Moving = true;

                    // Add only weighted fraction of the time to this split
                    split.Time = TimeSpan.FromMilliseconds(((float)c.Time.TotalMilliseconds * dist + (float)split.Time.TotalMilliseconds * split.Distance) / (dist + split.Distance));
                    split.Distance = dist + split.Distance;

                    // Save data
                    data.Add(split);

                    // Add remainder to a new split(s)
                    // Add also full splits if sample distance is larger than desired subset length
                    do
                    {
                        split = new MAFStream();
                        split.Altitude = c.Altitude;
                        split.Cadence = c.Cadence;

                        // Calculate remaining length of the split
                        var d = c.Distance - dist;
                        if (d > SplitDistance)
                            d = SplitDistance;

                        // Calculate time spent to this split
                        TimeSpan timeSpent = TimeSpan.FromMilliseconds(c.Time.TotalMilliseconds * d / c.Distance);
                        split.Distance = d;
                        split.Grade = c.Grade;
                        split.Heartrate = c.Heartrate;
                        split.Moving = c.Moving;
                        split.Temperature = c.Temperature;
                        split.Time = c.Time - timeSpent; // TimeSpan.FromMilliseconds(((float)c.Time.TotalMilliseconds * c.Distance + (float)split.Time.TotalMilliseconds * split.Distance) / (c.Distance + split.Distance));
                        split.Velocity = c.Velocity;

                        // Decrease c-distance with the amount used already for this split
                        c.Distance -= d;
                        c.Time -= timeSpent;
                        dist = d;

                        if (d >= SplitDistance)
                        {
                            //Console.WriteLine(" Split needs extra round");
                            // Save this data before next lap
                            data.Add(split);

                            int k = 2 + 2;
                        }
                    } while (c.Distance >= SplitDistance);
                }
                else
                {
                    // Weighted ratio of current data set vs data already accumulated in the split

                    // Add to current split
                    split.Altitude = (c.Altitude * c.Distance + split.Altitude * split.Distance) / (c.Distance + split.Distance);
                    split.Cadence = (c.Cadence * c.Distance + split.Cadence * split.Distance) / (c.Distance + split.Distance);
                    split.Grade = (c.Grade * c.Distance + split.Grade * split.Distance) / (c.Distance + split.Distance);
                    split.Heartrate = (c.Heartrate * c.Distance + split.Heartrate * split.Distance) / (c.Distance + split.Distance);
                    split.Temperature = (c.Temperature * c.Distance + split.Temperature * split.Distance) / (c.Distance + split.Distance);
                    split.Velocity = (c.Velocity * c.Distance + split.Velocity * split.Distance) / (c.Distance + split.Distance);

                    if (c.Moving == true)
                        split.Moving = true;

                    split.Time = c.Time + split.Time;
                    split.Distance = c.Distance + split.Distance;
                }
            }

            // Add remainder of data 
            data.Add(split);

            //
            return data;
        }
    }

    class MAFLap
    {
        private const char sep = ';';

        public int Lap { get; set; }
        public float cumDistance { get; set; }
        public float Length { get; set; }
        public TimeSpan Duration { get; set; }
        public float avgCadence { get; set; }
        public float avgHeartrate { get; set; }
        public float avgSpeed { get; set; }
        public float ElevationGain { get; set; }
        public float maxHeartrate { get; set; }

        public override string ToString()
        {
            return Lap.ToString() + sep + cumDistance.ToString() + sep + Length.ToString() + sep + Duration.ToString() + sep + avgCadence.ToString() 
                    + sep + avgHeartrate.ToString() + sep + avgSpeed.ToString() + sep + ElevationGain.ToString() + sep + maxHeartrate.ToString();
        }
    }

    class MAFStream
    {
        private const char sep = ';';

        public float Altitude { get; set; }
        public float Velocity { get; set; }
        public float Cadence { get; set; }
        public float Grade { get; set; }
        public float Heartrate { get; set; }
        public float Temperature { get; set; }
        public float Distance { get; set; }
        public bool Moving { get; set; }
        public TimeSpan Time { get; set; }

        public override string ToString()
        {
            return Time.ToString(@"hh\:mm\:ss") + sep + Altitude.ToString("N1") + sep + Velocity.ToString("N2") + sep + Cadence.ToString("N0") + sep + Grade.ToString("N2")
                    + sep + Heartrate.ToString("N0") + sep + Temperature.ToString("N1") + sep + Distance.ToString("N3") + sep + Moving;
        }
    }
}
