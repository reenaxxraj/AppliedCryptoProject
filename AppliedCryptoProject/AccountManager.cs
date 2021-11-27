
namespace AppliedCryptoProject
{
    public static class AccountManager
    {
        public static string userID;
       
        public static Boolean Login()
        {

            Console.Write("Enter User ID: ");
            string input = Console.ReadLine();

            (byte[], byte[]) publicKey = RetrieveAccountDetails(input);

            if (publicKey != (null,null))
            {
                userID = input;              
                if (!KeyManager.LoadRSAKeyPair(publicKey))
                    return false;
                else
                {

                    return true;
                }
                    
            }
            else
            {
                Console.Write("Account does not exist. Try again");
                    return false;
            }

            
        }

        public static (byte[], byte[]) RetrieveAccountDetails(string userID)
        {
            (byte[], byte[]) publicKey;
            publicKey = CloudManager.GetIdentity(userID);
            return publicKey;
        }

        public static bool CreateAccount()
        {

            Console.WriteLine("Enter User ID: ");
            string input = Console.ReadLine();
            (byte[], byte[]) publicKey = RetrieveAccountDetails(input);

            if (publicKey != (null, null))
            {
                Console.WriteLine("[ERROR]: User ID exists, Try again");
                return false;
            }

            publicKey = KeyManager.GenerateUserRSAKeyPair();
            if (publicKey == (null, null))
            {
                Console.WriteLine("[ERROR]: Unable to generate RSA public/private key pair");
                return false;
            }
            else
            {
                if (CloudManager.CreateIdentity(input, publicKey))
                    return KeyManager.StoreRSAKeyPair();
                    //return true;
                else
                    return false;
            }


        }
    }
}
