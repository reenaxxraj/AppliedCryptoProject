using System;
using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public class StoreKey
    {
        public static void SaveInContainer()
        {
            var parameters = new CspParameters
            {
                KeyContainerName = "PrivateKey"
            };

            // Create a new instance of RSACryptoServiceProvider that accesses
            // the key container MyKeyContainerName.
            using var rsa = new RSACryptoServiceProvider(parameters);

            // Display the key information to the console.

        }

        public static void GetKeyFromContainer()
    }
}
