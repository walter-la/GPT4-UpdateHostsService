﻿namespace UpdateHostsService
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Quartz.Impl;

    public class HostsUpdaterJob : IJob
    {
        private readonly ILogger<HostsUpdaterJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly List<string> _domainWhitelist;

        public HostsUpdaterJob(ILogger<HostsUpdaterJob> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _domainWhitelist = _configuration.GetSection("DomainWhitelist").Get<List<string>>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            HostsSection section = (HostsSection)dataMap["section"];

            try
            {
                var hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
                var hostsContent = ReadHostsFileWithRetry(hostsPath);

                var sectionContent = await GetSectionContent(section.Url);

                if (string.IsNullOrEmpty(sectionContent))
                {
                    _logger.LogError("Invalid URL or file path: {Url}", section.Url);
                    return;
                }

                hostsContent = ReplaceOrUpdateSection(hostsContent, section.Name, sectionContent);
                WriteHostsFileWithRetry(hostsPath, hostsContent);
                _logger.LogInformation("Hosts file updated for section {sectionName}.", section.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hosts file for section {sectionName}.", section.Name);
            }
        }

        private string ReadHostsFileWithRetry(string hostsPath, int maxRetries = 3, int retryDelay = 500)
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    return File.ReadAllText(hostsPath);
                }
                catch (IOException)
                {
                    if (retry == maxRetries - 1)
                    {
                        throw;
                    }

                    Task.Delay(retryDelay).Wait();
                }
            }

            return string.Empty;
        }

        private void WriteHostsFileWithRetry(string hostsPath, string content, int maxRetries = 3, int retryDelay = 500)
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    File.WriteAllText(hostsPath, content);
                    return;
                }
                catch (IOException)
                {
                    if (retry == maxRetries - 1)
                    {
                        throw;
                    }

                    Task.Delay(retryDelay).Wait();
                }
            }
        }

        private async Task<string> GetSectionContent(string url)
        {
            var content = string.Empty;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                content = await _httpClient.GetStringAsync(url);
            }
            else if (File.Exists(url))
            {
                content = File.ReadAllText(url);
            }
            else
            {
                _logger.LogError("Invalid URL or file path: {Url}", url);
                return content;
            }

            var filteredContent = new StringBuilder();

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!_domainWhitelist.Any(domain => line.Contains(domain)))
                    {
                        filteredContent.AppendLine(line);
                    }
                }
            }

            return filteredContent.ToString();
        }

        private string ReplaceOrUpdateSection(string hostsContent, string sectionName, string sectionContent)
        {
            var sectionHeader = $"### begin {sectionName}";
            var sectionFooter = $"### end {sectionName}";

            var sectionRegex = new Regex($@"{sectionHeader}.*?{sectionFooter}", RegexOptions.Singleline);

            if (sectionRegex.IsMatch(hostsContent))
            {
                hostsContent = sectionRegex.Replace(hostsContent, $"{sectionHeader}\n{sectionContent}\n{sectionFooter}");
            }
            else
            {
                hostsContent += $"\n{sectionHeader}\n{sectionContent}\n{sectionFooter}";
            }

            return hostsContent;
        }
    }

}
