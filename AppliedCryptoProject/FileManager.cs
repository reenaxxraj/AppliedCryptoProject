using Microsoft.Win32;
using System;
using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class FileManager
    {
        static string downloadsPath;
        static string encryptedPath;

        public static void InitialiseFolder()
        {
            downloadsPath = Directory.CreateDirectory(GetDownloadFolderPath() + "\\SecureFileSharing-Downloads-" + AccountManager.userID).FullName;
            encryptedPath = Directory.CreateDirectory(GetDownloadFolderPath() + "\\SecureFileSharing-EncryptedFiles-" + AccountManager.userID).FullName;
        }
        public static bool UploadFile()
        {
            string inputFilePath;

            while (true)
            {
                Console.Write("Enter file path: ");
                inputFilePath = Console.ReadLine();
                if (File.Exists(inputFilePath))
                    break;
                Console.WriteLine("[ERROR]: File does not exits. Try again");
            }

            FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open);

            string inputFileNameWithExt = Path.GetFileName(inputFilePath);
            string inputFileNameWithoutExt = Path.GetFileName(inputFilePath);
            string outputFile = encryptedPath + "\\" + inputFileNameWithoutExt + ".enc";
            FileStream outputFileStream = new FileStream(outputFile, FileMode.Create);
            byte[] encryptedSymmetricKey = KeyManager.EncryptFile(inputFileStream, outputFileStream, inputFileNameWithExt);

            if (encryptedSymmetricKey == null) 
                return false;

            byte[] encryptedFile = File.ReadAllBytes(outputFile);  
            string encryptedFileString = Convert.ToBase64String(encryptedFile);

            if (CloudManager.UploadFileToCloud(encryptedFileString, encryptedSymmetricKey, AccountManager.userID))
            {
                Console.WriteLine("[INFO]: Copy of encrypted file saved in pc at: " + outputFile);
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

            string outputPath = GetDownloadFolderPath() + "\\" + fileurl + ".txt";
            FileStream outputFileStream = new FileStream(outputPath, FileMode.Create);

            string FileName = KeyManager.DecryptFile(stream, outputFileStream, encryptedSymmetricKey);

            if (FileName == null)
            {
                Console.WriteLine("[ERROR]: Downloaded file has failed signature verification. File will be deleted.");
                File.Delete(outputPath);
                return false;
            }
            else
            {
                string newpath = downloadsPath + "\\" + FileName;
                File.Move(outputPath, newpath, true);
                Console.WriteLine("[INFO]: File Downloaded, Decrypted and Stored. File path: " + newpath);
                return true;
            }
        }

        public static bool Sharefile()
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
                return false;
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
                    Console.WriteLine("[ERRO]: "+ input + " does not exist");
                    continue;
                }
                else
                {
                    publickeytable.Add(input, publickey);
                }
            }

            UsernameKeyPair[] usernamekeypair = KeyManager.GenerateEncryptedSymmetricKey(encryptedKey, publickeytable);
            return CloudManager.ShareFile(fileurl,usernamekeypair,AccountManager.userID);

        }

        public static bool DeleteFile()
        {
            Console.Write("Enter file name: ");
            string input = Console.ReadLine();

            return CloudManager.DeleteFile(input);
        }

        public static bool UnshareFile()
        {
            Console.Write("Enter file url:");
            string fileurl = Console.ReadLine();

            (string, string) fileResponse = CloudManager.DownloadFileFromCloud(fileurl, AccountManager.userID);

            if (fileResponse == (null, null))
            {
                Console.WriteLine("[ERROR]: File url does not exist");
                return false;
            }

            string input;
            List<string> usersToUnshare = new List<string>();

            while (true)
            {
                Console.Write("Enter UserID to unshare file with (Enter # to exit loop): ");
                input = Console.ReadLine();

                if (input == "#")
                    break;

                usersToUnshare.Add(input);
            }

            return CloudManager.UnshareFile(fileurl,usersToUnshare.ToArray());

        }

        public static string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }

        public static bool ModifyFile()
        {
            Console.Write("Enter file url:");
            string fileurl = Console.ReadLine();

            (string, string) fileResponse = CloudManager.DownloadFileFromCloud(fileurl, AccountManager.userID);

            if (fileResponse == (null, null))
            {
                Console.WriteLine("[ERROR]: File url does not exist");
                return false;
            }

            byte[] encryptedSymmetricKey = Convert.FromBase64String(fileResponse.Item2);

            string inputFilePath;
            while (true)
            {
                Console.Write("Enter path of modified file: ");
                inputFilePath = Console.ReadLine();
                if (File.Exists(inputFilePath))
                    break;
                Console.WriteLine("[ERROR]: File does not exits. Try again");
            }

            FileStream inputFileStream;
            try
            {
                inputFileStream = new FileStream(inputFilePath, FileMode.Open);

            }
            catch
            {
                Console.WriteLine("[ERROR]: Unable to read file. Please ensure file is not open else where and try again.");
                return false;

            }
            string inputFileNameWithExt = Path.GetFileName(inputFilePath);
            string inputFileNameWithoutExt = Path.GetFileName(inputFilePath);
            string outputFile = encryptedPath + "\\" + inputFileNameWithoutExt + ".enc";
            FileStream outputFileStream = new FileStream(outputFile, FileMode.Create);

            if (KeyManager.ReEncryptFile(inputFileStream, outputFileStream, inputFileNameWithExt, encryptedSymmetricKey) == null)
                return false;

            byte[] encryptedFile = File.ReadAllBytes(outputFile);
            string encryptedFileString = Convert.ToBase64String(encryptedFile);

            if (CloudManager.UpdateFile(fileurl, encryptedFileString))
            {
                Console.WriteLine("[INFO]: Copy of encrypted file saved in pc at: " + outputFile);
                return true;
            }
            else
                return false;



        }

        public static bool AuditLog()
        {

            //(string, string) fileResponse = CloudManager.DownloadFileFromCloud(fileurl, AccountManager.userID);

            //if (fileResponse == (null, null))
            //{
            //    Console.WriteLine("[ERROR]: File url does not exist");
            //    return false;
            //}

            Console.WriteLine("Set filter options for audit logs");

            Console.Write("Enter file url to filter (Leave empty for all files):");
            string fileurl = Console.ReadLine();

            Console.Write("Enter log type to filter (GetFile, UploadFile, UpdateFile, DeleteFile, ShareFile, UnshareFile, Leave empty for all log types): ");
            string type = "";
            type = Console.ReadLine();

            if (type != "" && type != "GetFile" && type != "UploadFile" && type != "UpdateFile" && type != "DeleteFile" && type != "ShareFile" && type != "UnshareFile")
            {
                Console.WriteLine("[ERROR]: Invalid log type. Setting log type to all");
            }

            int count;
            Console.Write("Enter UserID to filter (Leave empty for all UserIDs): ");
            string userID = Console.ReadLine();

            try
            {
                Console.Write("Enter log count:");
                string inp = Console.ReadLine();
                count = Int32.Parse(inp);
            }
            catch
            {
                Console.WriteLine("[Error]: Unable to read input. Log count will be set to 10");
                count = 10;
            }

            return CloudManager.GetLogs(fileurl, type, count.ToString(), userID);


        }
    }
}
