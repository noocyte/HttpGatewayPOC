namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public class IdentityModel
    {
        public int Id { get; private set; }
        public string OrganizationPrefix { get; private set; }
        public string Username { get; private set; }
        public string EntityId { get; private set; }
        public int CurrentOrganizationId { get; private set; }
        public string OriginatingTenantGuid { get; private set; }
        public int OriginatingTenantId { get; private set; }
        public string Language { get; private set; }
        public static IdentityModel Empty => Create("", "", "");

        public static IdentityModel Create(
            string prefix,
            string username,
            string entityId,
            string language = "en-us")
        {
            return new IdentityModel
            {
                OrganizationPrefix = prefix,
                EntityId = entityId,
                Username = username,
                Language = language
            };
        }
    }
}