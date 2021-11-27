using AppliedCryptoProject;


Console.WriteLine("****Secure File Sharing Application****");

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
                    Console.WriteLine("[INFO]: Login Successful");
                }
                else
                    Console.WriteLine("[ERROR]: Login Attempt Failed");
                break;

            case "2":
                if (AccountManager.CreateAccount())
                {
                    loggedIn = true;
                    Console.WriteLine("[INFO]: Account Created Successfully");
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
        Console.Write(">");
        string cmd = Console.ReadLine();
        switch (cmd.ToLower())
        {
            case ("upload"):
                FileManager.UploadFile();
                break;

            case ("download"):
                FileManager.DownloadFile();
                break;

            case ("modify"):
                break;

            case ("exist"):
                Console.Write("User ID: ");
                string inp = Console.ReadLine();
                Console.Write(inp);
                if (CloudManager.GetIdentity(inp) != (null, null))
                    Console.WriteLine("User exists");
                else
                    Console.WriteLine("User does not exist");
                break;
            case ("share file"):
                FileManager.Sharefile();
                break;
            case ("delete"):
                FileManager.DeleteFile();
                break;
            case ("privatekey"):
                Console.WriteLine(Convert.ToBase64String(KeyManager.RSAalg.ExportParameters(true).P));
                break;

            case ("unshare file"):
                FileManager.UnshareFile();
                break;

            case ("logout"):
                loggedIn = false;
                break;
        }
    }



}