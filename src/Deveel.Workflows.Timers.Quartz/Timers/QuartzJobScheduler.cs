﻿using Deveel.Workflows.Events;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deveel.Workflows.Timers
{
    public sealed class QuartzJobScheduler : IJobScheduler
    {
        private IScheduler scheduler;

        internal QuartzJobScheduler(IScheduler scheduler)
        {
            this.scheduler = scheduler;
            scheduler.Start().Wait();
        }

        public void Dispose()
        {
            scheduler.Shutdown().Wait();
        }

        public async Task ScheduleAsync(EventId eventId, ScheduleInfo scheduleInfo, IScheduleCallback callback)
        {
            var group = $"{eventId.ProcessId}({eventId.InstanceKey})";
            var jobDetail = JobBuilder.Create<ScheduledJob>()
                .WithIdentity(eventId.EventName, group)
                .Build();

            var triggerBuilder = TriggerBuilder.Create()
                .ForJob(eventId.EventName, group)
                .StartNow();

            if (String.IsNullOrWhiteSpace(scheduleInfo.CronExpression))
            {
                TimeSpan interval;

                if (scheduleInfo.Date != null)
                    interval = scheduleInfo.Date.Value.ToUniversalTime().Subtract(DateTimeOffset.UtcNow);
                else if (scheduleInfo.Duration != null)
                    interval = scheduleInfo.Duration.Value;

                triggerBuilder = triggerBuilder.WithSimpleSchedule(x => x.WithInterval(interval));
            } else
            {
                triggerBuilder = triggerBuilder.WithCronSchedule(scheduleInfo.CronExpression);
            }

            var trigger = triggerBuilder.Build();

            var listenerName = $"listen({eventId.EventName}({group}))";
            scheduler.ListenerManager
                .AddJobListener(new JobListener(listenerName, callback),
                KeyMatcher<JobKey>.KeyEquals(new JobKey(eventId.EventName, group)));

            await scheduler.ScheduleJob(jobDetail, trigger);
        }

        public async Task UnscheduleAsync(EventId eventId)
        {
            var group = $"{eventId.ProcessId}({eventId.InstanceKey})";
            var listenerName = $"listen({eventId.EventName}({group}))";

            scheduler.ListenerManager.RemoveJobListener(listenerName);

            await scheduler.DeleteJob(new JobKey(eventId.EventName, group));
        }

        #region JobListener

        class JobListener : IJobListener
        {
            private IScheduleCallback callback;

            public JobListener(string name, IScheduleCallback callback)
            {
                Name = name;
                this.callback = callback;
            }

            public string Name { get; }

            public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.CompletedTask;
            }

            public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.CompletedTask;
            }

            public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken))
            {
                return callback.NotifyAsync();
            }
        }

        #endregion

        #region ScheduledJob

        class ScheduledJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
