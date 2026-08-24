using System;
using System.Configuration;
using System.ServiceProcess;
using Microsoft.Owin.Hosting;
namespace ToolLending.AppServer { internal static class Program { static void Main(string[] args) { if (Environment.UserInteractive || Array.Exists(args, a => a == "--console")) { using (WebApp.Start<Startup>(ConfigurationManager.AppSettings["BaseAddress"])) { Console.WriteLine("Tool Lending API running; Enter stops it."); Console.ReadLine(); } return; } ServiceBase.Run(new ToolLendingService()); } } internal sealed class ToolLendingService : ServiceBase { IDisposable host; public ToolLendingService() { ServiceName = "ToolLendingAppServer"; AutoLog = true; } protected override void OnStart(string[] a) { host = WebApp.Start<Startup>(ConfigurationManager.AppSettings["BaseAddress"]); } protected override void OnStop() { host?.Dispose(); host = null; } } }
