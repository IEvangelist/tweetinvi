using Tweetinvi.Models;

namespace xUnitinvi.EndToEnd
{
    public static class EndToEndTestConfig
    {
        public static readonly bool ShouldRunEndToEndTests = false;
        public static readonly bool ShouldRunRateLimitHungryTests = false;
        public static readonly bool ShouldRunAccountActivityStreamTests = false; // very slow tests
        public static readonly bool ShouldRunAuthTests = false;

        public static readonly IntegrationTestAccount TweetinviApi = new IntegrationTestAccount
        {
            Credentials = new TwitterCredentials("", "", "", "")
            {
                BearerToken = ""
            },
            AccountId = "tweetinviapi",
            UserId = 1577389800
        };

        public static readonly IntegrationTestAccount TweetinviTest = new IntegrationTestAccount
        {
            Credentials = new TwitterCredentials("", "", "", ""),
            AccountId = "tweetinvitest",
            UserId = 1693649419
        };

        public static readonly IntegrationTestAccount ProtectedUser = new IntegrationTestAccount
        {
            Credentials = new TwitterCredentials("", "", "", ""),
            AccountId = "artwolkt",
            UserId = 42290825
        };

        public static readonly IntegrationTestAccount ProtectedUserAuthenticatedToTweetinviApi = new IntegrationTestAccount
        {
            Credentials = new TwitterCredentials("", "", "", ""),
            AccountId = "artwolkt",
            UserId = 42290825
        };
    }

    public class IntegrationTestAccount
    {
        public ITwitterCredentials Credentials { get; set; }
        public string AccountId { get; set; }
        public long UserId { get; set; }

        public static implicit operator string (IntegrationTestAccount account)
        {
            return account.AccountId;
        }
    }
}
