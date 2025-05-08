using System.Net.Http.Json;



namespace MotorTownDiscordBot.MotorTown
{
    public class BanOptions
    {
        public string? reason { get; set; }

        public int? hours { get; set; }
    }

    public class WebAPI
    {
        private int _port;
        private string? _password;
        private HttpClient _client;

        public WebAPI(int port, string? password)
        {
            _port = port;
            _password = password;
            _client = new HttpClient(new PasswordHandler(_password)) { BaseAddress = new Uri($"http://localhost:{_port}/") };
        }

        public async Task<int> GetPlayerCount()
        {
            HttpResponseMessage response = await _client.GetAsync("/player/count");
            var result = await GetResult<PlayerCountData>(response);

            return result.data.num_players;
        }

        public async Task<PlayerListData[]?> GetPlayerList()
        {
            HttpResponseMessage response = await _client.GetAsync("/player/list");
            var result = await GetResult<Dictionary<string, PlayerListData>>(response);

            return result?.data?.Values.ToArray();
        }

        public async Task<PlayerListData[]?> GetPlayerBanList()
        {
            HttpResponseMessage response = await _client.GetAsync("/player/banlist");
            var result = await GetResult<Dictionary<string, PlayerListData>>(response);

            return result.data.Values.ToArray();
        }


        public async Task<bool> PlayerKick(string player_id)
        {
            HttpResponseMessage response = await _client.PostAsync($"/player/kick?unique_id={player_id}", null);
            var result = await GetResult<object>(response);

            return result.succeeded;
        }

        public async Task<bool> PlayerBan(string player_id, BanOptions? options = null)
        {
            var queries = new List<string>();
            queries.Add($"unique_id={player_id}");

            if (options?.hours != null && options.hours > 0)
            {
                queries.Add($"hours={options.hours}");
            }

            if (options?.reason != null && options.reason.Length > 0)
            {
                queries.Add($"reason={Uri.EscapeDataString(options.reason)}");
            }

            string QS = String.Join('&', queries.ToArray());


            HttpResponseMessage response = await _client.PostAsync($"/player/ban?{QS}", null);
            var result = await GetResult<object>(response);

            return result.succeeded;
        }

        public async Task<bool> PlayerUnban(string player_id)
        {
            HttpResponseMessage response = await _client.PostAsync($"/player/unban?unique_id={player_id}", null);
            var result = await GetResult<object>(response);

            return result.succeeded;
        }

        public async Task<bool> SendMessage(string message)
        {
            HttpResponseMessage response = await _client.PostAsync($"/chat?message={Uri.EscapeDataString(message)}", null);
            var result = await GetResult<object>(response);

            return result.succeeded;
        }

        private async Task<Response<T>> GetResult<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync();
            var result = await response.Content.ReadFromJsonAsync<Response<T>>();

            if (result == null)
            {
                throw new Exception("Request failed");
            }

            if (result.succeeded != true)
            {
                throw new Exception(result.message);
            }

            return result;
        }
    }
}

public class Response<T>
{
    public int code { get; set; }

    public string message { get; set; }

    public bool succeeded { get; set; }

    public T data { get; set; }
}

public class PlayerCountData
{
    public int num_players { get; set; }
}

public class PlayerListData
{
    public required string name { get; set; }

    public required string unique_id { get; set; }
}
