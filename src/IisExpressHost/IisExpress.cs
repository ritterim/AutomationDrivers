// Copyright (c) 2014 Insurance Marketing Technology. All rights reserved.
// </copyright>
// <author>Joshua Wiens</author>
// <date>3/20/2014</date>
// <summary>Implements the IISExpressHost class</summary>

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutomationDrivers.Core.Exceptions;

namespace AutomationDrivers.IisExpressHost
{
    public class IisExpress : IDisposable
    {
        private const string IisExpressPath = @"C:\Program Files\IIS Express\iisexpress.exe";

        private const string ReadyMsg = @"IIS Express is running.";

        private readonly ProcessStartInfo _startInfo;

        private Process _process;


        /// <summary>
        /// Initializes a new instance of the IISExpressHost class.
        /// </summary>
        ///
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the requested directory is not present.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one or more arguments are outside the required range.
        /// </exception>
        ///
        /// <param name="path"> The full path to the targeted web project </param>
        /// <param name="port"> The port assigned by ProcessFactory.GetAvailablePort() </param>
        public IisExpress(string path, int port)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException();
            }

            if (ushort.MinValue > port || ushort.MaxValue < port)
            {
                throw new ArgumentOutOfRangeException("port");
            }

            Path = path;
            Port = port;

            _startInfo = new ProcessStartInfo
            {
                FileName = IisExpressPath,
                Arguments = string.Format("/path:{0} /port:{1}", path, port),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }


        public string Path { get; private set; }

        public static int Port { get; private set; }

        public int ProcessId { get; private set; }


        /// <summary>   Starts the given cancellation token. </summary>
        ///
        /// <param name="cancellationToken">    The cancellation token. </param>
        ///
        /// <returns>   A Task. </returns>
        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            try
            {
                var proc = new Process { EnableRaisingEvents = true, StartInfo = _startInfo };

                DataReceivedEventHandler onOutput = null;
                onOutput =
                    (sender, e) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled();
                        }

                        try
                        {
                            Debug.WriteLine("  [StdOut]\t{0}", (object)e.Data);

                            if (string.Equals(ReadyMsg, e.Data, StringComparison.OrdinalIgnoreCase))
                            {
                                proc.OutputDataReceived -= onOutput;
                                _process = proc;
                                tcs.TrySetResult(null);
                            }
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                            proc.Dispose();
                        }
                    };
                proc.OutputDataReceived += onOutput;
                proc.ErrorDataReceived += (sender, e) => Debug.WriteLine("  [StdOut]\t{0}", (object)e.Data);
                proc.Exited += (sender, e) => Debug.WriteLine("  IIS Express exited.");

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                ProcessId = proc.Id;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }


        /// <summary>   Gets the stop. </summary>
        ///
        /// <returns>   A Task. </returns>
        public Task Stop()
        {
            var tcs = new TaskCompletionSource<object>(null);
            try
            {
                _process.Exited += (sender, e) => tcs.TrySetResult(null);

                SendStopMessageToProcess(ProcessId);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }


        /// <summary> Kills the IISExpress application based on the assigned pId </summary>
        public void Quit()
        {
            Process proc;
            if ((proc = Interlocked.Exchange(ref _process, null)) != null)
            {
                proc.Kill();
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases the unmanaged resources used by the Web.UI.Tests.IISExpress.IISExpressHost and
        /// optionally releases the managed resources.
        /// </summary>
        ///
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources; false to release only unmanaged
        /// resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Quit();
            }
        }


        /// <summary>   Sends a stop message to process. </summary>
        ///
        /// <param name="pid">  The process ID assigned to the current IISEcpress instance. </param>
        private static void SendStopMessageToProcess(int pid)
        {
            try
            {
                for (var ptr = NativeMethods.GetTopWindow(IntPtr.Zero); ptr != IntPtr.Zero; ptr = NativeMethods.GetWindow(ptr, 2))
                {
                    uint num;
                    NativeMethods.GetWindowThreadProcessId(ptr, out num);
                    if (pid == num)
                    {
                        var hWnd = new HandleRef(null, ptr);
                        NativeMethods.PostMessage(hWnd, 0x12, IntPtr.Zero, IntPtr.Zero);
                        return;
                    }
                }
            }
            catch (ArgumentException)
            {
            }
        }
    }
}
