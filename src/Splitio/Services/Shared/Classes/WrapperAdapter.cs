﻿#if NET_LATEST
using Microsoft.Extensions.Logging;
#endif
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class WrapperAdapter : IWrapperAdapter
    {
        private static readonly object _instanceLock = new object();

        private static IWrapperAdapter _instance;
        private ISplitLogger _customLogger;

        public static IWrapperAdapter Instance()
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new WrapperAdapter();
                    }
                }
            }

            return _instance;
        }

        public SdkMetadata BuildSdkMetadata(ConfigurationOptions config, ISplitLogger log)
        {
            var metadata = new SdkMetadata();
            var ipAddressesEnabled = config.IPAddressesEnabled ?? true;

#if NET_LATEST
            metadata.Version = ".NET_CORE-" + SplitSdkVersion(log);
#else
            metadata.Version = ".NET-" + SplitSdkVersion(log);
#endif
            metadata.MachineName = GetSdkMachineName(config, ipAddressesEnabled, log);
            metadata.MachineIP = GetSdkMachineIP(config, ipAddressesEnabled, log);

            return metadata;
        }

        public Task<Task> WhenAnyAsync(params Task[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        private static string SplitSdkVersion(ISplitLogger log)
        {
            try
            {
#if NET_LATEST
                return typeof(Split).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
#else
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
#endif
            }
            catch (Exception ex)
            {
                log.Warn("Exception retrieving sdk version", ex);
            }

            return Constants.Gral.SdkVersion;
        }

        public void SetCustomerLogger(ISplitLogger splitLogger)
        {
            _customLogger = splitLogger;
        }

        public ISplitLogger GetLogger(Type type)
        {
            if (_customLogger != null)
            {
                return _customLogger;
            }

#if NET_LATEST
            if (SplitLoggerFactoryExtensions.LoggerFactoryHasValue)
                return new MicrosoftExtensionsLogging(type);
            else
                return new NoopLogging();
#else
            return new CommonLogging(type);
#endif
        }

        public ISplitLogger GetLogger(string type)
        {
            if (_customLogger != null)
            {
                return _customLogger;
            }

#if NET_LATEST
            if (SplitLoggerFactoryExtensions.LoggerFactoryHasValue)
                return new MicrosoftExtensionsLogging(type);
            else
                return new NoopLogging();
#else
            return new CommonLogging(type);
#endif
        }

        #region Private Methods
        private static string GetSdkMachineName(ConfigurationOptions config, bool ipAddressesEnabled, ISplitLogger log)
        {
            try
            {
                if (ipAddressesEnabled)
                {
                    return config.SdkMachineName ?? Environment.MachineName;
                }
                else if (config.CacheAdapterConfig?.Type == AdapterType.Redis)
                {
                    return Constants.Gral.NA;
                }
            }
            catch (Exception e)
            {
                log.Warn("Exception retrieving machine name.", e);
            }

            return Constants.Gral.Unknown;
        }

        private static string GetSdkMachineIP(ConfigurationOptions config, bool ipAddressesEnabled, ISplitLogger log)
        {
            if (ipAddressesEnabled)
            {
                try
                {
#if NET_LATEST
                    var hostAddressesTask = Dns.GetHostAddressesAsync(Environment.MachineName);
                    return config.SdkMachineIP ?? hostAddressesTask.Result.Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.IsIPv6LinkLocal == false).Last().ToString();
#else
                    return config.SdkMachineIP ?? Dns.GetHostAddresses(Environment.MachineName).Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.IsIPv6LinkLocal == false).Last().ToString();
#endif
                }
                catch (Exception e)
                {
                    log.Warn("Exception retrieving machine IP.", e);
                    return Constants.Gral.Unknown;
                }
            }
            else if (config.CacheAdapterConfig?.Type == AdapterType.Redis)
            {
                return Constants.Gral.NA;
            }

            return string.Empty;
        }
        #endregion
    }
}
