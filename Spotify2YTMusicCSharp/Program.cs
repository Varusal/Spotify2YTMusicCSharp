using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;

// Add "selection" menu
// Add custom client_secret.json file picker
// Add custom YT playlist name
// Add custom spotify export file picker

#region Spotify
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

await foreach (var item in spotiClient.Paginate(playlist))
{
    if (item.Track.Type == ItemType.Track)
    {
        FullTrack track = (FullTrack)item.Track;
        string artistTack = track.Artists[0].Name + " - " + track.Name;
        Console.WriteLine("{0}", artistTack);
        tracksList.Add(artistTack);
    }
}

StreamWriter writer = new StreamWriter(filePath);
foreach (string line in tracksList)
{
    writer.WriteLine(line);
}

writer.Close();
#endregion

#region Youtube
int limitCounter = 0;

UserCredential ytCreds;
FileStream ytCredsStream = new FileStream(@"C:\Temp\client_secret.json", FileMode.Open, FileAccess.Read);
ytCreds = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(ytCredsStream).Secrets, new[] { YouTubeService.Scope.Youtube }, "user", CancellationToken.None, new FileDataStore(ytCredsStream.GetType().ToString()));
limitCounter += 1;

var youtubeService = new YouTubeService(new BaseClientService.Initializer()
{
    HttpClientInitializer = ytCreds,
    ApplicationName = ytCredsStream.GetType().ToString()
});
limitCounter += 1;

// Create Playlist
var ytPlaylist = new Playlist();
ytPlaylist.Snippet = new PlaylistSnippet();
ytPlaylist.Snippet.Title = "Ultimate";
ytPlaylist.Status = new PlaylistStatus();
ytPlaylist.Status.PrivacyStatus = "private";
ytPlaylist = await youtubeService.Playlists.Insert(ytPlaylist, "snippet,status").ExecuteAsync();

limitCounter += 50;

// Search for keywords based on Spotify output
var tracks = File.ReadAllLines(@"C:\Temp\SpotifyExport.txt");

foreach (var line in tracks)
{
    Console.WriteLine("Searching for track: {0}", line);
    var searchRequest = youtubeService.Search.List("snippet");
    searchRequest.Q = line;
    searchRequest.MaxResults = 1;

    var searchRespone = await searchRequest.ExecuteAsync();
    limitCounter += 100;
    if (searchRespone.Items[0].Id.Kind == "youtube#video")
    {
        var playlistItem = new PlaylistItem();
        playlistItem.Snippet = new PlaylistItemSnippet();
        playlistItem.Snippet.PlaylistId = ytPlaylist.Id;
        playlistItem.Snippet.ResourceId = new ResourceId();
        playlistItem.Snippet.ResourceId.Kind = "youtube#video";
        playlistItem.Snippet.ResourceId.VideoId = searchRespone.Items[0].Id.VideoId;
        playlistItem = await youtubeService.PlaylistItems.Insert(playlistItem, "snippet").ExecuteAsync();
        Console.WriteLine("Added {0} to playlist", line);
        limitCounter += 50;
    }
    else
    {
        Console.WriteLine("Could not find track: {0}", line);
    }

    if (limitCounter >= 10000)
    {
        Console.WriteLine("!!!Quota reached!!!");
        Console.ReadKey();
        Environment.Exit(0);
    }
}
Console.ReadKey();
#endregion