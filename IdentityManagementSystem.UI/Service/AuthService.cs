namespace IdentityManagementSystem.Service
{
    public class AuthService
    {
        public HttpClient HttpClient { get; }

        public AuthService(HttpClient client)
        {
            HttpClient = client ?? throw new ArgumentNullException(nameof(client));
            HttpClient.BaseAddress = new Uri("https://localhost:5066/api/");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var loginModel = new { Username = username, Password = password };
            var response = await HttpClient.PostAsJsonAsync("Auth/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public bool ValidateUserTest(string username, string password)
        {
            if (username == "admin" && password == "1234")
                return true;
            return false;
        }
    }
}