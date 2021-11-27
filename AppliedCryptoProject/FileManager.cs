using System;
using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class FileManager
    {

        public static bool UploadFile()
        {
            string inputFilePath;

            Console.WriteLine("Enter file path: ");
            while (true)
            {
                inputFilePath = Console.ReadLine();
                if (File.Exists(inputFilePath))
                    break;
                Console.WriteLine("File does not exits. Try again");
            }

            //while (true)
            //{
            //    Console.WriteLine("Enter userID to share with (Enter z when done): ");
            //    string userinput = Console.ReadLine();

            //    if (userinput.ToLower() == "z")
            //        break;

            //    /***
            //    Check if user exists
            //    ***/
            //    (byte[], byte[]) publickey;
                

            //}
            //Console.WriteLine("user id list generated");

            //using (StreamWriter file = new StreamWriter("C:/Users/Reena/Documents/publicKeyListToShare.txt"))
            //    foreach (KeyValuePair<string, (byte[], byte[])> kvp in publicKeyList)
            //        file.WriteLine("Key: {0}, n: {1}, e: {2}", kvp.Key, kvp.Value.Item1, kvp.Value.Item2);



            FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open);
            FileInfo fi = new FileInfo(inputFilePath);
            byte[] ext = new byte[8];
            //if (Convert.FromBase64String(fi.Extension).Length > 8)
            //{
            //    Console.WriteLine("[ERROR]: File format not supported");
            //    return false;
            //}
            //else
            //{ 
            //    ext = Convert.FromBase64String(fi.Extension);
            //}

            int startFileName = inputFilePath.LastIndexOf("\\") + 1;
            string outputFile = "C:/Users/Reena/Documents/" + inputFilePath.Substring(startFileName, inputFilePath.LastIndexOf(".") - startFileName) + ".enc";
            FileStream outputFileStream = new FileStream(outputFile, FileMode.Create);

            byte[] encryptedSymmetricKey = KeyManager.EncryptFile(inputFileStream, outputFileStream, ext);

            //string encryptedFileString;
            //using (StreamReader streamReader = File.OpenText(outputFile))
            //{
            //    encryptedFileString = streamReader.ReadToEnd();
            //    //Console.WriteLine(text);
            //}

            byte[] encryptedFile = File.ReadAllBytes(outputFile);  
            string encryptedFileString = Convert.ToBase64String(encryptedFile);
            Console.WriteLine(encryptedFileString);

            if (CloudManager.UploadFileToCloud(encryptedFileString, encryptedSymmetricKey, AccountManager.userID))
            {
                Console.WriteLine("Copy of encrypted file saved in pc at: " + outputFile);
                return true;
            }
            else
                return false;



        }

        public static bool DownloadFile()
        {
            Console.Write("Enter file url: ");
            string fileurl = Console.ReadLine();

            (string, string) response = CloudManager.DownloadFileFromCloud(fileurl, AccountManager.userID);
            if (response == (null,null))
            {
                return false;
            }
            byte[] encryptedFile = Convert.FromBase64String(response.Item1);
            byte[] encryptedSymmetricKey = Convert.FromBase64String(response.Item2);

            Stream stream = new MemoryStream(encryptedFile);

            FileStream outputFileStream = new FileStream("C:/Users/Reena/Documents/file101.txt", FileMode.Create);

            KeyManager.DecryptFile(stream, outputFileStream, encryptedSymmetricKey);

            //Console.WriteLine("File Downloaded, Decrypted and Stored in: " + outFile);
            return true;
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

        public static void Sharefile()
        {
            Console.Write("Enter file url:");
            string fileurl = Console.ReadLine();

            string input;
            IDictionary<string, (byte[],byte[])> publickeytable = new Dictionary<string, (byte[],byte[])>();
            (byte[], byte[]) publickey;

            (string, string) fileResponse = CloudManager.DownloadFileFromCloud(fileurl, AccountManager.userID);

            if (fileResponse == (null, null))
            {
                Console.WriteLine("[ERROR]: File does not exist");
                return;
            }
            byte[] encryptedKey = Convert.FromBase64String(fileResponse.Item2);

            while (true)
            {
                Console.Write("Enter UserID to share file with (Enter # to exit loop): ");
                input = Console.ReadLine();

                if (input == "#")
                    break;

                publickey = CloudManager.GetIdentity(input);

                if (publickey == (null, null))
                {
                    Console.WriteLine(input + " does not exist");
                    continue;
                }
                else
                {
                    publickeytable.Add(input, publickey);
                }
            }

            UsernameKeyPair[] usernamekeypair = KeyManager.GenerateEncryptedSymmetricKey(encryptedKey, publickeytable);
            CloudManager.ShareFile(fileurl,usernamekeypair,AccountManager.userID);

        }

        public static void DeleteFile()
        {
            Console.Write("Enter file name: ");
            string input = Console.ReadLine();

            CloudManager.DeleteFile(input);
        }

        public static void UnshareFile()
        {
            Console.Write("Enter file url:");
            string fileurl = Console.ReadLine();

            string input;
            string[] usersToUnshare = { };

            while (true)
            {
                Console.Write("Enter UserID to unshare file with (Enter # to exit loop): ");
                input = Console.ReadLine();

                if (input == "#")
                    break;

                usersToUnshare.Append(input);
            }

            CloudManager.UnshareFile(fileurl,usersToUnshare);

        }
    }
}
