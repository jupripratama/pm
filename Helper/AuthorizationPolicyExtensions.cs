using Microsoft.AspNetCore.Authorization;

namespace Pm.Helper
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddCustomAuthorizationPolicies(this AuthorizationOptions options)
        {
            // Permission policies
            options.AddPolicy("CanViewPermissions", policy =>
                policy.RequireClaim("Permission", "permission.view"));

            options.AddPolicy("CanEditPermission", policy =>
                policy.RequireClaim("Permission", "permission.edit"));

            // Role policies
            options.AddPolicy("CanViewRoles", policy =>
                policy.RequireClaim("Permission", "role.view-any"));

            options.AddPolicy("CanViewDetailRoles", policy =>
                policy.RequireClaim("Permission", "role.view"));

            options.AddPolicy("CanCreateRoles", policy =>
                policy.RequireClaim("Permission", "role.create"));

            options.AddPolicy("CanUpdateRoles", policy =>
                policy.RequireClaim("Permission", "role.update"));

            options.AddPolicy("CanDeleteRoles", policy =>
                policy.RequireClaim("Permission", "role.delete"));

            // User policies
            options.AddPolicy("CanViewUsers", policy =>
                policy.RequireClaim("Permission", "user.view-any"));

            options.AddPolicy("CanViewDetailUsers", policy =>
                policy.RequireClaim("Permission", "user.view"));

            options.AddPolicy("CanCreateUsers", policy =>
                policy.RequireClaim("Permission", "user.create"));

            options.AddPolicy("CanUpdateUsers", policy =>
                policy.RequireClaim("Permission", "user.update"));

            options.AddPolicy("CanDeleteUsers", policy =>
                policy.RequireClaim("Permission", "user.delete"));
        }
    }
}