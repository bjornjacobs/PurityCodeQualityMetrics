using System.IO;
using Newtonsoft.Json;

namespace CodeQualityAnalyzer
{
    internal static class SecretService
    {
        public static TfsSecrets GetTfsSecrets()
        {
            var secrets = GetSecrets();

            return secrets.TfsSecrets;
        }

        public static GithubSecrets GetGithubSecrets()
        {
            var secrets = GetSecrets();

            return secrets.GithubSecrets;
        }

        private static Secrets GetSecrets()
        {
            var baseDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            var location = Path.Combine(baseDir, "secrets.json");
            var json = File.ReadAllText(location);

            return JsonConvert.DeserializeObject<Secrets>(json);

        }

        private class Secrets
        {
            [JsonProperty("TFS")]
            public TfsSecrets TfsSecrets;

            [JsonProperty("GITHUB")]
            public GithubSecrets GithubSecrets;
        }

        public class TfsSecrets
        {
            [JsonProperty("BASE_URL")]
            public string BaseUrl;

            [JsonProperty("ACCESS_TOKEN")]
            public string AccessToken;

            [JsonProperty("USERNAME")]
            public string UserName;

            [JsonProperty("TEAM_ID")]
            public string TeamId;
        }

        public class GithubSecrets
        {
            [JsonProperty("ACCESS_TOKEN")]
            public string AccessToken;
        }
    }
}
