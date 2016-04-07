//#define INCLUDE_TemperatureSensorMCP0908_InDemo
/*
   Copyright (C) 2015 MadeInTheUSB LLC

   The MIT License (MIT)

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
 
    Written by FT for MadeInTheUSB
    MIT license, all text above must be included in any redistribution
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client; 
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFS_BUILD_LIGHTS
{
    /// <summary>
    /// https://www.visualstudio.com/integrate/get-started/client-libraries/dotnet
    ///   PM> Install-Package Microsoft.TeamFoundationServer.ExtendedClient
    /// https://www.nuget.org/packages/Microsoft.TeamFoundationServer.ExtendedClient/
    /// https://msdn.microsoft.com/en-us/library/bb130146.aspx
    /// </summary>
    class Demo
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Demo).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 4, "A)PI demo  Custom cH)ar demo  Nusbio R)ocks  P)erformance Test  Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/10557814/how-to-get-a-specific-build-with-the-tfs-api
        /// </summary>
        /// <param name="TeamProject"></param>
        /// <param name="BuildDefinition"></param>
        /// <param name="BuildID"></param>
        /// <returns></returns>
        public string GetBuildStatus(TfsTeamProjectCollection tfs,string TeamProject, string BuildDefinition, int BuildID)
        {
            IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));
            string status = string.Empty;
            IQueuedBuildSpec qbSpec = buildServer.CreateBuildQueueSpec(TeamProject, BuildDefinition);
            IQueuedBuildQueryResult qbResults = buildServer.QueryQueuedBuilds(qbSpec);
            if(qbResults.QueuedBuilds.Length > 0)
            {
                IQueuedBuild build = qbResults.QueuedBuilds.Where(x => x.Id == BuildID).FirstOrDefault();
                status = build.Status.ToString();
            }
            return status;
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            // How do I get the Visual Studio Online credentials cached in the registry? http://stackoverflow.com/questions/20809207/how-do-i-get-the-visual-studio-online-credentials-cached-in-the-registry
            //https://gist.github.com/ctaggart/8164083

            //var netCred = new NetworkCredential("fredericaltorres@live.com", "Resforever123!");
            //BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            //TfsClientCredentials credential = new TfsClientCredentials(basicCred);
            //credential.AllowInteractive = false;
            //string TFSServerPath = "https://fredericaltorres.visualstudio.com/defaultcollection";

            //using (var tfs = new TfsTeamProjectCollection(new Uri(TFSServerPath), credential))
            //{
            //    tfs.Authenticate();
            //    tfs.EnsureAuthenticated();
            //}

            // https://fredericaltorres.visualstudio.com/DefaultCollection/P1/_dashboards
            
            var collectionUri = new Uri("https://fredericaltorres.visualstudio.com:443/defaultcollection");
            var credentialProvider = new UICredentialsProvider();
            var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(collectionUri, new UICredentialsProvider());
            tfs.EnsureAuthenticated();
            

            var versionControl  = tfs.GetService<VersionControlServer>();
            var p1TeamProject   = versionControl.GetTeamProject("P1");
            var bs              = tfs.GetService<IBuildServer>();

            Console.WriteLine("Build ServerVersion:{0}",bs.BuildServerVersion);
            var allBuildDetails = bs.GetBuildQualities("P1");
            var buildController = bs.GetBuildController("");
            Console.WriteLine("Build Controller:{0}",buildController.Name);

            var projectName = "P1";
            var projectPath = "$/P1";
            var buildDefinitionName = "MainBuildDef";

            var builds0 = bs.QueryBuilds("P1", buildDefinitionName);
            
            var teamProjects = versionControl.GetAllTeamProjects(true);
            var p1Project = teamProjects[0];

            foreach (TeamProject proj in teamProjects)
            {
                Console.WriteLine("Query build for project {0}", proj.Name);
                var builds = bs.QueryBuilds(proj.Name);
                foreach (IBuildDetail build in builds)
                {
                    var result = string.Format("Build {0}/{3} {4} - current status {1} - as of {2}", build.BuildDefinition.Name, build.Status.ToString(), build.FinishTime, build.LabelName, Environment.NewLine);
                    System.Console.WriteLine(result);
                }            
            }

            var buildDefinition = bs.GetBuildDefinition(projectName, buildDefinitionName);
            var buildRequest    = buildDefinition.CreateBuildRequest();
            var queuedBuild     = bs.QueueBuild(buildRequest);
            var queuedBuildId   = queuedBuild.Id;

            
            using (var nusbio = new Nusbio(serialNumber))
            {
                Console.WriteLine("LCD i2c Initialization");
                var timeOut = new TimeOut(1000);
                Cls(nusbio);
                ConsoleEx.WriteLine(0, 10, string.Format("TFS Instance ID {0}, DisplayName:{1}, User:{2}", 
                    tfs.InstanceId, 
                    tfs.DisplayName,
                    versionControl.AuthenticatedUser
                    ), ConsoleColor.Cyan);

                nusbio[NusbioGpio.Gpio2].AsLed.SetBlinkMode(1000, 1);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q)
                        {
                            nusbio.ExitLoop();
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}

