# Getting Started with Azure SignalR Service

[SignalR](https://www.asp.net/signalr) is an open source library for ASP.NET developers that simplifies the process of adding real-time web functionality to applications. With SignalR, you can easily create real-time web applications like web chat room, web games, etc.

One technical difficulty when using SignalR is SignalR runtime is running inside your ASP.NET application, so you'll need to handle the complexity of managing SignalR connections. For example, if your application has many concurrent connections, you may need to scale your application using [backplane](https://docs.microsoft.com/en-us/aspnet/signalr/overview/performance/scaleout-in-signalr) and network load balancers, even though your application code doesn't really need to scale.

This is where Azure SignalR Service can help. With Azure SignalR Service, you can leave SignalR and connection management logic to the service and focus on your own business logic.

In this article, you'll learn how to use Azure SignalR Service to create a real-time web application.

> Azure SignalR Service is built on [SignalR for ASP.NET Core](https://github.com/aspnet/SignalR) (which has some differences from ASP.NET SignalR). So you'll need to switch to SignalR Core in order to use this service.

## Build a web chat room using SignalR
First let's create a web chat room using SignalR Core. To use SignalR Core, you need to first download and install [.NET Core SDK](https://www.microsoft.com/net/learn/get-started).

Build and run the chat room same at [ChatDemo](ChatDemo) folder:
1. Clone [repo]().
2. Checkout to local branch:
   ```
   git checkout local
   ```
3. Build chat room app:
   ```
   cd ChatDemo
   dotnet build
   ```
4. Run chat room app:
   ```
   dotnet run
   ```



> The sample in this article is based on 