using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Willow.Tests.Infrastructure
{
    public enum RestrictionType
    {
        Whitelist,
        Blacklist
    }

    public class RestrictionControllerFeatureProvider : ControllerFeatureProvider
    {
        private readonly RestrictionType _type;
        private readonly IEnumerable<TypeInfo> _controllers;

        public RestrictionControllerFeatureProvider(RestrictionType type, IEnumerable<Type> controllers)
        {
            _type = type;
            _controllers = controllers.Select(c => c.GetTypeInfo());
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            if (_type == RestrictionType.Whitelist)
            {
                if (!_controllers.Contains(typeInfo))
                {
                    return false;
                }
            }
            else if (_type == RestrictionType.Blacklist)
            {
                if (_controllers.Contains(typeInfo))
                {
                    return false;
                }
            }
            return base.IsController(typeInfo);
        }

        public bool InjectInto(IServiceCollection services)
        {
            var managerService = services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationPartManager));
            if (managerService != null)
            {
                var providers = ((ApplicationPartManager)managerService.ImplementationInstance).FeatureProviders;
                var provider = providers.FirstOrDefault(p => p.GetType() == typeof(ControllerFeatureProvider));
                if (provider != null)
                {
                    providers.Remove(provider);
                    providers.Add(this);
                    return true;
                }
            }
            return false;
        }
    }
}