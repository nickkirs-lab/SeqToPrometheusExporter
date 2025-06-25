using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("AssemblyName", "Seq.LogProducer")
    .CreateLogger();

Log.Information("Application started");
Log.ForContext("EventType", "0x37AA1435")
   .ForContext("Level", "Error")
   .ForContext("StatusCode", 500)
   .Error("Internal server error occurred");

Log.Information("Application run");
Log.ForContext("EventType", "0x37AA1435")
    .ForContext("Level", "Info")
    .ForContext("StatusCode", 200);

Log.ForContext("EventType", "0x37AA1435")
   .ForContext("Level", "Error")
   .ForContext("EndPointName", "Hot GraphQL Pipeline 1")
   .ForContext("Host", "api.test.local")
   .Error("GraphQL error");


Log.ForContext("EventType", "0x37AA1435")
    .ForContext("Level", "Error")
    .ForContext("EndPointName", "Hot GraphQL Pipeline 2")
    .ForContext("Host", "api.test.local")
    .Error("GraphQL error");

Log.CloseAndFlush();