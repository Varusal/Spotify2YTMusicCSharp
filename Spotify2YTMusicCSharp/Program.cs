using SpotifyAPI.Web;

List<string> tracksList = new List<string>();

string filePath = @"C:\Temp\tracks.txt";

string clientID = "e6a7a5cb1f5542c984f288b3684407e4";
Console.Write("Enter client secret: ");
string clientSecret = Console.ReadLine();

var config = SpotifyClientConfig.CreateDefault();
var request = new ClientCredentialsRequest(clientID, clientSecret);
var response = await new OAuthClient(config).RequestToken(request);

var spotiClient = new SpotifyClient(config.WithToken(response.AccessToken));

var playlist = await spotiClient.Playlists.GetItems("27zKKPzY1TIJOVvwaORqwY");

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
