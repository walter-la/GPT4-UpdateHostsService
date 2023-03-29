
namespace UpdateHostsService
{
    using Microsoft.Extensions.DependencyInjection;
    using Quartz;
    using Quartz.Spi;

    public class ServiceProviderJobFactory : IJobFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceProviderJobFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job) { }
    }

}
