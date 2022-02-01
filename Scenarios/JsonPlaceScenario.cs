using System;
using System.Net.Http;

using Newtonsoft.Json;
using NBomber;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using NBomber.Sinks.InfluxDB;

using NbomberJsonPlaceHolderApi.Models;

namespace NbomberJsonPlaceHolderApi.Scenarios
{
    public class JsonPlaceScenario
    {
        public static void Run()
        {
            string url = "https://jsonplaceholder.typicode.com/";
            var httpFactory = HttpClientFactory.Create();
            var data = FeedData.FromJson<PostIds>("./DataFeed/PostIds.json");
            var dataFeed = Feed.CreateCircular("postids", data);

            var GetPostsById = Step.Create("GetPostsById", feed: dataFeed, clientFactory: httpFactory, execute: async context =>
            {
                context.Logger.Information($"PostId: {context.FeedItem.id}");
                var request = Http.CreateRequest("GET", url + $"posts/{context.FeedItem.id}");
                var response = await Http.Send(request, context);
                return response;
            });

            var PostAPost = Step.Create("PostAPost", clientFactory: httpFactory, execute: async context =>
            {
                var prevResponse = context.GetPreviousStepResponse<HttpResponseMessage>();
                var body = await prevResponse.Content.ReadAsStringAsync();
                var postData = JsonConvert.DeserializeObject<PostData>(body);
                var request = Http.CreateRequest("POST", url + "posts").WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent($"\"title\" : \"{postData.title}\""));
                var response = await Http.Send(request, context);
                return response;
            });

            var scenario = ScenarioBuilder.CreateScenario("JsonPlaceApi", GetPostsById, PostAPost)
                            .WithLoadSimulations(Simulation.InjectPerSec(rate: 2, TimeSpan.FromSeconds(40)));

            var influxConfig = InfluxDbSinkConfig.Create("http://localhost:8086", database: "default");
            var influxDb = new InfluxDBSink(influxConfig);
            NBomberRunner
                .RegisterScenarios(scenario)
                .WithTestSuite("reporting")
                .WithTestName("influx_test")
                .WithReportFileName("JsonPlaceApi")
                .WithReportingSinks(new[] {influxDb})
                .WithReportingInterval(TimeSpan.FromSeconds(100))
                .Run();


        }
    }
}