using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class KeyManager
    {
        private static RSACryptoServiceProvider RSAalg;
        public static IDictionary<string, (byte[],byte[])> publicKeyList =  new Dictionary<string, (byte[], byte[])>();

        public static IDictionary<string, byte[]> fileAccessTable = new Dictionary<string, byte[]>(); //File ID || EncryptedSymmetricKey

        static KeyManager()
        {
            string[] userIDList = {"user1", "user2", "user3", "user4"};

            foreach (string id in userIDList)
            {
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2408);
                publicKeyList.Add(id, (RSA.ExportParameters(false).Modulus, RSA.ExportParameters(false).Modulus));
                   
            }   

            foreach(KeyValuePair<string, (byte[],byte[])> kvp in publicKeyList)
                Console.WriteLine("Key: {0}, n: {1}, e: {2}", kvp.Key, kvp.Value.Item1, kvp.Value.Item2);

        }
        
        public static (byte[], byte[]) GenerateUserRSAKeyPair(string userID)
        {
            byte[] n, exp;
            (byte[], byte[]) publicKey; //public key = (n, exponent)

            try
            {
                /***
                Create a new instance of RSACryptoServiceProvider to generate a new key pair. 
                Pass the CspParameters class to persist the key in the container.
                ***/
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = userID;
                RSAalg = new RSACryptoServiceProvider(2408, cspParams);
                Console.WriteLine("The RSA public/private key pair generated and persisted in the container, \"{0}\".", userID);
                
                //Load n and exponent from RSA instance and return it
                RSAParameters RSAParams = RSAalg.ExportParameters(false);
                n = RSAParams.Modulus; 
                exp = RSAParams.Exponent;
                publicKey = (n,exp);
                return publicKey; 
            }
            catch(CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return (null, null);
            }

        }

        public static void LoadRSAKeyPair(string userID){

            CspParameters cspParams = new CspParameters(){
                Flags = CspProviderFlags.UseExistingKey,
                KeyContainerName = userID
            };

            try
            {
                RSAalg = new RSACryptoServiceProvider(2408, cspParams);
                Console.WriteLine("Public/Private Key found on computer.");
            }
            catch(CryptographicException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Private Key not found on computer");

                /***
                Add code to load private key file
                ***/
            }
        }

        public static IDictionary<string, byte[]> GenerateEncryptedSymmetricKey(byte[] symmetricKey, IDictionary<string, (byte[],byte[])> publicKeyList){

            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSAParameters p;
            string userID;
            byte[] n, exp;
            byte[] encryptedSymmetricKey;
            IDictionary<string, byte[]> symmetricKeyTable = new Dictionary<string, byte[]>();

            foreach (KeyValuePair<string, (byte[],byte[])> keyValuePair in publicKeyList)
            {
                userID = keyValuePair.Key;
                n = keyValuePair.Value.Item1;
                exp = keyValuePair.Value.Item2;

                RSAParameters RSAKeyInfo = new RSAParameters();
                RSAKeyInfo.Modulus = n;
                RSAKeyInfo.Exponent = exp;
                RSA.ImportParameters(RSAKeyInfo);

                //Encrypt symmetric key with public key
                encryptedSymmetricKey = RSA.Encrypt(symmetricKey, true);  
                symmetricKeyTable.Add(userID, encryptedSymmetricKey);  
            }

            return symmetricKeyTable;
        }

        public static IDictionary<string, byte[]> EncryptFile(string fileID, FileStream inputFile, FileStream outputFile, IDictionary<string, (byte[],byte[])> publicKeyList){

            // Create instance of Aes for symmetric encryption of the data.
            Aes aes = Aes.Create();
            ICryptoTransform transform = aes.CreateEncryptor();

            //REMOVE LATER
            fileAccessTable.Add(fileID, RSAalg.Encrypt(aes.Key, true));

            IDictionary<string, byte[]> encryptedSymmetricKeyTable = GenerateEncryptedSymmetricKey(aes.Key, publicKeyList);

            byte[] LenIV = new byte[4];
            int lIV = aes.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // Write the following to the FileStream
            // for the encrypted file (outFs):
            // - length of the IV
            // - the IV
            // - the encrypted cipher content
   
            using (outputFile)
            {
                outputFile.Write(LenIV, 0, 4);
                outputFile.Write(aes.IV, 0, lIV);

                // Now write the cipher text using
                // a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new CryptoStream(outputFile, transform, CryptoStreamMode.Write))
                {
                    // Encrypting a chunk at a time to accommodate large files.
                    int count = 0;
                    int offset = 0;

                    int blockSizeBytes = aes.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;
                    do
                    {
                        count = inputFile.Read(data, 0, blockSizeBytes);
                        offset += count;
                        outStreamEncrypted.Write(data, 0, count);
                        bytesRead += blockSizeBytes;
                    }
                    while (count > 0);
                    inputFile.Close();
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outputFile.Close();
                return encryptedSymmetricKeyTable;
            }        
        }

        public static void DecryptFile(string fileID, FileStream inputFile, FileStream outputFile, byte[] encryptedSymmetricKey){
            
            byte[] sharedkey = RSAalg.Decrypt(encryptedSymmetricKey, true);
            
            Aes aes = Aes.Create();

            byte[] LenIV = new byte[4];

            inputFile.Seek(0, SeekOrigin.Begin);
            inputFile.Read(LenIV, 0, 3);

            // Convert the lengths to integer values.
            int lenIV = BitConverter.ToInt32(LenIV, 0);

            // Determine the start postition of
            // the ciphter text (startC)
            // and its length(lenC).
            int startC = lenIV + 8;
            int lenC = (int)inputFile.Length - startC;

            // Create the byte arrays for
            // the encrypted Aes key,
            // the IV, and the cipher text.
            byte[] IV = new byte[lenIV];

            // Extract the key and IV
            // starting from index 8
            // after the length values.
            inputFile.Seek(8, SeekOrigin.Begin);
            inputFile.Read(IV, 0, lenIV);

            // Decrypt the key.
            ICryptoTransform transform = aes.CreateDecryptor(sharedkey, IV);

            // Decrypt the cipher text from
            // from the FileSteam of the encrypted
            // file (inFs) into the FileStream
            // for the decrypted file (outFs).
            using (outputFile)
            {

                int count = 0;
                int offset = 0;

                // blockSizeBytes can be any arbitrary size.
                int blockSizeBytes = aes.BlockSize / 8;
                byte[] data = new byte[blockSizeBytes];

                // By decrypting a chunk a time,
                // you can save memory and
                // accommodate large files.

                // Start at the beginning
                // of the cipher text.
                inputFile.Seek(startC, SeekOrigin.Begin);
                using (CryptoStream outStreamDecrypted = new CryptoStream(outputFile, transform, CryptoStreamMode.Write))
                {
                    do
                    {
                        count = inputFile.Read(data, 0, blockSizeBytes);
                        offset += count;
                        outStreamDecrypted.Write(data, 0, count);
                    }
                    while (count > 0);

                    outStreamDecrypted.FlushFinalBlock();
                    outStreamDecrypted.Close();
                }
                outputFile.Close();
            }
            inputFile.Close();
          
        }

        public static void ModifyFile(){

        }

        public static void UploadPrivateKeyFile(string path){

        }

        public static void DownloadPrivateKeyFile(string path){

        }

        private static void Sign()
        {

        }

        public static Boolean CheckSignature((byte[],byte[]) publicKey)
        {
            return true;
        } 
    }
}
