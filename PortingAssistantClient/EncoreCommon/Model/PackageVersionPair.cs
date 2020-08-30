﻿using System;
namespace EncoreCommon.Model
{
    public class PackageVersionPair
    {
        public string PackageId { get; set; }
        public string Version { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageVersionPair pair &&
                   PackageId == pair.PackageId &&
                   Version == pair.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version);
        }
    }
}
