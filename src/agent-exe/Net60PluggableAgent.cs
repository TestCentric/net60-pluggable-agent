// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using NUnit.Engine;
using TestCentric.Engine.Agents;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Communication.Transports.Tcp;
using TestCentric.Engine.Runners;
using System.Xml;

namespace TestCentric.Agents
{
    public class Net60PluggableAgent
    {
        static Process AgencyProcess;
        static RemoteTestAgent Agent;
        private static Logger log;
        static int _pid = Process.GetCurrentProcess().Id;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            var options = new AgentOptions(args);
            var logName = $"testcentric-agent_{_pid}.log";

            InternalTrace.Initialize(Path.Combine(options.WorkDirectory, logName), options.TraceLevel);
            log = InternalTrace.GetLogger(typeof(Net60PluggableAgent));
            log.Info($".NET 6.0 Agent process {_pid} starting");

            if (options.DebugAgent || options.DebugTests)
                TryLaunchDebugger();

            if (!string.IsNullOrEmpty(options.AgencyUrl))
                RegisterAndWaitForCommands(options);
            else
                ExecuteTestsDirectly(options);
        }

        private static void RegisterAndWaitForCommands(AgentOptions options)
        {
            log.Info($"  AgentId:   {options.AgentId}");
            log.Info($"  AgencyUrl: {options.AgencyUrl}");
            log.Info($"  AgencyPid: {options.AgencyPid}");

            if (!string.IsNullOrEmpty(options.AgencyPid))
                LocateAgencyProcess(options.AgencyPid);

            log.Info("Starting RemoteTestAgent");
            Agent = new RemoteTestAgent(options.AgentId);
            Agent.Transport = new TestAgentTcpTransport(Agent, options.AgencyUrl);

            try
            {
                if (Agent.Start())
                    WaitForStop();
                else
                {
                    log.Error("Failed to start RemoteTestAgent");
                    Environment.Exit(AgentExitCodes.FAILED_TO_START_REMOTE_AGENT);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in RemoteTestAgent. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }
            log.Info("Agent process {0} exiting cleanly", _pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static void ExecuteTestsDirectly(AgentOptions options)
        {
            if (options.Files.Count == 0)
                throw new ArgumentException("No file specified for direct execution");

            try
            {
                var testFile = options.Files[0];

                var version = typeof(Net60PluggableAgent).Assembly.GetName().Version;
                Console.WriteLine($"\nNet60PluggableAgent {version}");
                Console.WriteLine($"\nTest File: {options.Files[0]}");

                var runner = new LocalTestRunner(new NUnit.Engine.TestPackage(testFile));
                var xmlResult = runner.Run(null, TestFilter.Empty).Xml;

                Console.WriteLine("\nAgent Result");
                Console.WriteLine($"  Overall result: {xmlResult.GetAttribute("result")}");
                int cases = int.Parse(xmlResult.GetAttribute("testcasecount"));
                int passed = int.Parse(xmlResult.GetAttribute("passed"));
                int failed = int.Parse(xmlResult.GetAttribute("failed"));
                int warnings = int.Parse(xmlResult.GetAttribute("warnings"));
                int inconclusive = int.Parse(xmlResult.GetAttribute("inconclusive"));
                int skipped = int.Parse(xmlResult.GetAttribute("skipped"));
                Console.WriteLine($"  Cases: {cases}, Passed: {passed}, Failed: {failed}, Warnings: {warnings}, Inconclusive: {inconclusive}, Skipped: {skipped}");

                var pathToResultFile = Path.Combine(options.WorkDirectory, "TestResult.xml");
                WriteResultFile(xmlResult, pathToResultFile);
                Console.WriteLine($"Saved result file as {pathToResultFile}");
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }

            Environment.Exit(AgentExitCodes.OK);

        }

        private static void LocateAgencyProcess(string agencyPid)
        {
            var agencyProcessId = int.Parse(agencyPid);
            try
            {
                AgencyProcess = Process.GetProcessById(agencyProcessId);
            }
            catch (Exception e)
            {
                log.Error($"Unable to connect to agency process with PID: {agencyProcessId}");
                log.Error($"Failed with exception: {e.Message} {e.StackTrace}");
                Environment.Exit(AgentExitCodes.UNABLE_TO_LOCATE_AGENCY);
            }
        }

        private static void WaitForStop()
        {
            log.Debug("Waiting for stopSignal");

            while (!Agent.WaitForStop(500))
            {
                if (AgencyProcess.HasExited)
                {
                    log.Error("Parent process has been terminated.");
                    Environment.Exit(AgentExitCodes.PARENT_PROCESS_TERMINATED);
                }
            }

            log.Debug("Stop signal received");
        }

        private static void TryLaunchDebugger()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                Debugger.Launch();
            }
            catch (SecurityException se)
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"System.Security.Permissions.UIPermission is not set to start the debugger. {se} {se.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_SECURITY_VIOLATION);
            }
            catch (NotImplementedException nie) //Debugger is not implemented on mono
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"Debugger is not available on all platforms. {nie} {nie.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED);
            }
        }

        public static void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                WriteResultFile(resultNode, writer);
            }
        }

        public static void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
            {
                xmlWriter.WriteStartDocument(false);
                resultNode.WriteTo(xmlWriter);
            }
        }
    }
}
