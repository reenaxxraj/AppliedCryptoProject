using AppliedCryptoProject;


Console.WriteLine("\n****Secure File Sharing System****");

while (true)
{
    Boolean loggedIn = false;

    while (!loggedIn)
    {
        Console.Write("1. Login\n2. Create Account\n>");
        string choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                if (AccountManager.Login())
                {
                    loggedIn = true;
                    FileManager.InitialiseFolder();
                    Console.WriteLine("[INFO]: Login Successful");
                    Console.WriteLine("[INFO]: Logged in as "+ AccountManager.userID);
                }
                else
                    Console.WriteLine("[ERROR]: Login Attempt Failed");
                break;

            case "2":
                if (AccountManager.CreateAccount())
                {
                    loggedIn = true;
                    FileManager.InitialiseFolder();
                    Console.WriteLine("[INFO]: Account Created Successfully");
                    Console.WriteLine("[INFO]: Logged in as " + AccountManager.userID);

                }
                else
                {
                    Console.WriteLine("[ERROR]: Account Setup Failed");
                }
                break;

            default:
                Console.WriteLine("[ERROR]: '" + choice + "'" + " is not a valid input");
                break;
        }
    }

    while (loggedIn)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(">");
        string cmd = Console.ReadLine();
        switch (cmd.ToLower())
        {
            case ("upload"):
                if (FileManager.UploadFile())
                    Console.WriteLine("[INFO]: Upload file successful");
                else
                    Console.WriteLine("[ERROR]: Upload file failed.");
                break;

            case ("download"):
                if (FileManager.DownloadFile())
                    Console.WriteLine("[INFO]: Download file successful");
                else
                    Console.WriteLine("[ERROR]: Download file failed.");
                break;

            case ("modify"):
                if (FileManager.ModifyFile())
                    Console.WriteLine("[INFO]: File update successful");
                else
                    Console.WriteLine("[ERROR]: File update failed.");
                break;

            case ("exist"):
                Console.Write("User ID: ");
                string inp = Console.ReadLine();
                Console.Write(inp);
                if (CloudManager.GetIdentity(inp) != (null, null))
                    Console.WriteLine("[INFO] User (" + inp + ") exists");
                else
                    Console.WriteLine("[INFO] User (" + inp + ") does not exist");
                break;

            case ("sharefile"):
                if(FileManager.Sharefile())
                    Console.WriteLine("[INFO] Share file successful");
                else
                    Console.WriteLine("[ERROR] Share file unsuccessful");
                break;

            case ("delete"):
                if (FileManager.DeleteFile())
                    Console.WriteLine("[INFO] Delete file successful");
                else
                    Console.WriteLine("[ERROR] Delete file unsuccessful");
                break;

            case ("unsharefile"):
                if (FileManager.UnshareFile())
                    Console.WriteLine("[INFO] Unshare file successful");
                else
                    Console.WriteLine("[ERROR] Unshare file unsuccessful");
                break;

            case ("getlogs"):
                if (FileManager.AuditLog() == false)
                    Console.WriteLine("[Error]: Unable to get logs. Try again later");
                break;

            case ("logout"):
                loggedIn = false;
                Console.ResetColor();
                Console.WriteLine("[INFO]: Logged out of " + AccountManager.userID);
                break;

            default:
                Console.WriteLine("[ERROR]: Invalid input");
                break;
        }
    }



}