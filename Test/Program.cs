using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Strava.Activities;
using Strava.Athletes;
using Strava.Authentication;
using Strava.Clients;
using RestSharp;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Strava.NET test");

            try
            {
                // Get exceptions from inside the StravaConnect
                StravaConnect().GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            Console.WriteLine("Finished...");
            Console.ReadLine();
        }

        static WebAuthentication auth;
        private static async Task<string> StravaConnect()
        {
            List<MAFData> data = new List<MAFData>();
            DateTime start = new DateTime();

            //StaticAuthentication auth = new StaticAuthentication("37f5eeeae3d0d2484861ed25ccb52ea63adb41ee ");
            auth = new WebAuthentication();

            if (auth.AuthCode == null || auth.AuthCode.Length < 1) // auth.AccessToken == null || auth.AccessToken.Length < 1 ||
            {
                // Request authentication, needs Administrators rights
                Console.WriteLine("Requesting permission");
                auth.AuthCodeReceived += Auth_AuthCodeReceived;
                auth.AccessTokenReceived += Auth_AccessTokenReceived;
                auth.GetTokenAsync("37659", "21d2a6ec68c28870375ef64e2dbac30ef331ab57", Scope.Full);
                
                // **** Wait for reply but how?
                Thread.Sleep(5000);

                // Write Auth code to memory
                if (auth.AuthCode.Length > 1 && auth.AccessToken.Length > 1)
                {
                    Properties.Settings.Default.Token = auth.AccessToken;
                    Properties.Settings.Default.AuthCode = auth.AuthCode;

                    Properties.Settings.Default.Save();
                }else
                {
                    Console.WriteLine(" Auth code did not arrive in time");
                    throw new ArgumentException("AuthCode Missing!");
                }
            }
            else
            {
                // We have code already in memory
                //auth.AccessToken = Properties.Settings.Default.Token;
                auth.AuthCode = Properties.Settings.Default.AuthCode;
            }

            StravaClient client = new StravaClient(auth);            

            var b = await client.Athletes.GetAthleteAsync();
            Console.Write("Getting athlete: " + b.FirstName + " ");
            start = DateTime.Now;

            // Get last x days of activities
            var activities = client.Activities.GetActivities(System.DateTime.Now.Subtract(new System.TimeSpan(360, 0, 0, 0)), System.DateTime.Now);
            Console.WriteLine(" Total of " + activities.Count() + " activities");
            
            //List<Strava.Activities.ActivitySummary> activities = new List<ActivitySummary>();
            //ActivitySummary test = new ActivitySummary();
            //test.Id = 2344450602; // Mattojuoksu
            //test.Id = 2606121265; // 10 + 50 + 10min
            //test.Id = 2344450396; // Aurajoen yöjuoksu
            //activities.Add(test);
            

            foreach (var activity in activities)
            {
                Console.WriteLine(" - Activity: " + activity.Id);

                // Request activity data
                Activity a = client.Activities.GetActivity(activity.Id.ToString(), true);
                Console.Write("  - Type: " + a.Type.ToString() + " Distance " + a.Distance.ToString() + " Elapsed: " + a.ElapsedTimeSpan.ToString());

                // Add activity to data
                data.Add(new MAFData(activity.Id.ToString(), a.DateTimeStart, a.Distance/1000.0f));
                data.First().Athlete = b.FirstName + " " + b.LastName;
                data.First().avgCadence = a.AverageCadence * 2.0f;
                data.First().avgHeartrate = a.AverageHeartrate;
                data.First().avgSpeed = a.AverageSpeed * 3.6f;
                data.First().avgTemperature = a.AverageTemperature;
                data.First().Calories = a.Calories;
                data.First().Elapsed = a.ElapsedTimeSpan;
                data.First().SufferScore = (int)a.SufferScore;
                
                // Get activity laps
                var laps = client.Activities.GetActivityLaps(activity.Id.ToString());
                Console.WriteLine(" " + laps.Count() + " laps");

                float distance = 0;
                foreach(var lap in laps)
                {
                    //Console.WriteLine("   Lap " + lap.LapIndex + " " + lap.ElapsedTimeSpan.ToString() + " " + lap.MovingTimeSpan.ToString() + " " + (lap.AverageCadence * 2.0).ToString("N0") + "spm " + lap.AverageHeartrate + "bpm " + (lap.AverageSpeed * 3.6).ToString("N2") + "km/h " + lap.Distance + "m " + lap.MaxHeartrate + "bpm " + lap.TotalElevationGain + "m ");

                    // Cumulative distance for the activity
                    distance += lap.Distance;

                    // Fill in lap data
                    MAFLap m = new MAFLap();
                    m.avgCadence = lap.AverageCadence * 2.0f;
                    m.avgHeartrate = lap.AverageHeartrate;
                    m.avgSpeed = lap.AverageSpeed * 3.6f;
                    m.cumDistance = distance / 1000.0f;
                    m.Length = lap.Distance;
                    m.Duration = lap.MovingTimeSpan;
                    m.ElevationGain = lap.TotalElevationGain;
                    m.Lap = lap.LapIndex;
                    m.maxHeartrate = lap.MaxHeartrate;
                    // Add lap to activity
                    data.First(i => i.ActivityID == activity.Id.ToString()).Laps.Add(m);
                }                

                // Get activity stream
                start = DateTime.Now;
                //Console.WriteLine();
                Console.Write("  - Getting activity stream... ");
                var stream = client.Streams.GetActivityStream(activity.Id.ToString(), (Strava.Streams.StreamType)2047, Strava.Streams.StreamResolution.High);

                foreach(var s in stream)
                {
                    //Console.WriteLine("   Data stream Type: " + s.StreamType + " samples: " + s.Data.Count().ToString());

                    // Add data to activity memory - Sorting handeled internally
                    data.First(i => i.ActivityID == activity.Id.ToString()).SetStreamData = s;
                }
                if(stream != null && stream.First().Data != null)
                    Console.WriteLine(" " + stream.First().Data.Count() + " samples");

                //Console.WriteLine();
            }
            Console.WriteLine(" - Done in " + DateTime.Now.Subtract(start).ToString());

            // SAVE ACTIVITY-DATA
            start = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Printing acitivity data - Total of " + data.Count().ToString() + " activities");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"activities.csv"))
            {
                file.WriteLine("Timestamp;Activity;Athlete;TotalDistance;avgCadence;avgHeartrate;avgSpeed;avgTemperature;TotalCalories;SufferScore;TotalDuration;LapsCount");
                foreach (var d in data)
                {
                    d.ToString();
                }
            }
            Console.WriteLine(" - Done in " + DateTime.Now.Subtract(start).ToString());

            // SAVE LAP-DATA
            start = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Printing acitivity data - Total of " + data.Sum(i => i.LapCount).ToString() + " laps");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"laps.csv"))
            {
                file.WriteLine("Timestamp;Activity;Lap;Distance;Length;Duration;Cadence;Heartrate;Speed;ElevationGain;MaxHeartrate");
                foreach (var d in data)
                {
                    foreach (var l in d.Laps)
                    {
                        file.WriteLine(d.Timestamp + ";" + d.ActivityID + ";" + l.ToString());
                    }
                }
            }
            Console.WriteLine(" - Done in " + DateTime.Now.Subtract(start).ToString());

            // SAVE SPLIT-DATA
            start = DateTime.Now;
            Console.WriteLine();            
            float splits = 0.25f;

            Console.WriteLine("Printing activity data with custom split lengths - Total of " + data.Sum(i => i.GetCustomSplits(splits).Count()).ToString() + " splits of " + splits.ToString("N3") + " km");
            Console.WriteLine(" - Total of " + data.Sum(i => i.GetCustomSplits(splits).Sum(j => j.Distance)).ToString() + " km");

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"splits.csv"))
            {
                file.WriteLine("Timestamp;Activity;Time;Altitude;Velocity;Cadence;Grade;Heartrate;Temperature;Distance;Moving");
                //Console.WriteLine("Time;Altitude;Velocity;Cadence;Grade;Heartrate;Temperature;Distance;Moving");
                foreach (var d in data)
                {
                    foreach (var l in d.GetCustomSplits(splits))
                    {
                        //Console.WriteLine(l.ToString());
                        file.WriteLine(d.Timestamp + ";" + d.ActivityID + ";" + l.ToString());
                    }
                }
            }
            Console.WriteLine(" - Done in " + DateTime.Now.Subtract(start).ToString());

            return "";
        }

        private static void Auth_AuthCodeReceived(object sender, AuthCodeReceivedEventArgs e)
        {            
            auth.AuthCode = e.AuthCode;
            Console.WriteLine("-> Auth code received: " + e.AuthCode);

            // Request actual token
            var client = new RestClient("https://www.strava.com/oauth/token");
            var request = new RestRequest("resource/{id}", Method.POST);
            request.AddParameter("client_id", "37659");
            request.AddParameter("client_secret", "21d2a6ec68c28870375ef64e2dbac30ef331ab57");
            request.AddParameter("code", e.AuthCode);
            request.AddParameter("grant_type", "authorization_code");
            //request.AddHeader("header", "value");

            // execute the request
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            string a = content.ToString();
        }

        private static void Auth_AccessTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            auth.AccessToken = e.Token;
            Console.WriteLine(" -> Token received: " + e.Token);
        }

        public class JSONAccessTokenReply
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
            public Athlete athlete { get; set; }
            public string refresh_token { get; set; }
            public int expires_at { get; set; }
            public string state { get; set; }
        }
    }
}
