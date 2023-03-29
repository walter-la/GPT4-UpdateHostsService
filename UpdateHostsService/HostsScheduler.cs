using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Quartz.Logging.OperationName;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;
using System.Text.RegularExpressions;

namespace UpdateHostsService
{
    public class HostsScheduler
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HostsScheduler(IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task ScheduleJobs()
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            scheduler.JobFactory = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IJobFactory>();
            await scheduler.Start();

            var sections = _configuration.GetSection("Sections").Get<List<HostsSection>>();

            foreach (var section in sections)
            {
                if (Uri.IsWellFormedUriString(section.Url, UriKind.Absolute) || (File.Exists(section.Url) && section.IntervalInSeconds > 0))
                {
                    var job = JobBuilder.Create<HostsUpdaterJob>()
                        .WithIdentity($"UpdateHostsJob-{section.Name}")
                        .UsingJobData(new JobDataMap { { "section", section } })
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"UpdateHostsTrigger-{section.Name}")
                        .StartAt(DateTime.UtcNow)
                        .WithSimpleSchedule(x => x
                            .WithIntervalInSeconds(section.IntervalInSeconds)
                            .RepeatForever())
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);
                }
                else if (File.Exists(section.Url) && section.IntervalInSeconds <= 0)
                {
                    var fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(section.Url), Path.GetFileName(section.Url))
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                    };

                    var jobKey = new JobKey($"UpdateHostsJob-{section.Name}");

                    // 檢查工作是否已經存在
                    if (!await scheduler.CheckExists(jobKey))
                    {
                        // 如果工作不存在，創建並添加到調度器中
                        var job = JobBuilder.Create<HostsUpdaterJob>()
                            .WithIdentity(jobKey)
                            .UsingJobData(new JobDataMap { { "section", section } })
                            .StoreDurably() // 使工作持久化，即使沒有觸發器也能保存
                            .Build();

                        await scheduler.AddJob(job, true); // true 參數表示如果存在相同的 JobKey，則替換現有的工作
                    }

                    // 觸發工作
                    await scheduler.TriggerJob(jobKey);

                    fileSystemWatcher.Changed += async (sender, e) =>
                    {
                        // 觸發工作
                        await scheduler.TriggerJob(jobKey);
                    };

                    fileSystemWatcher.EnableRaisingEvents = true;
                }
            }
        }

        public void RemoveUnusedSectionsFromHostsFile()
        {
            var hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
            var hostsContent = File.ReadAllText(hostsPath);
            var sections = _configuration.GetSection("Sections").Get<List<HostsSection>>();

            var sectionRegex = new Regex(@"### begin (?<sectionName>.*?)\r?\n.*?\r?\n### end \1", RegexOptions.Singleline);
            var matches = sectionRegex.Matches(hostsContent);

            var updated = false;

            foreach (Match match in matches)
            {
                var sectionName = match.Groups["sectionName"].Value;
                if (!sections.Any(s => s.Name == sectionName))
                {
                    hostsContent = hostsContent.Replace(match.Value, "");
                    updated = true;
                }
            }

            if (updated)
            {
                File.WriteAllText(hostsPath, hostsContent);
            }
        }



    }

}
