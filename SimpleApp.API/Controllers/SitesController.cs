using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SimpleApp.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SitesController : ControllerBase
    {
        // GET
        [HttpGet(Name = "GetUrlsFromWikipedia")]
        public async Task<string[]> Get()
        {
            var sitesTask = new List<Task<string>>();

            for (int i = 0; i < 100; i++)
            {
                sitesTask.Add(GetWikipediaUrl());
            }

            await Task.WhenAll(sitesTask);
            return sitesTask.Select(x => x.Result).ToArray();
        }

        private async Task<string> GetWikipediaUrl()
        {
            using (var client = new HttpClient())
            {
                var t = await client.GetAsync("https://en.wikipedia.org/wiki/Special:Random");
            
                var g = await t.Content.ReadAsStringAsync();

                return t.RequestMessage?.RequestUri?.ToString();
            }
        }
    }
}