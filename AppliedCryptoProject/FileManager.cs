using System;
using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class FileManager
    {

        public static void UploadFile(byte[] file, string[] userID)
        {

            // byte[] encryptedSharedKey = KeyManager.GenerateEncryptedSymmetricKey();
            // byte[] encryptedFile = EncryptFile(file, sharedKey);

            //Send Encrypted symmetric key json to cloud

        }

        public static void DownloadFile(string fileID)
        {
            //Check if permission exists

            //Get Encrypted Symmetric key

            //Decrypt Symmetric key

            //Retrieve File from cloud

            //Decrypt file

            //Open File
        }

        public static byte[]? GetFile(string path)
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
            else
            {
                Console.WriteLine("ERROR:File does not exist");
                return null;
            }
        }

        public static void OpenFile(string path){

        }

    }
}
