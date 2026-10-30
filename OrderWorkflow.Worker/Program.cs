using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;

var builder = Host.CreateApplicationBuilder(args);

const string connectionString =
    "Endpoint=http://localhost:8085;TaskHub=default;Authentication=None";

builder.Services.AddDurableTaskWorker()
    .AddTasks(tasks =>
    {
        tasks.AddOrchestratorFunc<int, string>(
            "ResumeTest",
            async (context, orderId) =>
            {
                var prepared = await context.CallActivityAsync<string>(
                    "PrepareInput", orderId);

                // Durable timer: survives a worker restart.
                await context.CreateTimer(
                    context.CurrentUtcDateTime.AddMinutes(2),
                    CancellationToken.None);

                return await context.CallActivityAsync<string>(
                    "FinishTest", prepared);
            });

        tasks.AddActivityFunc<int, string>(
            "PrepareInput",
            (context, orderId) =>
            {
                Console.WriteLine(
                    $"ACTIVITY: PrepareInput executed for order {orderId}");

                // Test input only. No order data is retrieved yet.
                return Task.FromResult($"Order ID: {orderId}");
            });

        tasks.AddActivityFunc<string, string>(
            "FinishTest",
            (context, prepared) =>
            {
                Console.WriteLine(
                    $"ACTIVITY: FinishTest executed. {prepared}");

                return Task.FromResult(
                    $"Restart-and-resume test completed. {prepared}");
            });
    })
    .UseDurableTaskScheduler(connectionString);

builder.Services.AddDurableTaskClient()
    .UseDurableTaskScheduler(connectionString);

using var host = builder.Build();

await host.StartAsync();

if (builder.Configuration.GetValue<bool>("start"))
{
    var client = host.Services.GetRequiredService<DurableTaskClient>();

    string instanceId =
        await client.ScheduleNewOrchestrationInstanceAsync(
            "ResumeTest",
            input: 1002);

    Console.WriteLine($"Started workflow: {instanceId}");
}
else
{
    Console.WriteLine(
        "Worker started. Processing existing workflows; no new workflow scheduled.");
}

await host.WaitForShutdownAsync();