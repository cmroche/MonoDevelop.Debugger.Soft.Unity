using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger.Soft.Unity
{
	class iOSUsbConnector: IUnityDbgConnector
	{
		public const ushort LocalPort = 12001;
		public const ushort DevicePort = 56000;

		public SoftDebuggerStartInfo SetupConnection()
		{
			iProxy.Start(LocalPort, DevicePort);

			var args = new SoftDebuggerConnectArgs("Any iOS Device", IPAddress.Loopback, LocalPort);
			return new SoftDebuggerStartInfo(args);
		}

		public void OnDisconnect()
		{
			iProxy.Stop();
		}
	}


	static class iProxy
	{
		static Process process;


		public static void Start(ushort localPort, ushort devicePort)
		{
			if ((process != null) && !process.HasExited)
				return;

			string iproxy = Path.Combine(Util.UnityEditorDataFolder, "PlaybackEngines", "iOSSupport", "Tools", "OSX", "unityiproxy");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = iproxy;
			startInfo.Arguments = localPort + " " + devicePort;
			startInfo.UseShellExecute = false;
			process = Process.Start(startInfo);

			// No better way to check if iproxy has started and set everything up :(
			Thread.Sleep(1000);
		}


		public static void Stop()
		{
			if (process == null)
				return;

			// Try to close nicely...
			process.CloseMainWindow();
			if (!process.WaitForExit(1000))
			{
				// ... and kill if it's not cooperating
				process.Kill();
				process.WaitForExit(3000);
			}

			process.Dispose();
			process = null;
		}
	}


	static class iOSDevices
	{
		public static void GetUSBDevices(ConnectorRegistry connectors, List<ProcessInfo> processes)
		{
			if (Platform.IsMac)
			{
				var processId = connectors.GetProcessIdForUniqueId("Any iOS Device");
				processes.Add(new ProcessInfo(processId, "Unity USB: any iOS device"));
				connectors.Connectors[processId] = new iOSUsbConnector();
			}
		}
	}
}
