using System.Security.Cryptography;
using System.Text;

namespace AppliedCryptoProject
{
    public static class KeyManager
    {
        public static RSACryptoServiceProvider RSAalg;
        public static RSAParameters RSAParams;
        public static (byte[],byte[]) publicKey;
        public static RSAPKCS1SignatureFormatter formatter;

        public static (byte[], byte[]) GenerateUserRSAKeyPair()
        {
            byte[] n, exp;
            (byte[], byte[]) publicKey;

            try
            {
                CspParameters cspParams = new CspParameters();
                RSAalg = new RSACryptoServiceProvider(2048, cspParams);
                RSAParams = RSAalg.ExportParameters(true);

                n = RSAParams.Modulus;
                exp = RSAParams.Exponent;
                publicKey = (n, exp);

                return publicKey;

            }
            catch
            {
                return (null, null);
            }

        }


        public static bool StoreRSAKeyPair() 
        {

            try
            {
                CspParameters cspParams = new CspParameters()
                {
                    KeyContainerName = AccountManager.userID
                };

                RSAalg = new RSACryptoServiceProvider(2048, cspParams);
                RSAalg.ImportParameters(RSAParams);

                Console.WriteLine("[INFO]: RSA public/private key pair generated and persisted in key container \"{0}\".", AccountManager.userID);

                if (LoadRSAKeyPair(publicKey))
                    return true;
                else
                {
                    Console.WriteLine("[ERROR]: Unable to successfully setup RSA public/private key pair. Please try again");
                    return false;
                }

            }
            catch
            {
                return false;
            }

        }

        public static Boolean LoadRSAKeyPair( (byte[], byte[]) publicKey)
        {

            try
            {
                CspParameters cspParams = new CspParameters()
                {
                    Flags = CspProviderFlags.UseExistingKey,
                    KeyContainerName = AccountManager.userID
                };

                RSAalg = new RSACryptoServiceProvider(2048, cspParams);
                RSAParams = RSAalg.ExportParameters(true);
                Console.WriteLine("[INFO]: RSA Public/Private Key found in computer.");
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("[INFO]: Private Key not found in computer");

                return false;

                /***
                Add code to load private key file
                ***/

            }

            return true;
        }

        public static UsernameKeyPair[] GenerateEncryptedSymmetricKey(byte[] encryptedSymmetricKey, IDictionary<string, (byte[], byte[])> publicKeyList)
        {

            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);
            string userID;
            byte[] n, exp;
            byte[] symmetricKey = RSAalg.Decrypt(encryptedSymmetricKey, false);
            List<UsernameKeyPair> symmetricKeyTable = new List<UsernameKeyPair>();

            foreach (KeyValuePair<string, (byte[], byte[])> keyValuePair in publicKeyList)
            {
                userID = keyValuePair.Key;
                n = keyValuePair.Value.Item1;
                exp = keyValuePair.Value.Item2;

                RSAParameters RSAKeyInfo = RSA.ExportParameters(false);
                RSAKeyInfo.Modulus = n;
                RSAKeyInfo.Exponent = exp;
                RSA.ImportParameters(RSAKeyInfo);
                encryptedSymmetricKey = RSA.Encrypt(symmetricKey, false);

                symmetricKeyTable.Add(new UsernameKeyPair(EncryptedKey: Convert.ToBase64String(encryptedSymmetricKey), TaggedUsername: userID));
            }

