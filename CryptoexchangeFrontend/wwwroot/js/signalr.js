console.log("orderBook signalR js is loaded");

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${window.backendSettings.backendUrl}/orderBookHub`) // Match the hub URL
    .build();

connection.start().then(() => {
    console.log("SignalR connected.");
}).catch(err => console.error(err.toString()));

// Example of listening to server messages
connection.on("ReceiveMessage", (user, message) => {
    console.log(`${user}: ${message}`);
});

// Example of sending messages to the server
function sendMessageTestSignalR(user, message) {
    console.log("sendMessageTestSignalR is called");
    connection.invoke("SendMessage", user, message)
        .catch(err => console.error(err.toString()));
}