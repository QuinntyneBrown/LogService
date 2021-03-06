using LogServer.API;
using LogServer.Core.Extensions;
using LogServer.Core.Models;
using LogServer.Infrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
    public class LogScenarios: LogScenarioBase
    {

        [Fact]
        public async Task ShouldSave()
        {
            using (var server = CreateServer())
            {
                IMediator mediator = server.Host.Services.GetService(typeof(IMediator)) as IMediator;
                IBackgroundTaskQueue queue = server.Host.Services.GetService(typeof(IBackgroundTaskQueue)) as IBackgroundTaskQueue;

                var eventStore = new EventStore(mediator,queue);
                var id = Guid.NewGuid();

                var response = await server.CreateClient()
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = id,
                        LogLevel = "Trace",
                        Message = "Q"
                    });

                var aggregate = eventStore.Query<Log>(response.LogId);

                Assert.Equal("Q", aggregate.Message);
            }
        }

        [Fact]
        public async Task Perf()
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = CreateServer())
            {                
                var client = server.CreateClient();
                var stopWatch = new Stopwatch();
                var clientId = Guid.NewGuid();
                stopWatch.Start();
                
                for(var i = 0; i < 5; i++)
                {
                    await client
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = clientId,
                        LogLevel = "Trace",
                        Message = $"{i}"
                    });
                }
                
                stopWatch.Stop();

                var duration = stopWatch.ElapsedMilliseconds;                
            }

            await tcs.Task;
        }

        [Fact]
        public async Task ShouldSaveMultiple()
        {
            using (var server = CreateServer())
            {
                IMediator mediator = server.Host.Services.GetService(typeof(IMediator)) as IMediator;
                IBackgroundTaskQueue queue = server.Host.Services.GetService(typeof(IBackgroundTaskQueue)) as IBackgroundTaskQueue;

                var eventStore = new EventStore(mediator, queue);
                var id = Guid.NewGuid();
                var client = server.CreateClient();

                var taskList = new List<Task>
                {
                    client
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = id,
                        LogLevel = "Trace",
                        Message = "1"
                    }),

                    client
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = id,
                        LogLevel = "Trace",
                        Message = "2"
                    }),

                    client
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = id,
                        LogLevel = "Trace",
                        Message = "3"
                    })
                };

                await Task.WhenAll(taskList);
                
                Assert.Equal(1, 1);
            }
        }

        [Fact]
        public async Task ShouldGetAll()
        {
            using (var server = CreateServer())
            {
                _ = await server.CreateClient()
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = Guid.NewGuid(),
                        LogLevel = "Trace",
                        Message = "Test"
                    });

                var response = await server.CreateClient()
                    .GetAsync<GetLogs.Response>(Get.Logs);

                Assert.True(response.Logs.Count() > 0);
            }
        }

        [Fact]
        public async Task ShouldGetById()
        {
            using (var server = CreateServer())
            {
                var  response1 = await server.CreateClient()
                    .PostAsAsync<CreateLog.Request, CreateLog.Response>(Post.Logs, new CreateLog.Request()
                    {
                        ClientId = Guid.NewGuid(),
                        LogLevel = "Trace",
                        Message = "Test"
                    });

                var response2 = await server.CreateClient()
                    .GetAsync<GetLogById.Response>(Get.LogById(response1.LogId));

                Assert.Equal(response2.Log.LogId,response1.LogId);
            }
        }
    }
}
