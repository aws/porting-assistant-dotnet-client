﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace PortingAssistant.Client.Model
{
    public class CompatibilityResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility Compatibility { get; set; }
        public List<string> CompatibleVersions { get; set; }
        
        /// <summary>
        /// Returns list of compatible versions with and pre-release (alpha, beta, rc) versions filtered out
        /// </summary>
        public List<string> GetCompatibleVersionsWithoutPreReleases()
        {
            return CompatibleVersions.Where(v => !v.Contains("-")).ToList();
        }
    }
}
