﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2016-05-15
// Last Modified:			2016-05-15
// 

using cloudscribe.Core.Models;
using cloudscribe.Core.Models.Geography;
using cloudscribe.Core.Storage.NoDb;
using Microsoft.Extensions.DependencyInjection;
using NoDb;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting // so it will show up in startup without a using
{
    public static class CoreNoDbStartup
    {
        public static async Task InitializeDataAsync(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var siteQueries = serviceScope.ServiceProvider.GetService<ISiteQueries>();
                var siteCommands = serviceScope.ServiceProvider.GetService<ISiteCommands>();
                var userQueries = serviceScope.ServiceProvider.GetService<IUserQueries>();
                var userCommands = serviceScope.ServiceProvider.GetService<IUserCommands>();
                var geoQueries = serviceScope.ServiceProvider.GetService<IGeoQueries>();
                var geoCommands = serviceScope.ServiceProvider.GetService<IGeoCommands>();
                var roleQueries = serviceScope.ServiceProvider.GetService<IBasicQueries<SiteRole>>();
                var projectResolver = serviceScope.ServiceProvider.GetService<IProjectResolver>();
                var userBasic = serviceScope.ServiceProvider.GetService<IBasicQueries<SiteUser>>();

                await EnsureData(
                    siteQueries,
                    siteCommands,
                    userQueries,
                    userCommands,
                    geoQueries,
                    geoCommands,
                    roleQueries,
                    userBasic,
                    projectResolver
                    );

            }
        }


        private static async Task EnsureData(
            ISiteQueries siteQueries,
            ISiteCommands siteCommands,
            IUserQueries userQueries,
            IUserCommands userCommands,
            IGeoQueries geoQueries,
            IGeoCommands geoCommands,
            IBasicQueries<SiteRole> roleQueries,
            IBasicQueries<SiteUser> userBasic,
            IProjectResolver projectResolver
            )
        {
            
            int count = await geoQueries.GetCountryCount();
            if (count == 0)
            {
                foreach (GeoCountry c in InitialData.BuildCountryList())
                {
                    await geoCommands.Add(c);
                }

                foreach (GeoZone c in InitialData.BuildStateList())
                {
                    await geoCommands.Add(c);
                }
            }
            
            count = await geoQueries.GetLanguageCount();
            if (count == 0)
            {
                foreach (Language c in InitialData.BuildLanguageList())
                {
                    await geoCommands.Add(c);
                }
            }

            var all = await geoQueries.GetAllCurrencies();
            count = all.Count;
            if (count == 0)
            {
                foreach (Currency c in InitialData.BuildCurrencyList())
                {
                    await geoCommands.Add(c);
                }
            }
            
            count = await siteQueries.GetCount();
            SiteSettings newSite = null;
            if (count == 0)
            {
                // create first site
                newSite = InitialData.BuildInitialSite();
                await siteCommands.Create(newSite);
            }

            // ensure roles
            var projectId = await projectResolver.ResolveProjectId();
            
            count = await roleQueries.GetCountAsync(projectId);
            if (count == 0)
            {
                var adminRole = InitialData.BuildAdminRole();
                adminRole.SiteId = newSite.Id;
                await userCommands.CreateRole(adminRole);
                
                var roleAdminRole = InitialData.BuildRoleAdminRole();
                roleAdminRole.SiteId = newSite.Id;
                await userCommands.CreateRole(roleAdminRole);
                
                var contentAdminRole = InitialData.BuildContentAdminsRole();
                contentAdminRole.SiteId = newSite.Id;
                await userCommands.CreateRole(contentAdminRole);

                var authenticatedUserRole = InitialData.BuildAuthenticatedRole();
                authenticatedUserRole.SiteId = newSite.Id;
                await userCommands.CreateRole(authenticatedUserRole);
                
            }

            // ensure admin user
            count = await userBasic.GetCountAsync(projectId);

            if (count == 0)
            {
                var role = await userQueries.FetchRole(newSite.Id, "Administrators");
                
                if (role != null)
                {
                    var adminUser = InitialData.BuildInitialAdmin();
                    adminUser.SiteId = newSite.Id;
                    await userCommands.Create(adminUser);
                    
                    await userCommands.AddUserToRole(role.Id, adminUser.Id);

                    role = await userQueries.FetchRole(newSite.Id, "Authenticated Users");
                    
                    if (role != null)
                    {
                        await userCommands.AddUserToRole(role.Id, adminUser.Id);
                    }
                }
                
            }

        }

    }
}
