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
            Console.WriteLine(" - Athlete: " + b.FirstName);

            var activities = client.Activities.GetActivities(System.DateTime.Now.Subtract(new System.TimeSpan(7, 0, 0, 0)), System.DateTime.Now);
            foreach (var activity in activities)
            {
                Console.WriteLine(" - Activity: " + activity.Id);

                // Request activity data
                Activity a = client.Activities.GetActivity(activity.Id.ToString(), true);

                Console.WriteLine("  - Type: " + a.Type.ToString());
                Console.WriteLine("  - Elapsed time: " + a.ElapsedTimeSpan.ToString());
                Console.WriteLine("  - Distance: " + a.Distance.ToString());
                Console.WriteLine();
            }
           
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
