using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Use ConfigurationBuilder to read the appsettings.json file
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration config = builder.Build();

        // Get the URL to send the request to
        string url = config.GetValue<string>("Url");

        // Create a CancellationTokenSource
        var cts = new CancellationTokenSource();

        // Create a logger
        var loggerFactory = LoggerFactory.Create(builder => {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole();
        });
        ILogger logger = loggerFactory.CreateLogger<Program>();

        // Create an HttpClient
        //TODO: move httpclien in dependency
        //TODO: add validation on settings
        //TODO: make settings like dependency
        var client = new HttpClient();

        // Send the request and print the response every 10 minutes
        while (!cts.IsCancellationRequested)
        {
            try
            {
                // Send the GET request
                var response = await client.GetAsync(url, cts.Token);
                var t = await response.Content.ReadAsStreamAsync();

                var arr = await JsonSerializer.DeserializeAsync<string[]>(t);
                Console.WriteLine(response);

                var listOfTasks = new List<Task>();

                string pathForSaving = config.GetValue<string>("PathForSavingFiles");
                
                for (int i = 0; i < arr.Length; i++)
                {
                    var url_ = arr[i];
                    //TODO: see course about streams
                    listOfTasks.Add(
                        Task.Run(async () =>
                        {
                            var responseFromWiki = await client.GetAsync(url_);

                            var str = await responseFromWiki.Content.ReadAsStringAsync();
                            var htmlDocument = new HtmlDocument();
                            if (str != null)
                            {
                                
                                htmlDocument.LoadHtml(str);
                            }
                            
                            var contentNode = htmlDocument.DocumentNode.SelectSingleNode("//*[@class='mw-parser-output']")
                            
                        })
                        );
                }
                
                await Task.WhenAll(listOfTasks);
                
                // Print the response status code
                logger.LogInformation(response.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                // Print the exception message
                logger.LogError(ex, "Error: ");
            }

            // Wait for 10 minutes
            Thread.Sleep(600000);
        }
    }
}