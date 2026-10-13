"use strict";
var connection = new signalR.HubConnectionBuilder()
    .withUrl("../NotificationUserHub?userId=" + localStorage["AxpertConnectUser"])
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();

connection.on("NotifyUser", (content) => {
    //alert("Result from Push notification:" + content);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
}).then(function () {
    connection.invoke('GetConnectionId').then(function (connectionId) {
        document.getElementById('signalRConnectionId').innerHTML = connectionId;
    })
});