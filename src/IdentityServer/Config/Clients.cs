namespace IdentityServer.Config
{
    using IdentityServer3.Core.Models;
    using System.Collections.Generic;

    public static class Clients
    {
        public static IEnumerable<Client> Get()
        {
            return new[]
            {
                new Client
                {
                    Enabled = true,
                    ClientName = "JS Client",
                    ClientId = "mvc",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("mvc".Sha512())
                    },
                    Flow = Flows.AuthorizationCode,

                    RedirectUris = new List<string>
                    {
                        "http://localhost:54428/openid-callback"
                    },

                    AllowAccessToAllScopes = true,
                    AccessTokenLifetime = 60
                }
            };
        }
    }
}