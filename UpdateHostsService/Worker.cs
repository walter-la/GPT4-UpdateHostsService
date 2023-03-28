using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UpdateHostsService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                try
                {
                    var hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
                    var hostsContent = File.ReadAllText(hostsPath);

                    var sections = _configuration.GetSection("Sections").Get<List<HostsSection>>();

                    foreach (var section in sections)
                    {
                        var sectionContent = string.Empty;

                        if (Uri.IsWellFormedUriString(section.Url, UriKind.Absolute))
                        {
                            sectionContent = await _httpClient.GetStringAsync(section.Url);
                        }
                        else if (File.Exists(section.Url))
                        {
                            sectionContent = File.ReadAllText(section.Url);
                        }
                        else
                        {
                            _logger.LogError("Invalid URL or file path: {Url}", section.Url);
                            continue;
                        }

                        var sectionHeader = $"### begin {section.Name}";
                        var sectionFooter = $"### end {section.Name}";

                        var sectionRegex = new Regex($@"{sectionHeader}.*?{sectionFooter}", RegexOptions.Singleline);

                        if (sectionRegex.IsMatch(hostsContent))
                        {
                            hostsContent = sectionRegex.Replace(hostsContent, $"{sectionHeader}\n{sectionContent}\n{sectionFooter}");
                        }
                        else
                        {
                            hostsContent += $"\n{sectionHeader}\n{sectionContent}\n{sectionFooter}";
                        }
                    }

                    File.WriteAllText(hostsPath, hostsContent);
                    _logger.LogInformation("Hosts file updated.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating hosts file.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Adjust the delay as needed.
            }
        }
    }

    public class HostsSection
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}



