# Order book viewer

## Prerequisites
- .NET 9 SDK
- MongoDB (Optional)  
For storing order book snapshots you may provide connection string in appsettings.json.

## Steps to run
### Visual studio
Use **"Back & Front"** profile.

### CLI  
1. In the main directory open two separate command prompts:
```cmd
cd OrderBookMonitorBackend && dotnet run
```
```cmd
cd ./Frontend/Frontend.Host && dotnet run
```

Alternatively in Powershell:

```powershell
Start-Process "dotnet" "run --project OrderBookMonitorBackend"; Start-Process "dotnet" "run --project Frontend/Frontend.Host"
```
2. Follow the url provided by Frontend.Host 

