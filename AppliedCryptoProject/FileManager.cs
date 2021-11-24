using System;
using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class FileManager
    {

        public static void UploadFile(string[] userID)
        {
            string inputFilePath;

            string fileid = "101";
            Console.WriteLine("Enter file path: ");
            while (true)
            {
                inputFilePath = Console.ReadLine();  
                if (File.Exists(inputFilePath))
                    break;
                Console.WriteLine("File does not exits. Try again");
            }

            IDictionary<string, (byte[],byte[])> publicKeyList =  new Dictionary<string, (byte[], byte[])>();

            Console.WriteLine("Enter userID to share with: ");
            while (true)
            {
                string userinput = Console.ReadLine();

                if (userinput.ToLower() == "Z")
                    break;

                /***
                Check if user exists
                ***/
                (byte[], byte[]) publickey;
                KeyManager.publicKeyList.TryGetValue(userinput, out publickey);
                publicKeyList.Add(userinput, publickey);

            } 

            using (StreamWriter file = new StreamWriter("publicKeyListToShare.txt"))
                foreach(KeyValuePair<string, (byte[],byte[])> kvp in publicKeyList)
                    file.WriteLine("Key: {0}, n: {1}, e: {2}", kvp.Key, kvp.Value.Item1, kvp.Value.Item2); 



            FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open);
            // Change the file's extension to ".enc"
            int startFileName = inputFilePath.LastIndexOf("\\") + 1;
            string outputFile = inputFilePath.Substring(startFileName, inputFilePath.LastIndexOf(".") - startFileName) + ".enc";
            FileStream outputFileStream = new FileStream(outputFile, FileMode.Create);
            IDictionary<string, byte[]> encryptedSymmetricKeyTable = KeyManager.EncryptFile(fileid, inputFileStream, outputFileStream, publicKeyList);

            /*
            Send Output File & Encrypted Private keys to cloud 
            */
            
            Console.WriteLine("Copy of encrypted file saved in pc");

        }

        public static void DownloadFile(string filepath)
        {

            //Check if permission exists
            string fileid = "101";

            byte[] encryptedSymmetricKey;
            KeyManager.fileAccessTable.TryGetValue(fileid, out encryptedSymmetricKey);
            
            //Get Encrypted Symmetric key
            FileStream inputFileStream = new FileStream(filepath, FileMode.Open);
            string outFile = filepath.Substring(0, filepath.LastIndexOf(".")) + ".txt";
            FileStream outputFileStream = new FileStream(outFile, FileMode.Create);

            KeyManager.DecryptFile(fileid, inputFileStream, outputFileStream, encryptedSymmetricKey);

            Console.WriteLine("File Downloaded, Decrypted and Stored in: " + outFile); 
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
