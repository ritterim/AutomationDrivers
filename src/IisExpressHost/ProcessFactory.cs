﻿// Copyright (c) 2014 Insurance Marketing Technology. All rights reserved.
// </copyright>
// <author>Joshua Wiens</author>
// <author>Justin Rusbatch</author>
// <author>Ken Dale</author>
// <date>3/20/2014</date>
// <summary>Implements the iis express host class</summary>

using System;
using System.Configuration;
using System.IO;
using System.Net.NetworkInformation;
using System.Linq;


namespace Ritter.AutomationDrivers.IisExpressHost
{
    public static class ProcessFactory
    {
        public static int IisPort;

        public static IisExpress CreateDefaultInstance(string solutionRootDir)
        {
            var targetProjectFolderName = ConfigurationManager.AppSettings["TargetProjectFolderName"];
            var applicationPath = GetApplicationPath(targetProjectFolderName, solutionRootDir);

            var portNumber = GetAvailablePort();

            IisPort = portNumber;

            return new IisExpress(applicationPath, portNumber);
        }

        public static string GetApplicationPath(string targetProjectFolderName, string solutionRootDir)
        {
            var logicalPath = String.Format("src\\{0}", targetProjectFolderName);
            var applicationPath = Path.Combine(solutionRootDir, logicalPath);
            return applicationPath;
        }

        public static string BaseUrl
        {
            get { return string.Format("http://localhost:{0}", IisPort); }
        }

        public static int GetAvailablePort()
        {
            const int portStartIndex = 49152;
            const int portEndIndex = 65535;

            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();

            var usedPorts = tcpEndPoints.Select(p => p.Port).ToList();
            var unusedPort = 0;

            for (var port = portStartIndex; port < portEndIndex; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    unusedPort = port;
                    break;
                }
            }
            return unusedPort;
        }
    }
}