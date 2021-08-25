using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace PortingAssistant.Client.Client.Utils
{
    public class HashUtils
    {
        public static string GenerateGuid(List<string> guids)
        {
            if (guids == null || guids.Count == 0)
            {
                return null;
            }

            guids.Sort();

            string inputStr = String.Join(",", guids);
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = md5.ComputeHash(Encoding.Default.GetBytes(inputStr));
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return new Guid(hashBytes).ToString().ToLower();
            }
        }
    }
}
