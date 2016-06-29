using EPiServer.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Security;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    public static class UserExtensions
    {
        public static IPrincipal GetUser() => PrincipalInfo.Current.Principal;

        public static IPrincipal GetUser(string username) => PrincipalInfo.CreatePrincipal(username);
                
        public static IEnumerable<string> GetRoles(this IPrincipal principle)
        {
            if (principle == null)
                throw new ArgumentNullException(nameof(principle));

            var userPrinciple = new PrincipalInfo(principle);
            var list = new List<string>(userPrinciple.RoleList);
            VirtualRoleRepository<VirtualRoleProviderBase> repository = VirtualRoleRepository<VirtualRoleProviderBase>.GetDefault();

            foreach (string name in repository.GetAllRoles())
            {
                VirtualRoleProviderBase virtualRoleProvider;

                if (repository.TryGetRole(name, out virtualRoleProvider) && virtualRoleProvider.IsInVirtualRole(userPrinciple.Principal, null))
                    list.Add(name);

            }

            if (Roles.Enabled)
                list.AddRange(Roles.GetRolesForUser(userPrinciple.Name));

            return list.Distinct();
        }
    }
}
