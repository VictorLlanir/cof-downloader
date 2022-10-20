using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using static System.Console;

namespace CofDownloader.App;

public class Program
{
    private const string COF_API_LOGIN = "https://api.seminariodefilosofia.org/v1/accounts/signin";
    private const string COF_FILES_URL = "https://api.seminariodefilosofia.org/v1/courses/sources/1?limit=320&offset=0&exclude=audios%2Cvideos&page=1";
    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: false);

        var configRoot = builder.Build();
        var configuration = configRoot.GetSection("ConnectionStrings").Get<Configuration>();

        MainAsync(args, configuration).Wait();
    }

    private static async Task MainAsync(string[] args, Configuration configuration)
    {
        using var client = new HttpClient();

        var payload = new
        {
            email = configuration.Email,
            password = configuration.Password
        };

        WriteLine("[REQUEST] Autenticando...\n");
        var loginMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(COF_API_LOGIN),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
        };

        loginMessage.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36");
        loginMessage.Headers.Add("origin", "https://app.seminariodefilosofia.org");
        loginMessage.Headers.Add("referer", "https://app.seminariodefilosofia.org/");
        loginMessage.Headers.Add("accept", "application/json, text/plain, */*");

        var loginResponse = await client.SendAsync(loginMessage);
        var loginContent = JsonConvert.DeserializeObject<LoginResult>(await loginResponse.Content.ReadAsStringAsync());

        WriteLine("[REQUEST] Buscando dados...\n");
        var getFilesMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(COF_FILES_URL),
            Method = HttpMethod.Get
        };

        getFilesMessage.Headers.Add("Authorization", $"JWT {loginContent.Token}");

        var filesResponse = await client.SendAsync(getFilesMessage);
        var filesContent = JsonConvert.DeserializeObject<PaginatedResult<FileResult>>(await filesResponse.Content.ReadAsStringAsync());
        if (filesContent != null)
        {
            foreach (var fileContent in filesContent.Results.Skip(307))
            {
                WriteLine($"[DOWNLOAD] Baixando {fileContent.Name} - {filesContent.Results.FindIndex(p => p.Name == fileContent.Name)} de {filesContent.Results.Count}");
                var downloadMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri(fileContent.File),
                    Method = HttpMethod.Get
                };

                var downloadResponse = await client.SendAsync(downloadMessage);
                var downloadContent = await downloadResponse.Content.ReadAsByteArrayAsync();

                await File.WriteAllBytesAsync(@$"D:\Arquivos_COF\{fileContent.Name.Replace("?", "").Replace("/", ".")}{GetFileExtension(fileContent.File)}", downloadContent);
            }
        }
    }

    private static string GetFileExtension(string file)
    {
        return $".{file.Split(".").Last()}";
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
    public List<T> Results { get; set; }
}

public class FileResult
{
    public string Name { get; set; }
    public string File { get; set; }
    public string Category { get; set; }
}

public class Configuration
{
    public string Email { get; set; }
    public string Password { get; set; }
}