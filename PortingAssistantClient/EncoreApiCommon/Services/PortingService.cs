using System;
using System.Collections.Generic;
using System.Linq;
using EncoreApiCommon.Model;
using EncoreCommon.Model;
using EncorePorting;
using Microsoft.Extensions.Logging;

namespace EncoreApiCommon.Services
{
    public class PortingService : IPortingService
    {
        private readonly ILogger _logger;
        private readonly IPortingHandler _handler;

        public PortingService(ILogger<PortingService> logger, IPortingHandler handler)
        {
            _logger = logger;
            _handler = handler;
        }

        public Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>> ApplyPortingProjectFileChanges(ApplyPortingProjectFileChangesRequest request)
        {
            try
            {
                var results = _handler.ApplyPortProjectFileChanges(request.ProjectPaths, request.SolutionPath,
                    request.TargetFramework, request.UpgradeVersions);
                return new Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>
                {
                    Value = results.Where(r => r.Success == true).ToList(),
                    Status = Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>.Success(),
                    ErrorValue = results.Where(r => r.Success == false).ToList()
                };
            }
            catch (Exception ex)
            {
                return new Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>
                {
                    Status = Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>.Failed(ex),
                };
            }
        }
    }
}
