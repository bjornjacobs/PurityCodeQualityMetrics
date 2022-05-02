﻿using CommandLine;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;


var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));

var project =
//@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
 @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
 //@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

var repo = new InMemoryReportRepo();
repo.Clear();

var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());

var purityReports = await analyzer.GeneratePurityReportsProject(project);
repo.AddRange(purityReports);

ConsoleInterface.PrintOverview(repo);
