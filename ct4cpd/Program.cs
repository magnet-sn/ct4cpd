using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System;
using System.Threading;

namespace ct4cpd;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            global::Shmuelie.WinRTServer.ComServer server = new();
            ManualResetEvent extensionDisposedEvent = new(false);
            
            // マニフェストが求めている正しい名前「ct4cpd」を登録
            global::ct4cpd.ct4cpd extensionInstance = new(extensionDisposedEvent);
            server.RegisterClass<global::ct4cpd.ct4cpd, IExtension>(() => extensionInstance);
            server.Start();
            
            extensionDisposedEvent.WaitOne();
            server.Stop();
            server.UnsafeDispose();
        }
        else
        {
            Console.WriteLine("Not being launched as a Extension... exiting.");
        }
    }
}