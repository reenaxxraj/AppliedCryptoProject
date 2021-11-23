using System.Security.Cryptography;

namespace AppliedCryptoProject
{
    public static class KeyManager
    {
        private static Aes aes = Aes.Create();
        private static RSACryptoServiceProvider RSAalg;

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

        public static FileStream EncryptFile(FileStream inputFile, IDictionary<string, (byte[],byte[])> publicKeyList){

            // Create instance of Aes for symmetric encryption of the data.
            Aes aes = Aes.Create();
            ICryptoTransform transform = aes.CreateEncryptor();

            IDictionary<string, byte[]> encryptedSymmetricKeyTable = GenerateEncryptedSymmetricKey(aes.Key, publicKeyList);

            // Create byte arrays to contain
            // the length values of the key and IV.
            byte[] LenIV = new byte[4];

            int lIV = aes.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);


            // Write the following to the FileStream
            // for the encrypted file (outFs):
            // - length of the IV
            // - the IV
            // - the encrypted cipher content

            string outFile = "C:/Users/Reena/Desktop";    
            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {

                outFs.Write(LenIV, 0, 4);
                outFs.Write(aes.IV, 0, lIV);

                // Now write the cipher text using
                // a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    // By encrypting a chunk at
                    // a time, you can save memory
                    // and accommodate large files.
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
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
                outFs.Close();
                return outFs;
            }        
        }

        public static byte[] DecryptFile(string inputFile){
            
            return null;
        }

        public static void reEncryptFile(){

        }

        public static void UploadPrivateKeyFile(string path){

        }

        public static void DownloadPrivateKeyFile(string path){

        }

        public static byte[] EncryptWithSymmetricKey(byte[] symmetricKey, byte[] plaintext, byte[] IV)
        {
            aes.CreateEncryptor(symmetricKey, IV);
            return aes.EncryptCbc(plaintext, IV, PaddingMode.PKCS7);
        }

        public static byte[] DecryptWithSymmetricKey(byte[] symmetricKey, byte[] ciphertext, byte[] IV)
        {
            aes.CreateDecryptor(symmetricKey, IV);
            return aes.DecryptCbc(ciphertext, IV, PaddingMode.PKCS7);
        }

    }
}
