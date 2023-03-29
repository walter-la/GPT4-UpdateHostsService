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
            scheduler.JobFactory = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IJobFactory>(); // Set the custom JobFactory
            await scheduler.Start();

            var sections = _configuration.GetSection("Sections").Get<List<HostsSection>>();

            foreach (var section in sections)
            {
                var job = JobBuilder.Create<HostsUpdaterJob>()
                    .WithIdentity($"UpdateHostsJob-{section.Name}")
                    .UsingJobData(new JobDataMap { { "section", section } })
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"UpdateHostsTrigger-{section.Name}")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(section.IntervalInSeconds)
                        .RepeatForever())
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
                var jobDetail = scheduler.GetJobDetail(job.Key);
                var triggers = await scheduler.GetTriggersOfJob(job.Key);
                var nextFireTimeUtc = triggers.FirstOrDefault()?.GetNextFireTimeUtc();
            }
        }
    }

}
