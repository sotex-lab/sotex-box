We serve various metrics that are used to monitor the stack and to evaluate whether the application meets its required performance requirements. We have included a lot of metrics related to the overall application ecosystem. Backend servers metrics related to:
* ASP.NET Core instruments
* .NET Runtime instruments
* Process instruments
* Custom metrics

Most of the metrics explaination can be found on this [blog post](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-aspnetcore#microsoftaspnetcorehosting).

## Other metrics
Alongisde those we have kept tract of some things on our own and those are:

| <div style="width:220px">Metric name</div> | Unit | <div style="width:150px">Tags</div> | Description |
| :-----------| :--- | :- | :--- |
| `sotex_web_devices_num_total` | total | <ul><li>`id`: device id</li><ul> | Represents the number of connected devices to backend. It is also used to see if  the device is currently connected or not |
| `sotex_web_sent_bytes_total` | byte | <ul><li>`id`: device id</li><ul> | Represents the total amout of bytes sent from backend to frontend via SSE during one connection |
