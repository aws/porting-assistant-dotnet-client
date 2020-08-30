using System;
using System.Collections.Generic;
using EncoreApiCommon.Model;
using EncoreCommon.Model;

namespace EncoreApiCommon.Services
{
    public interface IPortingService
    {
        Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>> ApplyPortingProjectFileChanges(ApplyPortingProjectFileChangesRequest request);
    }
}
