using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;

using Localization.SqlLocalizer.DbStringLocalizer;
using JsonManager.Helpers;

namespace Localization.SqlLocalizer
{
    public interface ILocalizationRepository : IDisposable
    {
        Task<IList<IEntityType>> GetEntityList(DbContext context);
        Task<ICollection<EntityProperty>> GetEntityAttributes(string EntityName);
        /// <summary>
        /// Get Values for particular? attribute(s)(Member Variable(s)) of an Entity(Class) from Context Table.
        /// </summary>
        /// <param name="fullQualifiedName">The name of the Entity with NameSpace (Fully Qualified Name)</param>
        /// <param name="properties">The (;) semi-colon separated string of Selected Properties for EntityType</param>
        Task<ICollection<PropertyData>> GetPropertyValueForAttribute(IQueryable query, string properties);
        /// <summary>
        /// Find translated value for a value of an Attribute(Member Variable value).
        /// </summary>
        /// <param name="translateTo">The target language.</param>
        /// <param name="propertyValue">The value to translate.</param>
        /// <param name="resourceKey">The Resource Key to find Server Resource, such as (.Net => Controller, Model, Views etc.)</param>
        Task<PropertyValue> GetTransValueForValue(string translateTo, string propertyValue, string resourceKey, bool ignoreCase);
        Task<LocalizationRecord> GetLocalizationRecord(string key, string resourceKey, string localizationCulture);
        /// <summary>
        /// Get all the keys from SQLite DB file for server resource culture/translateTo
        /// </summary>
        /// <param name="translateTo">translate lang: string </param>
        Task<string[]> GetServerResourceKeys(string translateTo);
        /// <summary>
        /// Get list of transvalues by search string
        /// </summary>
        /// <param name="searchId"></param>
        /// <param name="transLang">translated language: string</param>
        /// <param name="searchString">query string: string</param>
        /// <param name="resourceKey">The server Resource key, if search in Localization DB</param>
        Task<PropertyData> GetTransValuesForSearch(int searchId, string transLang, string searchString, string resourceKey, bool ignoreCase);
        /// <summary>
        /// Find Localization Records from Localization DB, with searchKey and Culture
        /// </summary>
        /// <param name="searchKey">The term to match in Key/Value of Localization Resources</param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        Task<ICollection<LocalizationRecord>> SearchLocalizationRecords(string searchKey, string localizationCulture);
        /// <summary>
        /// Add entries into i18n folder's > json files
        /// </summary>
        /// <param name="translateTo">language(code i.e bn, en)</param>
        /// <param name="propertyValue">Value in English</param>
        /// <param name="transValue">Value in Translated Lang</param>
        /// <param name="resourceKey" type="optional">The resource Key implicitly given for server resource. i.e .Net Controller, Model, View etc.</param>
        Task<IJsonResult> UpdateTranslateValue(string translateTo, string propertyValue, string transValue, string resourceKey, bool addIfNotFound, bool ignoreCase);
        /// <summary>
        /// Update a Localization Record from Localization DB, with Key, ResourceKey and Culture
        /// </summary>
        /// <param name="key">Key of key-value pair in Localization Record</param>
        /// <param name="text">Value of Key-Value pair in Localization Record</param>
        /// <param name="resourceKey">The Type of Record, Domain specificity </param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        Task<IJsonResult> UpdateLocalizationRecord(string key, string text, string resourceKey, string localizationCulture, bool addIfNotFound);
        /// <summary>
        /// Add new Localization Record into Localization DB, with Key, ResourceKey and Culture
        /// </summary>
        /// <param name="key">Key of key-value pair in Localization Record</param>
        /// <param name="text">Value of Key-Value pair in Localization Record</param>
        /// <param name="resourceKey">The Type of Record, Domain specificity </param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        Task<IJsonResult> AddLocalizationRecord(string key, string text, string resourceKey, string localizationCulture);
        Task<IList> GetLocalizationRecords(string reason);
        Task<IJsonResult> PushClientLocalizationData(string cultureName, bool ignoreCase);
        Task<IJsonResult> ResetLocalizationCache(string cultureName, bool ignoreCase);
    }
}