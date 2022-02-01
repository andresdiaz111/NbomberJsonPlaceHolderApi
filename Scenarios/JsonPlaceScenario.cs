using System;
using System.Net.Http;

using Newtonsoft.Json;
using NBomber;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;

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
                            .WithLoadSimulations(Simulation.InjectPerSec(rate: 1, TimeSpan.FromSeconds(10)));
            NBomberRunner
                .RegisterScenarios(scenario)
                .WithTestSuite("JsonPlace Example")
                .WithTestName("Basic Method")
                .WithReportFileName("JsonPlaceApi")
                .Run();


        }
    }
}