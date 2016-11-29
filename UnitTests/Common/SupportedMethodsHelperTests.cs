﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Helpers;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Models;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Models.Commands;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.UnitTests.Common
{
    public class SupportedMethodsHelperTests
    {
        [Fact]
        public void GenerateSupportedMethodsReportedPropertyTest()
        {
            var commands = new List<Command>() {
                // Method with parameters
                new Command("method1", DeliveryType.Method, "desc1", new List<Parameter>() {
                    new Parameter("p1", "string"),
                    new Parameter("p2", "int")
                }),
                // Command, should be ignored
                new Command("command1", DeliveryType.Method, "desc1", new List<Parameter>() {
                    new Parameter("p1", "int"),
                    new Parameter("p2", "string")
                }),
                // Method without parameters
                new Command("method2", DeliveryType.Method, "desc2"),
                // Method name with _
                new Command("method_3", DeliveryType.Method, "desc3"),
                // Method without name, should be ignored
                new Command("", DeliveryType.Method, "desc2"),
                // parameter with no type, should be ignored
                new Command("method4", DeliveryType.Method, "desc1", new List<Parameter>() {
                    new Parameter("p1", ""),
                    new Parameter("p2", "int")
                }),
            };

            var property = SupportedMethodsHelper.GenerateSupportedMethodsReportedProperty(commands);

            JObject supportedMethods = property["SupportedMethods"] as JObject;
            Assert.Equal(supportedMethods.Count, commands.Where(c => c.DeliveryType == DeliveryType.Method).Count());

            Assert.Equal(supportedMethods["method1_string_int"]["Name"].ToString(), "method1");
            Assert.Equal(supportedMethods["method1_string_int"]["Description"].ToString(), "desc1");
            Assert.Equal(supportedMethods["method1_string_int"]["Parameters"]["p1"]["Type"].ToString(), "string");
            Assert.Equal(supportedMethods["method1_string_int"]["Parameters"]["p2"]["Type"].ToString(), "int");

            Assert.Equal(supportedMethods["method2"]["Name"].ToString(), "method2");
            Assert.Equal(supportedMethods["method2"]["Description"].ToString(), "desc2");

            Assert.Equal(supportedMethods["method__3"]["Name"].ToString(), "method_3");

            var device = new DeviceModel();
            var twin = new Twin();
            twin.Properties.Reported["SupportedMethods"] = supportedMethods;

            SupportedMethodsHelper.AddSupportedMethodsFromReportedProperty(device, twin);
            Assert.Equal(supportedMethods.Count - 2, device.Commands.Count);
            foreach(var command in device.Commands)
            {
                var srcCommand = commands.FirstOrDefault(c => c.Name == command.Name);
                Assert.Equal(command.Name, srcCommand.Name);
                Assert.Equal(command.Description, srcCommand.Description);
                Assert.Equal(command.Parameters.Count, srcCommand.Parameters.Count);

                foreach (var parameter in command.Parameters)
                {
                    var srcParameter = srcCommand.Parameters.FirstOrDefault(p => p.Name == parameter.Name);
                    Assert.Equal(parameter.Name, srcParameter.Name);
                    Assert.Equal(parameter.Type, srcParameter.Type);
                }
            }
        }
    }
}