            return symmetricKeyTable.ToArray();
        }
        public static void PrintByteArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write($"{array[i]:X2}");
                if ((i % 4) == 3) Console.Write(" ");
            }
            Console.WriteLine();
        }


        public static byte[] EncryptFile(FileStream inputFile, FileStream outputFile, string filename)
        {
            try
            {

                inputFile.Position = 0;

                Aes aes = Aes.Create();
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] LenIV = new byte[4];
                int lIV = aes.IV.Length;
                LenIV = BitConverter.GetBytes(lIV);

                byte[] LenOwnerID = new byte[4];
                byte[] OwnerID = Encoding.ASCII.GetBytes(AccountManager.userID);
                int lOwnerID = OwnerID.Length;
                LenOwnerID = BitConverter.GetBytes(lOwnerID);

                byte[] lenFileName = new byte[4];
                byte[] fileName = Encoding.ASCII.GetBytes(filename);
                int lenfilename = fileName.Length;
                lenFileName = BitConverter.GetBytes(lenfilename);

                inputFile.Position = 0;

                SHA256 mySHA256 = SHA256.Create();
                byte[] hashValue = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(lIV.ToString() + Convert.ToBase64String(aes.IV) + lOwnerID.ToString() + AccountManager.userID + lenfilename.ToString() + filename + inputFile));
                byte[] sign = RSAalg.SignHash(hashValue, CryptoConfig.MapNameToOID("SHA256"));
                int lSign = sign.Length;
                byte[] lenSign = BitConverter.GetBytes(lSign);


                using (outputFile)
                {
                    outputFile.Write(LenIV, 0, 4);
                    outputFile.Write(aes.IV, 0, lIV);
                    outputFile.Write(lenSign, 0, 4);
                    outputFile.Write(sign, 0, lSign);
                    outputFile.Write(LenOwnerID, 0, 4);
                    outputFile.Write(OwnerID, 0, lOwnerID);
                    outputFile.Write(lenFileName, 0, 4);

                    using (CryptoStream outStreamEncrypted = new CryptoStream(outputFile, transform, CryptoStreamMode.Write))
                    {
                        outStreamEncrypted.Write(fileName, 0, lenfilename);

                        int count = 0;
                        int offset = 0;
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int bytesRead = 0;

                        using (inputFile)
                        {
                            do
                            {
                                count = inputFile.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamEncrypted.Write(data, 0, count);
                                bytesRead += blockSizeBytes;
                            }
                            while (count > 0);
                            inputFile.Close();
                        }
                        outStreamEncrypted.FlushFinalBlock();
                        outStreamEncrypted.Close();
                    }
                    outputFile.Close();
                }

                return RSAalg.Encrypt(aes.Key, false);
            }
            catch
            {
                Console.WriteLine("[ERROR]: Unable to encrypt file. Try again");
                return null;
            }
        }

        public static string DecryptFile( Stream inputFile, FileStream outputFile, byte[] encryptedSymmetricKey)
        {
            try
            {
                byte[] sharedkey = RSAalg.Decrypt(encryptedSymmetricKey, false);

                Aes aes = Aes.Create();
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.PKCS7;

                byte[] LenIV = new byte[4];
                inputFile.Seek(0, SeekOrigin.Begin);
                inputFile.Read(LenIV, 0, 3);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                byte[] IV = new byte[lenIV];
                inputFile.Seek(4, SeekOrigin.Begin);
                inputFile.Read(IV, 0, lenIV);

                byte[] LenSign = new byte[4];
                inputFile.Seek(4 + lenIV, SeekOrigin.Begin);
                inputFile.Read(LenSign, 0, 3);
                int lenSign = BitConverter.ToInt32(LenSign, 0);

                byte[] sign = new byte[lenSign];
                inputFile.Seek(4 + lenIV + 4, SeekOrigin.Begin);
                inputFile.Read(sign, 0, lenSign);

                byte[] LenOwnerID = new byte[4];
                inputFile.Seek(4 + lenIV + 4 + lenSign, SeekOrigin.Begin);
                inputFile.Read(LenOwnerID, 0, 3);
                int lenOwnerID = BitConverter.ToInt32(LenOwnerID, 0);

                byte[] OwnerID = new byte[lenOwnerID];
                inputFile.Seek(4 + lenIV + 4 + lenSign + 4, SeekOrigin.Begin);
                inputFile.Read(OwnerID, 0, lenOwnerID);

                byte[] LenFileName = new byte[4];
                inputFile.Seek(4 + lenIV + 4 + lenSign + 4 + lenOwnerID, SeekOrigin.Begin);
                inputFile.Read(LenFileName, 0, 3);
                int lenFileName = BitConverter.ToInt32(LenFileName, 0);

                int startC = 4 + lenIV + 4 + lenSign + 4 + lenOwnerID + 4;

                ICryptoTransform transform = aes.CreateDecryptor(sharedkey, IV);

                SHA256 mySHA256 = SHA256.Create();
                byte[] FileNameBuffer = new byte[lenFileName];
                byte[] FileName = new byte[lenFileName];
                byte[] hash;


                using (MemoryStream memStream = new MemoryStream(100))
                {

                    int count = 0;
                    int offset = 0;
                    int blockSizeBytes = aes.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];

                    inputFile.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(memStream, transform, CryptoStreamMode.Write))
                    {
                        count = inputFile.Read(FileNameBuffer, 0, lenFileName);
                        outStreamDecrypted.Write(FileNameBuffer, 0, count);

                        do
                        {
                            count = inputFile.Read(data, 0, data.Length);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);
                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();

                        memStream.Position = 0;
                        count = memStream.Read(FileName, 0, lenFileName);
                        memStream.Position = lenFileName;

                        using (outputFile)
                        {
                            memStream.CopyTo(outputFile);
                            outputFile.Position = 0;
                            hash = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(lenIV.ToString() + Convert.ToBase64String(IV) + lenOwnerID.ToString() + Encoding.ASCII.GetString(OwnerID) + lenFileName.ToString() + Encoding.ASCII.GetString(FileName) + outputFile));
                            outputFile.Close();
                        }
                        outStreamDecrypted.Close();
                        memStream.Close();

                    }
                }
                inputFile.Close();


                string OwnerUserID = Encoding.ASCII.GetString(OwnerID);
                Boolean valid = VerifySign(hash, sign, OwnerUserID);
                Console.WriteLine("[INFO]: Signature validity: " + valid);

                if (valid)
                {
                    return Encoding.ASCII.GetString(FileName);
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("[ERROR]: Unable to decrypt file");
                return null;
            }
        }

        public static byte[] ReEncryptFile(FileStream inputFile, FileStream outputFile, string filename, byte[] encryptedSymmetricKey)
        {
            try
            {
                byte[] sharedkey = RSAalg.Decrypt(encryptedSymmetricKey, false);

                inputFile.Position = 0;

                Aes aes = Aes.Create();
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform transform = aes.CreateEncryptor(sharedkey, aes.IV);

                byte[] LenIV = new byte[4];
                int lIV = aes.IV.Length;
                LenIV = BitConverter.GetBytes(lIV);

                byte[] LenOwnerID = new byte[4];
                byte[] OwnerID = Encoding.ASCII.GetBytes(AccountManager.userID);
                int lOwnerID = OwnerID.Length;
                LenOwnerID = BitConverter.GetBytes(lOwnerID);

                byte[] lenFileName = new byte[4];
                byte[] fileName = Encoding.ASCII.GetBytes(filename);
                int lenfilename = fileName.Length;
                lenFileName = BitConverter.GetBytes(lenfilename);

                inputFile.Position = 0;

                SHA256 mySHA256 = SHA256.Create();
                byte[] hashValue = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(lIV.ToString() + Convert.ToBase64String(aes.IV) + lOwnerID.ToString() + AccountManager.userID + lenfilename.ToString() + filename + inputFile));
                byte[] sign = RSAalg.SignHash(hashValue, CryptoConfig.MapNameToOID("SHA256"));
                int lSign = sign.Length;
                byte[] lenSign = BitConverter.GetBytes(lSign);


                using (outputFile)
                {
                    outputFile.Write(LenIV, 0, 4);
                    outputFile.Write(aes.IV, 0, lIV);
                    outputFile.Write(lenSign, 0, 4);
                    outputFile.Write(sign, 0, lSign);
                    outputFile.Write(LenOwnerID, 0, 4);
                    outputFile.Write(OwnerID, 0, lOwnerID);
                    outputFile.Write(lenFileName, 0, 4);

                    using (CryptoStream outStreamEncrypted = new CryptoStream(outputFile, transform, CryptoStreamMode.Write))
                    {
                        outStreamEncrypted.Write(fileName, 0, lenfilename);

                        int count = 0;
                        int offset = 0;
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int bytesRead = 0;

                        using (inputFile)
                        {
                            do
                            {
                                count = inputFile.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamEncrypted.Write(data, 0, count);
                                bytesRead += blockSizeBytes;
                            }
                            while (count > 0);
                            inputFile.Close();
                        }
                        outStreamEncrypted.FlushFinalBlock();
                        outStreamEncrypted.Close();
                    }
                    outputFile.Close();
                }

                return RSAalg.Encrypt(aes.Key, false);
            }
            catch
            {
                Console.WriteLine("[ERROR]: Unable to encrypt file. Try again");
                return null;
            }
        }

        public static byte[] Sign(string plaintext)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(RSAalg.ExportParameters(true));
            formatter = new RSAPKCS1SignatureFormatter(rsa);
            formatter.SetHashAlgorithm("SHA256");
            byte[] data = Encoding.ASCII.GetBytes(plaintext);
            var hash = SHA256.HashData(data);
            return formatter.CreateSignature(hash);        
        }

        public static bool VerifySign(byte[] hash, byte[] sign, string OwnerID)
        {
            (byte[], byte[]) publicKey = CloudManager.GetIdentity(OwnerID);
            RSACryptoServiceProvider rSA = new RSACryptoServiceProvider();

            RSAParameters rsaKeyInfo = rSA.ExportParameters(false);
            rsaKeyInfo.Modulus = publicKey.Item1;
            rsaKeyInfo.Exponent = publicKey.Item2;
            rSA.ImportParameters(rsaKeyInfo);

            RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rSA);
            rsaDeformatter.SetHashAlgorithm("SHA256");
            return rsaDeformatter.VerifySignature(hash, sign);
        }

    }
}
