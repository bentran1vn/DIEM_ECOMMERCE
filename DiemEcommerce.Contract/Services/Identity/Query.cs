using System.Text;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;

namespace DiemEcommerce.Contract.Services.Identity;

public static class Query
{
    public record Login(string EmailOrUserName, string Password) : IQuery<Response.Authenticated>, ICacheable
    {
        public bool BypassCache => true;
        public string CacheKey {
            get
            {
                var builder = new StringBuilder();
                builder.Append($"{nameof(Login)}");
                builder.Append($"-UserAccount:{EmailOrUserName}");
                return builder.ToString();
            }
        }
        public int SlidingExpirationInMinutes => 10;
        public int AbsoluteExpirationInMinutes => 15;
    }

    public record Token(string AccessToken, string RefreshToken) : IQuery<Response.Authenticated>;

    public record GetMe(Guid UserId) : IQuery<Response.GetMe>;
    
    public record GetUsers(string? SearchTerm, int PageIndex, int PageSize) : IQuery<PagedResult<Response.GetMe>>;
}