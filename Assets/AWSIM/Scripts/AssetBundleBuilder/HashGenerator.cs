using System.Text;
using MD5 = System.Security.Cryptography.MD5;

namespace AWSIM.Scripts.AssetBundleBuilder
{
    public static class HashGenerator
    {
        /// <summary>
        /// Generates an MD5 hash from the input seed.
        /// </summary>
        public static string GenerateMD5Hash(string inputSeed)
        {
            var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputSeed);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder hashString = new StringBuilder();
            foreach (var t in hashBytes)
            {
                hashString.Append(t.ToString("x2")); // Convert byte to hex
            }

            return hashString.ToString();
        }
    }
}
