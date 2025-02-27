using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class ApiSplitChangeFetcherTests
    {
        private readonly Mock<ISplitSdkApiClient> _apiClient;
        private readonly ISplitChangeFetcher _apiFetcher;

        public ApiSplitChangeFetcherTests()
        {
            _apiClient = new Mock<ISplitSdkApiClient>();

            _apiFetcher = new ApiSplitChangeFetcher(_apiClient.Object);
        }

        [TestMethod]
        [Description("Test a Json that changes its structure and is deserialized without exception. Contains: a field renamed, a field removed and a field added.")]
        public async Task ExecuteJsonDeserializeSuccessfulWithChangeInJsonFormat()
        {
            //Arrange            
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                             's':-1,
                             't':-1,
                             'd':[
        
                             ]
                          },
                          'ff':{
                             'd':[
                                {
                                   'trafficType':'user',
                                   'name':'Reset_Seed_UI',
                                   'seed':1552577712,
                                   'status':'ACTIVE',
                                   'defaultTreatment':'off',
                                   'changeNumber':1469827821322,
                                   'conditions':[
                                      {
                                         'matcherGroup':{
                                            'combiner':'AND',
                                            'matchers':[
                                               {
                                                  'keySelector':{
                                                     'trafficType':'user',
                                                     'attribute':null
                                                  },
                                                  'matcherType':'ALL_KEYS',
                                                  'negate':false
                                               }
                                            ]
                                         },
                                         'partitions':[
                                            {
                                               'treatment':'on',
                                               'size':100
                                            },
                                            {
                                               'treatment':'off',
                                               'size':0,
                                               'addedField':'test'
                                            }
                                         ]
                                      }
                                   ]
                                }
                             ],
                             's':1469817846929,
                             't':1469827821322
                          }
                        }");


            ApiSplitChangeFetcher apiSplitChangeFetcher = new ApiSplitChangeFetcher(_apiClient.Object);

            //Act
            var result = await apiSplitChangeFetcher.FetchAsync(new FetchOptions());

            //Assert
            Assert.IsTrue(result != null);
            Assert.IsTrue(result.FeatureFlags.Data.Count > 0);
        }

        [TestMethod]
        public async Task FetchSplitChangesSuccessfull()
        {
            //Arrange
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                            's':-1,
                            't':-1,
                            'd':[]
                          },
                          'ff':{
                            'd': [
                            {
                              'trafficTypeName': 'user',
                              'name': 'Test_1',
                              'seed': 673896442,
                              'status': 'ACTIVE',
                              'killed': false,
                              'defaultTreatment': 'off',
                              'changeNumber': 1470855828956,
                              'conditions': [
                                {
                                  'matcherGroup': {
                                    'combiner': 'AND',
                                    'matchers': [
                                      {
                                        'keySelector': {
                                          'trafficType': 'user',
                                          'attribute': null
                                        },
                                        'matcherType': 'ALL_KEYS',
                                        'negate': false,
                                        'userDefinedSegmentMatcherData': null,
                                        'whitelistMatcherData': null,
                                        'unaryNumericMatcherData': null,
                                        'betweenMatcherData': null
                                      }
                                    ]
                                  },
                                  'partitions': [
                                    {
                                      'treatment': 'on',
                                      'size': 0
                                    },
                                    {
                                      'treatment': 'off',
                                      'size': 100
                                    }
                                  ]
                                }
                              ]
                            }   
                          ],
                          's': -1,
                          't': 1470855828956
                          },
                        }");

            //Act
            var result = await _apiFetcher.FetchAsync(new FetchOptions());

            //Assert
            Assert.IsNotNull(result);
            var split = result.FeatureFlags.Data.First();
            Assert.AreEqual("Test_1", split.name);
            Assert.AreEqual(false, split.killed);
            Assert.AreEqual("ACTIVE", split.status);
            Assert.AreEqual("user", split.trafficTypeName);
            Assert.AreEqual("off", split.defaultTreatment);
            Assert.IsNotNull(split.conditions);
            Assert.AreEqual(-1, result.FeatureFlags.Since);
            Assert.AreEqual(1470855828956, result.FeatureFlags.Till);
            Assert.AreEqual(null, split.algo);
        }

        [TestMethod]
        public async Task FetchSplitChangesSuccessfullVerifyAlgorithmIsLegacy()
        {
            //Arrange
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                            's':-1,
                            't':-1,
                            'd':[]
                          },
                          'ff': {
                            'd': [
                            {
                              'trafficTypeName': 'user',
                              'name': 'Test_1',
                              'seed': 673896442,
                              'status': 'ACTIVE',
                              'killed': false,
                              'algo': 1,
                              'defaultTreatment': 'off',
                              'changeNumber': 1470855828956,
                              'conditions': [
                                {
                                  'matcherGroup': {
                                    'combiner': 'AND',
                                    'matchers': [
                                      {
                                        'keySelector': {
                                          'trafficType': 'user',
                                          'attribute': null
                                        },
                                        'matcherType': 'ALL_KEYS',
                                        'negate': false,
                                        'userDefinedSegmentMatcherData': null,
                                        'whitelistMatcherData': null,
                                        'unaryNumericMatcherData': null,
                                        'betweenMatcherData': null
                                      }
                                    ]
                                  },
                                  'partitions': [
                                    {
                                      'treatment': 'on',
                                      'size': 0
                                    },
                                    {
                                      'treatment': 'off',
                                      'size': 100
                                    }
                                  ]
                                }
                              ]
                            }   
                          ],
                          's': -1,
                          't': 1470855828956
                          }
                        }");

            //Act
            var result = await _apiFetcher.FetchAsync(new FetchOptions());

            //Assert
            Assert.IsNotNull(result);
            var split = result.FeatureFlags.Data.First();
            Assert.AreEqual(AlgorithmEnum.LegacyHash, (AlgorithmEnum)split.algo);
        }

        [TestMethod]
        public async Task FetchSplitChangesSuccessfullVerifyAlgorithmIsMurmur()
        {
            //Arrange
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                            's':-1,
                            't':-1,
                            'd':[]
                          },
                          'ff':{
                            'd': [
                            {
                              'trafficTypeName': 'user',
                              'name': 'Test_1',
                              'seed': 673896442,
                              'status': 'ACTIVE',
                              'killed': false,
                              'algo': 2,
                              'defaultTreatment': 'off',
                              'changeNumber': 1470855828956,
                              'conditions': [
                                {
                                  'matcherGroup': {
                                    'combiner': 'AND',
                                    'matchers': [
                                      {
                                        'keySelector': {
                                          'trafficType': 'user',
                                          'attribute': null
                                        },
                                        'matcherType': 'ALL_KEYS',
                                        'negate': false,
                                        'userDefinedSegmentMatcherData': null,
                                        'whitelistMatcherData': null,
                                        'unaryNumericMatcherData': null,
                                        'betweenMatcherData': null
                                      }
                                    ]
                                  },
                                  'partitions': [
                                    {
                                      'treatment': 'on',
                                      'size': 0
                                    },
                                    {
                                      'treatment': 'off',
                                      'size': 100
                                    }
                                  ]
                                }
                              ]
                            }   
                          ],
                          's': -1,
                          't': 1470855828956
                          }
                        }");

            //Act
            var result = await _apiFetcher.FetchAsync(new FetchOptions());

            //Assert
            Assert.IsNotNull(result);
            var split = result.FeatureFlags.Data.First();
            Assert.AreEqual(AlgorithmEnum.Murmur, (AlgorithmEnum)split.algo);
        }

        [TestMethod]
        public async Task FetchSplitChangesWithExcepionSouldReturnNull()
        {
            // Arrange.
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .Throws(new Exception());

            var apiFetcher = new ApiSplitChangeFetcher(_apiClient.Object);

            // Act.
            var result = await apiFetcher.FetchAsync(new FetchOptions());

            // Assert.
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task FetchSplitChangesSuccessfull_WhenConfigurationsIsNotNull()
        {
            //Arrange
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                            's':-1,
                            't':-1,
                            'd':[]
                          },
                          'ff':{
                            'd': [
                            {
                              'trafficTypeName': 'user',
                              'name': 'Test_1',
                              'seed': 673896442,
                              'status': 'ACTIVE',
                              'killed': false,
                              'algo': 2,
                              'defaultTreatment': 'off',
                              'changeNumber': 1470855828956,
                              'configurations': { 
                                'on': '{ size: 15 }',
                                'off':'{ size: 10 }'
                              },
                              'conditions': [
                                {
                                  'matcherGroup': {
                                    'combiner': 'AND',
                                    'matchers': [
                                      {
                                        'keySelector': {
                                          'trafficType': 'user',
                                          'attribute': null
                                        },
                                        'matcherType': 'ALL_KEYS',
                                        'negate': false,
                                        'userDefinedSegmentMatcherData': null,
                                        'whitelistMatcherData': null,
                                        'unaryNumericMatcherData': null,
                                        'betweenMatcherData': null
                                      }
                                    ]
                                  },
                                  'partitions': [
                                    {
                                      'treatment': 'on',
                                      'size': 0
                                    },
                                    {
                                      'treatment': 'off',
                                      'size': 100
                                    }
                                  ]
                                }
                              ]
                            }   
                          ],
                          's': -1,
                          't': 1470855828956
                          }
                        }");

            //Act
            var result = await _apiFetcher.FetchAsync(new FetchOptions());

            //Assert
            Assert.IsNotNull(result);
            var split = result.FeatureFlags.Data.First();
            Assert.AreEqual(AlgorithmEnum.Murmur, (AlgorithmEnum)split.algo);
            Assert.IsNotNull(split.configurations);
        }

        [TestMethod]
        public async Task FetchSplitChangesSuccessfull_WithSets()
        {
            // Arrange.
            _apiClient
                .Setup(x => x.FetchSplitChangesAsync(It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'rbs':{
                            's':-1,
                            't':-1,
                            'd':[]
                          },
                          'ff':{
                            'd': [
                            {
                              'trafficTypeName': 'user',
                              'name': 'Test_1',
                              'seed': 673896442,
                              'status': 'ACTIVE',
                              'killed': false,
                              'algo': 2,
                              'defaultTreatment': 'off',
                              'changeNumber': 1470855828956,
                              'sets': ['set_a', 'set_b', 'set_c', 'set_d'],
                              'configurations': { 
                                'on': '{ size: 15 }',
                                'off':'{ size: 10 }'
                              },
                              'conditions': [
                                {
                                  'matcherGroup': {
                                    'combiner': 'AND',
                                    'matchers': [
                                      {
                                        'keySelector': {
                                          'trafficType': 'user',
                                          'attribute': null
                                        },
                                        'matcherType': 'ALL_KEYS',
                                        'negate': false,
                                        'userDefinedSegmentMatcherData': null,
                                        'whitelistMatcherData': null,
                                        'unaryNumericMatcherData': null,
                                        'betweenMatcherData': null
                                      }
                                    ]
                                  },
                                  'partitions': [
                                    {
                                      'treatment': 'on',
                                      'size': 0
                                    },
                                    {
                                      'treatment': 'off',
                                      'size': 100
                                    }
                                  ]
                                }
                              ]
                            }   
                          ],
                          's': -1,
                          't': 1470855828956
                          }
                        }");

            // Act.
            var result = await _apiFetcher.FetchAsync(new FetchOptions());

            // Assert.
            var split = result.FeatureFlags.Data.First();
            Assert.AreEqual(4, split.Sets.Count);
            Assert.IsTrue(split.Sets.Contains("set_a"));
            Assert.IsTrue(split.Sets.Contains("set_b"));
            Assert.IsTrue(split.Sets.Contains("set_c"));
            Assert.IsTrue(split.Sets.Contains("set_d"));
        }
    }
}
