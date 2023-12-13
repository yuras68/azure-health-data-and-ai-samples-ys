// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Security;
using Microsoft.AzureHealth.DataServices.Filters;
using Microsoft.AzureHealth.DataServices.Pipelines;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SMARTCustomOperations.AzureAuth.Extensions;
using SMARTCustomOperations.Export.Configuration;

namespace SMARTCustomOperations.Export.Filters
{
    public class CheckExportJobOutputFilter : IOutputFilter
    {
        private readonly ILogger _logger;
        private readonly ExportCustomOperationsConfig _configuration;
        private readonly string _id;

        private const string TemplateType = "AllergyIntolerance";

        public CheckExportJobOutputFilter(ILogger<CheckExportJobOutputFilter> logger, ExportCustomOperationsConfig configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _id = Guid.NewGuid().ToString();
        }

        public event EventHandler<FilterErrorEventArgs>? OnFilterError;

        public string Name => nameof(CheckExportJobOutputFilter);

        public StatusType ExecutionStatusType => StatusType.Normal;

        public string Id => _id;

        public Task<OperationContext> ExecuteAsync(OperationContext context)
        {
            if (!IsRunningOrCompletedExportCheckJob(context))
            {
                return Task.FromResult(context);
            }

            _logger?.LogInformation("Entered {Name}", Name);

            Uri requestUri = context.Request!.RequestUri!;

            try
            {
                // Replace the content location URL with the public endpoint
                var jBody = JObject.Parse(context.ContentString);

                jBody["requiresAccessToken"] = true;
                var outputArray = (JArray)jBody.SelectToken("output")!;

                foreach (JToken output in outputArray)
                {
                    // Get the orig URL of the output array item
                    var outputObj = (JObject)output;
                    var origUrl = new Uri(outputObj["url"]!.ToString());

                    if (!origUrl.LocalPath.StartsWith("/" + context.Properties["oid"], StringComparison.InvariantCulture))
                    {
                        var ex = new SecurityException($"User attempted export access with token with wrong oid claim. {Id}. OID: {context.Properties["oid"]}. Container: {origUrl.Segments[1]}.");

                        FilterErrorEventArgs error = new(name: Name, id: Id, fatal: true, error: ex, code: HttpStatusCode.Unauthorized);
                        OnFilterError?.Invoke(this, error);
                        return Task.FromResult(context.SetContextErrorBody(error, _configuration.Debug));
                    }

                    outputObj["url"] = BuildNewExportFileUri(_configuration.ApiManagementHostName!, _configuration.ApiManagementFhirPrefex, origUrl.LocalPath);
                }
                _logger?.LogInformation("Custom code for {Name} - start", Name);
                var template = (JObject) ((JArray)jBody.SelectToken("output")!).Where(o => o["type"]!.ToString() == TemplateType).First();
                
                var practitioner = MakeSingleEntry(template, "Practitioner", 6);
                outputArray.Add(practitioner);

                var device = MakeSingleEntry(template, "Device", 3);
                outputArray.Add(device);

                var organization = MakeSingleEntry(template, "Organization", 6);
                outputArray.Add(organization);

                _logger?.LogInformation("Custom code for {Name} - end", Name);

                context.ContentString = jBody.ToString();
            }
            catch (Exception ex)
            {
#pragma warning disable CA2201
                FilterErrorEventArgs error = new(name: Name, id: Id, fatal: true, error: new Exception($"Could not process export check result.", ex), code: HttpStatusCode.InternalServerError, responseBody: context.ContentString);
                OnFilterError?.Invoke(this, error);
                return Task.FromResult(context.SetContextErrorBody(error, _configuration.Debug));
            }
            // ys 2023-12-05 - modify the content (add the Device, Org, and Practitioner elements.
            
            return Task.FromResult(context);
        }

        // ys - 2023-12-05 - making the new entries out of the TemplateType entry
        private JObject MakeSingleEntry(JObject template, string type, int count)
        {
            var result = new JObject();
            result["type"] = type;
            result["url"] = template["url"]!.ToString().Replace(TemplateType, type);
            result["count"] = count;
            return result;
        }

        // Maps the url segment to our proxied storage endpoint
        private static Uri BuildNewExportFileUri(string apiManagementHostname, string? apiManagementEndpointPrefix, string requestLocalPath)
        {
            var outputUriBuilder = new UriBuilder();
            outputUriBuilder.Scheme = "https://";
            outputUriBuilder.Host = apiManagementHostname;

            // Add the API prefix if it exists
            if (apiManagementEndpointPrefix is not null)
            {
                outputUriBuilder.Path += $"{apiManagementEndpointPrefix}/";
            }

            outputUriBuilder.Path += "_export";
            outputUriBuilder.Path += requestLocalPath;

            return outputUriBuilder.Uri;
        }

        // Only execute filter for export job check operations that are running or completed.
        private static bool IsRunningOrCompletedExportCheckJob(OperationContext context)
        {
            if (context.Properties["PipelineType"] != ExportOperationType.ExportCheck.ToString() || context.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return true;
        }
    }
}
