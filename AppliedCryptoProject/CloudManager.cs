using ConsoleTables;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AppliedCryptoProject
{
    public static class CloudManager
    {
        private static readonly HttpClient clientIdentity = new HttpClient();
        private static readonly HttpClient clientFileSharer = new HttpClient();

        static CloudManager()
        {
            clientIdentity.BaseAddress = new Uri("http://localhost:81/Identity/");
            clientFileSharer.BaseAddress = new Uri("http://localhost:80/FileSharer/");

        }

        public static bool CreateIdentity(string userID, (byte[], byte[]) publicKey)
        {
            RSAPubKey pubKey = new RSAPubKey(Modulus: Convert.ToBase64String(publicKey.Item1), Exponent: Convert.ToBase64String(publicKey.Item2));
            CreateIdentityRequest Request = new CreateIdentityRequest(PublicKey: pubKey, Username: userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(Request));
            CreateIdentitySignedRequest SignedIdentityRequest = new CreateIdentitySignedRequest(Request: Request, Signature: Convert.ToBase64String(signature));
            //Console.WriteLine(JsonSerializer.Serialize(SignedIdentityRequest));
            var response = clientIdentity.PostAsJsonAsync("CreateIdentity", SignedIdentityRequest).Result;

            if (response.IsSuccessStatusCode)
            {
                //Console.WriteLine(response.Content);
                string jsonString = response.Content.ReadAsStringAsync().Result;
                CreateIdentityResponse resp = JsonConvert.DeserializeObject<CreateIdentityResponse>(jsonString);

                AccountManager.userID = resp.TaggedUsername;
                KeyManager.publicKey = (Convert.FromBase64String(resp.PublicKey.Modulus), Convert.FromBase64String(resp.PublicKey.Exponent));

                //if (Convert.ToBase64String(publicKey.Item1).Equals(Convert.ToBase64String(KeyManager.publicKey.Item1)) && Convert.ToBase64String(publicKey.Item2).Equals(Convert.ToBase64String(KeyManager.publicKey.Item2)))
                //    Console.WriteLine("[DEBUG]: Public key matches");
                //else
                //    Console.WriteLine("[DEBUG]: Public key does not match");

                Console.WriteLine("[INFO]: You have been assigned to user ID: " + AccountManager.userID + " (Please use this user ID for future login)");
                return true;
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }
        }

        public static (byte[], byte[]) GetIdentity(string userID)
        {
            try
            {
                string taggedUsername = Convert.ToBase64String(Encoding.UTF8.GetBytes(userID));
                string webAddress = "GetIdentity?taggedUsername=" + userID;
                string fixedUri = Regex.Replace(webAddress, "#", "%23");

                var response = clientIdentity.GetAsync(fixedUri).Result;
                string jsonString = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    RSAPubKey resp = JsonConvert.DeserializeObject<RSAPubKey>(jsonString);
                    //Console.WriteLine("Modulus: " + resp.Modulus);
                    //Console.WriteLine("Exponent: " + resp.Exponent);
                    return (Convert.FromBase64String(resp.Modulus), Convert.FromBase64String(resp.Exponent));
                }
                else
                {
                    Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                    return (null, null);
                }
                
            }
            catch (Exception ex)
            {
                //Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return (null, null);
            }
        }

        public static bool UploadFileToCloud(string encryptedFile, byte[] encryptedKey, string taggedUsername)
        {
            //taggedUsername = Regex.Replace(taggedUsername, "#", "%23");
            SubmitFileRequest req = new SubmitFileRequest(EncryptedFile: encryptedFile, EncryptedKey: Convert.ToBase64String(encryptedKey), TaggedUsername: taggedUsername);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(req));

            SubmitFileSignedRequest signedReq = new SubmitFileSignedRequest(Request: req, Signature: Convert.ToBase64String(signature));
            //Console.WriteLine(JsonSerializer.Serialize(signedReq));
            //Console.WriteLine(JsonConvert.SerializeObject(signedReq));
            var response = clientFileSharer.PostAsJsonAsync("", signedReq).Result;

            //Console.WriteLine(Convert.ToBase64String(KeyManager.RSAalg.ExportParameters(false).Modulus));
            //Console.WriteLine(Convert.ToBase64String(KeyManager.RSAalg.ExportParameters(false).Exponent));

            if (response.IsSuccessStatusCode)
            {
                string jsonString = response.Content.ReadAsStringAsync().Result;
                SubmitFileResponse resp = JsonConvert.DeserializeObject<SubmitFileResponse>(jsonString);

                string fileURL = resp.URL;

                Console.WriteLine("[INFO]: File successfully added to cloud with URL: " + fileURL);

                return true;
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }

        }

        public static (string,string) DownloadFileFromCloud(string fileurl, string userID)
        {
            GetFileRequest request = new GetFileRequest(Url: fileurl, TaggedUsername: userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(request));

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["url"] = fileurl;
            query["taggedUsername"] = userID;
            query["signature"] = Convert.ToBase64String(signature);
            string queryString = query.ToString();

            //string addr = string.Format("file?url={0}&taggedUsername={1}&signature={2}", fileurl, userID, Convert.ToBase64String(signature));

            string addr = string.Format("file?" + queryString);

            var response = clientFileSharer.GetAsync(addr).Result;
            string jsonString = response.Content.ReadAsStringAsync().Result;
            //Console.WriteLine(JsonSerializer.Serialize(response));

            if (response.IsSuccessStatusCode)
            {
                GetFileResponse resp = JsonConvert.DeserializeObject<GetFileResponse>(jsonString);
                return (resp.EncryptedFile, resp.EncryptedKey);
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return (null, null);
            }

                
        }

        public static bool ShareFile(string url, UsernameKeyPair[] encryptedSharedKeys, string userID)
        {

            SharingFileRequest req = new SharingFileRequest(URL: url, TaggedUsernamesToShareWith: encryptedSharedKeys, CallerTaggedUsername: userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(req));

            SharingFileSignedRequest signedRequest = new SharingFileSignedRequest(Request: req, Signature: Convert.ToBase64String(signature));

            //Console.WriteLine(JsonSerializer.Serialize(signedRequest));
            var response = clientFileSharer.PostAsJsonAsync("share", signedRequest).Result;

            if (response.IsSuccessStatusCode)
            {
                string jsonString = response.Content.ReadAsStringAsync().Result;
                SharingFileResponse resp = JsonConvert.DeserializeObject<SharingFileResponse>(jsonString);

                string fileURL = resp.URL;
                IEnumerable<string> userIDList = resp.TaggedUsernames;

                Console.Write("[INFO]: File url: " + fileURL + " has been shared with the following userIDs:");
                foreach (string sharedUserID in userIDList)
                    Console.Write(sharedUserID + " ");
                Console.Write("\n");
                return true;
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }

            return true;
        }

        public static bool DeleteFile(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "file");

            DeleteFileRequest req = new DeleteFileRequest(URL: url, TaggedUsername: AccountManager.userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(req));
            DeleteFileSignedRequest signedRequest = new DeleteFileSignedRequest(Request: req, Signature: Convert.ToBase64String(signature));
            request.Content = new StringContent(JsonConvert.SerializeObject(signedRequest), Encoding.UTF8, "application/json");

            //Console.WriteLine(request.Content);
            //await this.client.SendAsync(request);
            var response = clientFileSharer.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }

        }

        public static bool UnshareFile(string fileurl, string[] userIDToRemove)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "unshare");

            UnsharingFileRequest req = new UnsharingFileRequest(URL: fileurl, TaggedUsernamesToRemove: userIDToRemove, CallerTaggedUsername: AccountManager.userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(req));
            UnsharingFileSignedRequest signedRequest = new UnsharingFileSignedRequest(Request: req, Signature: Convert.ToBase64String(signature));
            request.Content = new StringContent(JsonConvert.SerializeObject(signedRequest), Encoding.UTF8, "application/json");

            //Console.WriteLine(JsonConvert.SerializeObject(signedRequest));
            var response = clientFileSharer.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                return true;
                //Console.WriteLine(response);
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }

        }


        public static bool UpdateFile(string url, string encryptedfile)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "file");

            UpdateFileRequest req = new UpdateFileRequest(URL: url, EncryptedFile: encryptedfile, TaggedUsername: AccountManager.userID);
            byte[] signature = KeyManager.Sign(JsonSerializer.Serialize(req));
            UpdateFileSignedRequest signedReq = new UpdateFileSignedRequest(Request: req, Signature: Convert.ToBase64String(signature));
            request.Content = new StringContent(JsonConvert.SerializeObject(signedReq), Encoding.UTF8, "application/json");

            var response = clientFileSharer.SendAsync(request).Result;


            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("[INFO]: File url: " + url + "updated in database");
                return true;
                
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }

        }

        public static bool GetLogs(string fileurl, string type, string count, string userID)
        {

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["url"] = fileurl;
            query["taggedUsername"] = userID;
            query["type"] = type;
            query["count"] = count;
            string queryString = query.ToString();

            string addr = string.Format("logs?" + queryString);

            var response = clientFileSharer.GetAsync(addr).Result;
            string jsonString = response.Content.ReadAsStringAsync().Result;
            //Console.WriteLine(JsonSerializer.Serialize(jsonString));

            if (response.IsSuccessStatusCode)
            {
                AuditLogModel[] resp = JsonConvert.DeserializeObject<AuditLogModel[]>(jsonString);

                string[] columnNames = { "FileURL", "UserID Accessed", "Log Type", "Timestamp" };
                var table = new ConsoleTable(columnNames);

                foreach (AuditLogModel log in resp)
                {
                    string[] logPrint = { log.url, log.caller, log.type, log.timestamp.ToString()};
                    table.AddRow(logPrint);
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                table.Write(Format.Default);
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.WriteLine("[ERROR]: Response from server: " + response.ReasonPhrase);
                return false;
            }


        }


    }


    
}
