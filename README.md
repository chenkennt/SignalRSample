# Getting Started with Azure SignalR Service

[SignalR](https://www.asp.net/signalr) is an open source library for ASP.NET developers that simplifies the process of adding real-time web functionality to applications. With SignalR, you can easily create real-time web applications like web chat room, web games, etc.

One technical difficulty when using SignalR is SignalR runtime is running inside your ASP.NET application, so you'll need to handle the complexity of managing SignalR connections. For example, if your application has many concurrent connections, you may need to scale your application using [backplane](https://docs.microsoft.com/en-us/aspnet/signalr/overview/performance/scaleout-in-signalr) and network load balancers, even though your application code doesn't really need to scale.

This is where Azure SignalR Service can help. With Azure SignalR Service, you can leave SignalR and connection management logic to the service and focus on your own business logic.

In this article, you'll learn how to use Azure SignalR Service to create a real-time web application.

> Azure SignalR Service is built on [SignalR for ASP.NET Core](https://github.com/aspnet/SignalR) (which has some differences from ASP.NET SignalR). So you'll need to switch to SignalR Core in order to use this service.

## Build a Web Chat Room using SignalR
First let's create a web chat room using SignalR Core. To use SignalR Core, you need to first download and install [.NET Core SDK](https://www.microsoft.com/net/learn/get-started).

Build and run the chat room same at [ChatDemo](ChatDemo) folder:
1. Checkout local branch:
   ```
   git checkout local
   ```
2. Build chat room app:
   ```
   cd ChatDemo
   dotnet build
   ```
3. Run chat room app:
   ```
   dotnet run
   ```

Now you can open http://localhost:5050 in browser.

First time you open the chat room you'll be asked for you name:

![chat1](resources/chat1.png)

Then type something in the text box and press enter, your message will be sent to everyone in the room:

![chat2](resources/chat2.png)

Open multiple browser windows, then each one can talk to each other.

## A Brief Explanation

Here is a brief explanation of how this chat room application works.

One core concept in SignalR is Hub. Hub is a programming model that allows server and client talk to each other by calling methods. Here a hub defined in [Chat.cs](ChatDemo/Chat.cs):
```csharp
public class Chat : Hub
{
    public void broadcastMessage(string name, string message)
    {
        Clients.All.InvokeAsync("broadcastMessage", name, message);
    }
}
```

By defining this hub, server exposes a `broadcastMessage` method that can directly called by client (in [index.html](ChatDemo/wwwroot/index.html), when user clicks send button):
```js
document.getElementById('sendmessage').addEventListener('click', function (event) {
  // Call the broadcastMessage method on the hub.
  connection.invoke('broadcastMessage', name, messageInput.value);
  ...
});
```

Also inside a hub, server can call methods that are defined in client directly. In this sample, it calls a `broadcastMessage` method that displays the message to the chat window:
```js
var messageCallback = function (name, message) {
  // Html encode display name and message.
  var encodedName = name;
  var encodedMsg = message;
  // Add the message to the page.
  var liElement = document.createElement('li');
  liElement.innerHTML = '<strong>' + encodedName + '</strong>:&nbsp;&nbsp;' + encodedMsg;
  var messageBox = document.getElementById('messages');
  messageBox.appendChild(liElement);
  messageBox.scrollTop = messageBox.scrollHeight;
};
connection.on('broadcastMessage', messageCallback);
```

After you have the server and client logic, there is a few lines of code needed to start the SignalR server and connect it with client.

At server side, you need to call `AddSignalR` to initialize your SignalR server and map your hub to a server url (/chat):
```cs
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSignalR();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...
    app.UseSignalR(routes =>
    {
        routes.MapHub<Chat>("chat");
    });
}
```

At client side, create a HubConnection that connects to the chat endpoint:
```js
var connection = new signalR.HubConnection(url, { transport: transport, uid: name });
...
return connection.start();
```

> This sample is based on the chat sample in SignalR official documentation, see this [article](https://docs.microsoft.com/en-us/aspnet/signalr/overview/getting-started/tutorial-getting-started-with-signalr-and-mvc) for more information of this sample.

## Use Azure SignalR Service in Web Chat Room
The sample above is a traditional SignalR application, where SignalR runtime and business logic runs inside the same ASP.NET web application. Now let's use Azure SignalR Service to replace the SignalR runtime in your application.

### Create an Azure SignalR Service

First let's create a SignalR service on Azure.

1. Open Azure portal, click "Create a resource" and find "SignalR Service (preview)".

   ![signalr1](resources/signalr1.png)

2. Click "Create", and then fill necessary information, including resource name, resource group and location.

   ![signalr2](resources/signalr2.png)

   Resource name will also be used as the DNS name of your service point. So you'll get a `<resource_name>.<location>.cloudapp.azure.com` that you can connect to.

3. Click "Create", your SignalR service will be created in a few minutes.

   ![signalr3](resources/signalr3.png)

After your service is ready, let's go to the resource and see which properties it has.
![signalr4](resources/signalr4.png)

1. DNS and public IP, this is the public address of the service. Azure SignalR Service uses 5001 port, you can use `<DNS>:5001` (also shown as host name) to connect to the service.

2. Primary key and secondary key. This is the key you can use to authenticate with the service. When using SignalR service, your application server authenticates with service using this access keys. Clients will authenticate with service using a different way (JWT, will be discussed later in this article).

### Update Web Chat Room to use Azure SignalR Service

Now let's update the chat room sample to use Azure SignalR Service.

The code is already at master branch, checkout this branch (`git checkout master`) and let's what're the differences.
