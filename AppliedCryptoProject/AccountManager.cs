using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppliedCryptoProject
{
    public static class AccountManager
    {
        public static Account Login(string userID)
        {

            if (CheckAccountExistance(userID) == true)
            {
                Console.WriteLine("Password:");
                string password = Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Account does not exist. Proceed to create account (Yes/No) ?");
                string input = Console.ReadLine();

                if (input == "Yes")
                {
                    return CreateAccount(userID);
                }
                else
                {
                    return null;
                }


            }
        }

        public static Boolean CheckAccountExistance(string userID)
        {
            //Check Cloud for account existance
            return true;
        }

        public static Account CreateAccount(string userID)
        {

        }
    }
}
