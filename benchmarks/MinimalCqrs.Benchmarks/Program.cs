using BenchmarkDotNet.Running;

// Run all:        dotnet run -c Release
// Run one:        dotnet run -c Release -- --filter *HttpThroughput*
// Run one:        dotnet run -c Release -- --filter *HandlerDispatch*
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
