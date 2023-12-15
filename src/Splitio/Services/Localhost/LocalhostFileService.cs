﻿using Splitio.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Splitio.Services.Localhost
{
    public class LocalhostFileService : AbstractLocalhostFileService
    {
        public override ConcurrentDictionary<string, ParsedSplit> ParseSplitFile(string filePath)
        {
            var splits = new ConcurrentDictionary<string, ParsedSplit>();

            try
            {
                string line;
                using (var file = new StreamReader(filePath))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        {
                            continue;
                        }

                        var feature_treatment = Regex.Split(line, @"\s+");

                        if (feature_treatment.Length != 2)
                        {
                            _log.Info("Ignoring line since it does not have exactly two columns: " + line);
                            continue;
                        }

                        var splitName = feature_treatment[0];
                        var treatment = feature_treatment[1];
                        var conditions = new List<ConditionWithLogic> { CreateCondition(treatment) };

                        splits.TryAdd(splitName, CreateParsedSplit(splitName, treatment, conditions));
                        _log.Info("100% of keys will see " + treatment + " for " + splitName);

                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Something went wrong parsing SplitFile.", ex);
            }

            return splits;
        }
    }
}