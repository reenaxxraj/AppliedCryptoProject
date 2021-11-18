using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AppliedCryptoProject
{
    public static class KeyManager
    {
        public static void GenerateSharedKeyTable(string[] userList)
        {

            IDictionary<string, byte[]> publicKeyTable = GetPublicKeyTable(userList);
            IDictionary<string, byte[]> fileAccessTable;

            byte[] sharedKey = GenerateSymmetricKey();

            foreach (string userID in userList)
            {
                fileAccessTable.Add(userID, EncryptSymmetricKey(sharedKey, publicKeyTable[userID], IV);
            }

            //send to cloud
        } 

        public static byte[] GenerateSymmetricKey()
        {
            Aes aes = Aes.Create();
            aes.GenerateKey();
            return aes.Key;
        }

        public static byte[] EncryptSymmetricKey(byte[] symmetricKey, byte[] publicKey, byte[] IV)
        {
            Aes aes = Aes.Create();
            aes.CreateEncryptor(publicKey, IV);
            return aes.EncryptCbc(symmetricKey, IV, PaddingMode.PKCS7);
        }

        public static byte[] DecryptSymmetricKey(byte[] encryptedSymmetricKey, byte[] privateKey, byte[] IV)
        {
            Aes aes = Aes.Create();
            aes.CreateDecryptor(privateKey, IV);
            return aes.DecryptCbc(encryptedSymmetricKey, IV, PaddingMode.PKCS7);
        }

        public static byte[] GenerateRSAKeyPair(string userID)
        {

            return PublicKey
        }


    }
}
