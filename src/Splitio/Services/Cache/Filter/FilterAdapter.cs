using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.Cache.Filter
{
    public class FilterAdapter : IFilterAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(FilterAdapter));

        private readonly IFilter _filter;

        public FilterAdapter(IFilter filter)
        {
            _filter = filter;
        }

        public bool Add(string featureName, string key)
        {
            try
            {
                return _filter.Add($"{featureName}{key}");
            }
            catch (Exception ex)
            {
                _logger.Debug("Exception caught adding inside filter.", ex);
                return false;
            }
        }

        public bool Contains(string featureName, string key)
        {
            try
            {
                return _filter.Contains($"{featureName}{key}");
            }
            catch (Exception ex)
            {
                _logger.Debug("Exception caught in filter.Contains.", ex);
                return false;
            }
        }

        public void Clear()
        {
            try
            {
                _filter.Clear();
            }
            catch (Exception ex)
            {
                _logger.Debug("Exception caught Clearing filter.", ex);
            }
        }
    }
}
