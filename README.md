# Order book viewer

Use **"Back & Front"** profile to run the project.  
**CryptoExchangeBackend** provides: 
1) REST endpoint with Order book snapshot
2) SignalR hub for real-time updates  

**Frontend** hosts **Frontend.Client** WebAssembly project.

## Installation
### Required
.NET 9
### Optional 
For storing order book snapshots you may configure MongoDb connection string in appsettings.json.  

