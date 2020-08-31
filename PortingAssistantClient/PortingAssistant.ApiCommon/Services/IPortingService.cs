using System;
using System.Collections.Generic;
using PortingAssistantApiCommon.Model;
using PortingAssistantCommon.Model;

namespace PortingAssistantApiCommon.Services
{
    public interface IPortingService
    {
        Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>> ApplyPortingProjectFileChanges(ApplyPortingProjectFileChangesRequest request);
    }
}
