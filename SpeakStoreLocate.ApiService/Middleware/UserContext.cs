namespace SpeakStoreLocate.ApiService.Middleware;

public interface IUserContext
{
    string UserId { get; set; }
}

public class UserContext : IUserContext
{
    public string UserId { get; set; } = string.Empty;
}

