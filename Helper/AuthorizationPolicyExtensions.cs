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

            options.AddPolicy("CanEditPermissions", policy =>
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

            // Call Record policies
            options.AddPolicy("CanImportCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.import"));
            options.AddPolicy("CanViewCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.view-any"));
            options.AddPolicy("CanViewDetailCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.view"));
            options.AddPolicy("CanExportCallRecordsExcel", policy =>
                policy.RequireClaim("Permission", "callrecord.export-excel"));
            options.AddPolicy("CanExportCallRecordsCsv", policy =>
                policy.RequireClaim("Permission", "callrecord.export-csv"));
            options.AddPolicy("CanDeleteCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.delete"));
            options.AddPolicy("CanDeleteAllData", policy =>
                policy.RequireClaim("Permission", "delete.all-data"));


            // Setting
            options.AddPolicy("CanViewSettings", policy =>
                policy.RequireClaim("Permission", "system.settings.view"));

            options.AddPolicy("CanEditSettings", policy =>
                policy.RequireClaim("Permission", "system.settings.edit"));

            options.AddPolicy("CanViewAuditLog", policy =>
                policy.RequireClaim("Permission", "system.audit.view"));


            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireClaim("Permission", "manage.user.create", "manage.user.update", "manage.user.delete"));

            options.AddPolicy("CanManageRoles", policy =>
                policy.RequireClaim("Permission", "manage.role.create", "manage.role.update", "manage.role.delete"));


            options.AddPolicy("InspeksiTemuanKpcView", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.view"));

            options.AddPolicy("InspeksiTemuanKpcCreate", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.create"));

            options.AddPolicy("InspeksiTemuanDelete", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.delete"));

            options.AddPolicy("InspeksiTemuanRestore", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.restore"));
        }


    }
}