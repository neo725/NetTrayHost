Console.Title = "NetTrayHost Mock CLI";
Console.WriteLine($"[MockCli] Started. PID={Environment.ProcessId}");
Console.WriteLine("[MockCli] Press Ctrl+C to stop.");

var tick = 0;
while (true)
{
    Console.WriteLine($"[MockCli] running... tick={tick++}, time={DateTime.Now:HH:mm:ss}");
    Thread.Sleep(1000);
}
