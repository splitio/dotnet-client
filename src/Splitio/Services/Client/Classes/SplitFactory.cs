using Splitio.Domain;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Reflection;

namespace Splitio.Services.Client.Classes
{
    public class SplitFactory : ISplitFactory
    { 
        private readonly IApiKeyValidator _apiKeyValidator;
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;
        private readonly string _apiKey;

        private ISplitClient _client;
        private ISplitManager _manager;
        private ConfigurationOptions _options;

        public SplitFactory(string apiKey, 
            ConfigurationOptions options = null)
        {
            _apiKey = apiKey;
            _options = options ?? new ConfigurationOptions();

            var wrapperAdapter = WrapperAdapter.Instance();
            wrapperAdapter.SetCustomerLogger(_options.Logger);
            _apiKeyValidator = new ApiKeyValidator();
            _factoryInstantiationsService = FactoryInstantiationsService.Instance();

            Client();
        }

        public ISplitClient Client()
        {
            if (_client == null)
            {
                BuildSplitClient();
            }

            return _client;
        }

        public ISplitManager Manager()
        {
            if (_client == null)
            {
                BuildSplitClient();
            }

            _manager = _client.GetSplitManager();

            return _manager;
        }

        private void BuildSplitClient()
        { 
            switch (_options.Mode)
            {
                case Mode.Standalone:
                    _apiKeyValidator.Validate(_apiKey);

                    if (_apiKey == "localhost")
                    {
                        _client = new LocalhostClient(_options);
                    }
                    else
                    {
                        _client = new SelfRefreshingClient(_apiKey, _options);
                    }
                    break;
                case Mode.Consumer:

                    try
                    {
                        RedisConfigurationValidator.Validate(_options.CacheAdapterConfig);
                        var redisAssembly = Assembly.Load(new AssemblyName("Splitio.Redis"));
                        var redisType = redisAssembly.GetType("Splitio.Redis.Services.Client.Classes.RedisClient");

                        _client = (ISplitClient)Activator.CreateInstance(redisType, new object[] { _options, _apiKey });
                    }
                    catch (ArgumentException ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Splitio.Redis package should be added as reference, to build split client in Redis Consumer mode.", e);
                    }
                    
                    break;
                case Mode.Producer:
                    throw new Exception("Unsupported mode.");
                default:
                    throw new Exception("Mode should be set to build split client.");
            }

            _factoryInstantiationsService.Increase(_apiKey);
        }        
    }
}
