using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly GitHubApiSettings _settings;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly ConcurrentDictionary<string, List<int>> _userBookmarks = new();


        public GitHubController(
            HttpClient httpClient,
            IOptions<GitHubApiSettings> settings,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _settings = (GitHubApiSettings?)settings.Value;
            _httpContextAccessor = httpContextAccessor;
            //_userBookmarks = new Dictionary<string, List<string>>();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchRepositories(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            //in our case the query was build in client and just passed to the API
            //API URI is stored in settings
            var apiUrl = $"{_settings.SearchUrl}{query}";
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubSearch");

            var response = await _httpClient.GetFromJsonAsync<JsonElement>(apiUrl);
            return Ok(response);
        }

        [HttpPost("bookmark")]
        public IActionResult BookmarkRepository([FromBody] string repositoryId)
        {
            if (string.IsNullOrEmpty(repositoryId))
            {
                return BadRequest("Repository ID cannot be null or empty.");
            }

            // Retrieve username from the JWT or session
            // Make sure the user is authenticated
            string username = HttpContext.User.Identity?.Name; 
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            // Ensure the user has a bookmark list
            _userBookmarks.TryAdd(username, new List<int>());
            var userBookmarks = _userBookmarks[username];

            int _repositoryId = int.Parse(repositoryId);
            // Check if the repository already exists in the user's bookmarks
            if (userBookmarks.Contains(_repositoryId))
            {
                // Remove the repository if it already exists
                userBookmarks.Remove(_repositoryId);
                return Ok(false); // Return false to indicate it was removed
            }
            else
            {
                // Add the repository if it doesn't exist
                userBookmarks.Add(_repositoryId);
                return Ok(true); // Return true to indicate it was added
            }
        }

        [HttpGet("bookmarks")]
        public IActionResult GetBookmarks()
        {
            // Retrieve the username from the session or token
            string username = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User is not authenticated.");
            }

            // Check if the user has any bookmarks
            if (!_userBookmarks.ContainsKey(username))
            {
                return Ok(new List<int>()); // Return an empty list if no bookmarks are found
            }

            // Return the user's bookmarks
            var bookmarks = _userBookmarks[username];
            return Ok(bookmarks);
        }
    }
}
