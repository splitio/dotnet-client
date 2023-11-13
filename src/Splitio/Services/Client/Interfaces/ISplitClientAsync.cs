using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Interfaces
{
    public interface ISplitClientAsync
    {
        /// <summary>
        /// Returns the treatment to show this key for this feature flag.
        /// The set of treatments for a feature flag can be configured on the Split user interface.
        /// </summary>
        /// <param name="key">a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null.</param>
        /// <param name="feature">the name of the feature flag we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>string with evaluated treatment, the default treatment of this feature flag, or 'control'.</returns>
        Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Returns the treatment to show this key for this feature flag. The set of treatments for a feature flag can be configured on the Split user interface.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="feature">the name of the feature flag we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>string with evaluated treatment, the default treatment of this feature flag, or 'control'</returns>
        Task<string> GetTreatmentAsync(Key key, string feature, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Returns a Dictionary of feature flag name and treatments to show this key for these feature flags. The set of treatments 
        /// for a feature flag can be configured on the Split user interface.
        /// </summary>
        /// <param name="key">a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null.</param>
        /// <param name="features">the names of the feature flags we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>
        /// Dictionay<string, string> containing for each feature flag the evaluated treatment, the default treatment for each feature flag, or 'control'.
        /// </returns>
        Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Returns a Dictionary of feature flag name and treatments to show this key for these feature flags. The set of treatments 
        /// for a feature flag can be configured on the Split user interface.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="features">the names of the feature flags we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>Dictionay<string, string> containing for each feature flag the evaluated treatment, the default treatment for each feature flag, or 'control'.</returns>
        Task<Dictionary<string, string>> GetTreatmentsAsync(Key key, List<string> features, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatment but it returns the configuration associated to the matching treatment if any.
        /// Otherwise SplitResult.Config will be null.
        /// </summary>
        /// <param name="key">a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null.</param>
        /// <param name="feature">the name of the feature flag we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>
        /// SplitResult containing the evaluated treatment (the default treatment of this feature flag, or 'control') and
        /// a configuration associated to this treatment if set.
        /// </returns>
        Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatment but it returns the configuration associated to the matching treatment if any.
        /// Otherwise SplitResult.Config will be null.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="feature">the name of the feature flag we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>
        /// SplitResult containing the evaluated treatment (the default treatment of this feature flag, or 'control') and
        /// a configuration associated to this treatment if set.
        /// </returns>
        Task<SplitResult> GetTreatmentWithConfigAsync(Key key, string feature, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatments but it returns the configuration associated to the matching treatments if any.
        /// Otherwise SplitResult.Config will be null.
        /// </summary>
        /// <param name="key">a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null.</param>
        /// <param name="features">the names of the feature flags we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>
        /// Dictionay<string, SplitResult> containing for each feature flag the evaluated treatment (the default treatment of
        /// this feature flag, or 'control') and a configuration associated to this treatment if set.
        /// </returns>
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatments but it returns the configuration associated to the matching treatments if any.
        /// Otherwise SplitResult.Config will be null.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="features">the names of the feature flags we want to evaluate. MUST NOT be null.</param>
        /// <param name="attributes">of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>
        /// Dictionay<string, SplitResult> containing for each feature flag the evaluated treatment (the default treatment of
        /// this feature flag, or 'control') and a configuration associated to this treatment if set.
        /// </returns>
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(Key key, List<string> features, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatmentsWithConfig but this method evaluate by FlagSets and returns a Dictionary<string, SplitResult> containing the resulting treatment for each feature flag evaluated.
        /// </summary>
        /// <param name="key"> a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null or empty.</param>
        /// <param name="flagSets"> the names of Flag Sets that you want to evaluate. MUST not be null or empty</param>
        /// <param name="attributes"> of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>Dictionary<string, SplitResult> containing for each feature flag the evaluated treatment (the default treatment of this feature flag, or 'control') and a configuration associated to this treatment if set.</returns>
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatmentsWithConfig but this method evaluate by FlagSets and returns a Dictionary<string, SplitResult> containing the resulting treatment for each feature flag evaluated.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="flagSets"> the names of Flag Sets that you want to evaluate. MUST not be null or empty</param>
        /// <param name="attributes"> of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>Dictionary<string, SplitResult> containing for each feature flag the evaluated treatment (the default treatment of this feature flag, or 'control') and a configuration associated to this treatment if set.</returns>
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatments but this method evaluate by FlagSets and returns a Dictionary<string, string> containing the resulting treatment for each feature flag evaluated.
        /// </summary>
        /// <param name="key"> a unique key of your customer (e.g. user_id, user_email, account_id, etc.) MUST not be null or empty.</param>
        /// <param name="flagSets"> the names of Flag Sets that you want to evaluate. MUST not be null or empty</param>
        /// <param name="attributes"> of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>Dictionary<string, string> containing for each feature flag the evaluated treatment (the default treatment of this feature flag, or 'control').</returns>
        Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Same as GetTreatments but this method evaluate by FlagSets and returns a Dictionary<string, string> containing the resulting treatment for each feature flag evaluated.
        /// </summary>
        /// <param name="key">the matching and bucketing keys. MUST NOT be null.</param>
        /// <param name="flagSets"> the names of Flag Sets that you want to evaluate. MUST not be null or empty</param>
        /// <param name="attributes"> of the customer (user, account etc.) to use in evaluation. Can be null or empty.</param>
        /// <returns>Dictionary<string, string> containing for each feature flag the evaluated treatment (the default treatment of this feature flag, or 'control').</returns>
        Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null);

        /// <summary>
        /// Enqueue a new event to be sent to split data collection services.
        /// </summary>
        /// <param name="key">the identifier of the entity.</param>
        /// <param name="trafficType">the type of the event.</param>
        /// <param name="eventType">the type of the event.</param>
        /// <param name="value">the value of the event.</param>
        /// <param name="properties">a map of key value pairs that can be used to filter your metrics.</param>
        /// <returns>true if the track was successful, false otherwise</returns>
        Task<bool> TrackAsync(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null);

        /// <summary>
        /// Destroys the background processes and clears the cache, releasing the resources used by 
        /// the any instances of SplitClient or SplitManager generated by the client's parent SplitFactory
        /// </summary>
        Task DestroyAsync();
    }
}
