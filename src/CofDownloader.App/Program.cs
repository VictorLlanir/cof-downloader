using Newtonsoft.Json;

namespace CofDownloader.App;

public class Program
{
    private const string COF_API_LOGIN = "https://api.seminariodefilosofia.org/v1/accounts/signin";
    private const string COF_FILES_URL = "https://api.seminariodefilosofia.org/v1/courses/sources/1?limit=320&offset=0&exclude=audios%2Cvideos&page=1";
    public static void Main(string[] args)
    {
        MainAsync(args).Wait();
    }

    private static async Task MainAsync(string[] args)
    {
        using var client = new HttpClient();

        var payload = new
        {
            email = "victortrevisan1997@gmail.com",
            password = "filosofiaconcreta"
        };

        var loginMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(COF_API_LOGIN),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(payload))
        };

        var loginResponse = await client.SendAsync(loginMessage);
        var loginContent = JsonConvert.DeserializeObject<LoginResult>(await loginResponse.Content.ReadAsStringAsync());

        var getFilesMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(COF_FILES_URL),
            Method = HttpMethod.Get
        };

        var filesResponse = await client.SendAsync(getFilesMessage);
        var filesContent = JsonConvert.DeserializeObject<PaginatedResult<FileResult>>(await filesResponse.Content.ReadAsStringAsync());
    }
}

public class LoginResult
{
    public string Email { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Refresh { get; set; }
    public string Token { get; set; }
}

public class PaginatedResult<T>
{
    public int Count { get; set; }
    public string Next { get; set; }
    public string Previous { get; set; }
    public IEnumerable<T> Results { get; set; }
}

public class FileResult
{
    public string Name { get; set; }
    public string File { get; set; }
    public string Category { get; set; }
}
