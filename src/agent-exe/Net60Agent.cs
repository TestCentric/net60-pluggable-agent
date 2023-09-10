// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using TestCentric.Engine.Agents;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Communication.Transports.Tcp;

namespace TestCentric.Agents
{
    public class Net60Agent : TestCentricAgent<Net60Agent>
    {
        public static void Main(string[] args) => TestCentricAgent<Net60Agent>.Execute(args);
    }
}
