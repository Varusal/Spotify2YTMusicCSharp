using SpotifyAPI.Web;

List<string> tracksList = new List<string>();
string filePath = string.Empty;
string fileExtension = string.Empty;
bool fileInputCheck = false;

do
{
    fileInputCheck = false;
    Console.Write("Enter path for list of exported tracks (*.txt): ");
    filePath = Console.ReadLine();

    if (filePath != string.Empty)
    {
        fileExtension = filePath.Substring(filePath.Length - 4);

        if (fileExtension != ".txt")
        {
            fileInputCheck = true;
            Console.Clear();
            Console.WriteLine("ERROR! Please enter a filepath with *.txt file extension!");
        }
        else if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Console.Clear();
            Console.WriteLine("Entered file path does not exist!");
            Console.Write("Create directory? (y/N): ");
            if (Console.ReadLine().ToLower() != "y")
            {
                Console.Clear();
                Console.WriteLine("Directory was NOT created!");
                fileInputCheck = true;
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
        }
    }
    else
    {
        Console.Clear();
        Console.WriteLine("ERROR! Do not enter nothing!");
        fileInputCheck = true;
    }
} while (fileInputCheck);

Console.Write("Enter client ID: ");
string clientID = Console.ReadLine();

Console.Write("Enter client secret: ");
string clientSecret = Console.ReadLine();

var config = SpotifyClientConfig.CreateDefault();
var request = new ClientCredentialsRequest(clientID, clientSecret);
var response = await new OAuthClient(config).RequestToken(request);

var spotiClient = new SpotifyClient(config.WithToken(response.AccessToken));

Console.Write("Enter playlist ID: ");
string playlistID = Console.ReadLine();

var playlist = await spotiClient.Playlists.GetItems(playlistID);

await foreach(var item in spotiClient.Paginate(playlist))
{
    if (item.Track.Type == ItemType.Track)
    {
        FullTrack track = (FullTrack)item.Track;
        string artistTack = track.Artists[0].Name + " - " + track.Name;
        Console.WriteLine("{0}",artistTack);
        tracksList.Add(artistTack);
    }
}

StreamWriter writer = new StreamWriter(filePath);
foreach(string line in tracksList)
{
    writer.WriteLine(line);
}

writer.Close();
