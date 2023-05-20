using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

class Program
{
    public class TextModel
    {
        public Guid Id { get; set; }
        
        public string Url { get; set; }
        
        public string DraftFilePath { get; set; }
        
        public string ArticlePath { get; set; }
        
        public string ShortDesciption { get; set; }
        
        public DateTime IndexedDate { get; set; }
    }
    
    static async Task Main(string[] args)
    {
        // Use ConfigurationBuilder to read the appsettings.json file
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration config = builder.Build();

        // Get the URL to send the request to
        string url = config.GetValue<string>("Url");
        
        Console.WriteLine($"Url: {url}");

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
                //Console.WriteLine(response);

                var listOfTasks = new List<Task<TextModel>>();

                string pathForSaving = config.GetValue<string>("PathForSavingFiles");
                
                var connectionStringToDatabase = "mongodb://mongo_dc:27017/";
                var mongoClient = new MongoClient(connectionStringToDatabase);

                var database = mongoClient.GetDatabase("articles_new");

                var collections = await database.ListCollectionNames().ToListAsync();

                Console.WriteLine($"Collections name: {string.Join(", ", collections)}");
                
                if (collections.All(x => x != "articleinfo"))
                {
                    Console.WriteLine("Need create collection");
                    database.CreateCollection("articleinfo");
                    Console.WriteLine("collection was created.");
                }
                
                var collection = database.GetCollection<TextModel>("articleinfo");
                
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

                            var contentNode =
                                htmlDocument.DocumentNode.SelectSingleNode("//*[@class='mw-parser-output']");

                            if (contentNode == null || !contentNode.ChildNodes.Any())
                            {
                                return new TextModel()
                                {
                                    ArticlePath = null,
                                    DraftFilePath = null,
                                    Id = Guid.NewGuid(),
                                    IndexedDate = DateTime.Now,
                                    ShortDesciption = string.Empty,
                                    Url = url_
                                };
                            }

                            string shortDescription = string.Empty;

                            var fullText = new StringBuilder();

                            var articleName = url_.Split("/", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                            var filenameClean = $"{articleName}_clean.txt";
                            var filenameDraft = $"{articleName}_draft.txt";

                            foreach (var child in contentNode.ChildNodes)
                            {
                                if (child.HasClass("shortdescription"))
                                {
                                    shortDescription = child.InnerText;
                                }

                                if (child.Name == "p")
                                {
                                    fullText.Append(child.InnerText);
                                }

                                if (child.Name == "ul")
                                {
                                    foreach (var listElement in child.ChildNodes)
                                    {
                                        fullText.Append($" - {listElement.InnerText}");
                                    }
                                }

                                if (child.Name == "h2")
                                {
                                    if (child.Id == "References" || child.ChildNodes.Any(x => x.Id == "References"))
                                    {
                                        break;
                                    }
                                    fullText.Append('\n');
                                    fullText.Append(child.InnerText);
                                    fullText.Append('\n');
                                }
                                
                                Console.WriteLine(child.InnerText);
                            }

                            var fileGuid = Guid.NewGuid().ToString();
                            var fullPath = Path.Combine(pathForSaving, fileGuid[0].ToString(), fileGuid[1].ToString());
                            if (!Directory.Exists(fullPath))
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            
                            await File.WriteAllTextAsync(Path.Combine(fullPath, filenameClean), fullText.ToString());
                            await File.WriteAllTextAsync(Path.Combine(fullPath, filenameDraft), str);

                            var model = new TextModel()
                            {
                                Id = Guid.NewGuid(),
                                Url = url_,
                                ShortDesciption = shortDescription,
                                ArticlePath = Path.Combine(fullPath, filenameClean),
                                DraftFilePath = Path.Combine(fullPath, filenameDraft),
                                IndexedDate = DateTime.Now
                            };

                            return model;
                        })
                        );
                }
                
                var completed = await Task.WhenAll(listOfTasks);

                await collection.InsertManyAsync(completed);
                
                Console.WriteLine("data is successed saving.");
                
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