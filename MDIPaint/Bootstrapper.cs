using System;
using MDIPaint.Models;
using Microsoft.Extensions.Configuration;
using PluginInterface;
using Splat;

namespace MDIPaint;

public class Bootstrapper
{
    public static void Register(IMutableDependencyResolver services, IConfiguration configuration)
    {
        services.RegisterConstant(configuration);
        
        services.RegisterConstant(configuration.GetSection("Plugins").Get<Plugins>());
    }
}