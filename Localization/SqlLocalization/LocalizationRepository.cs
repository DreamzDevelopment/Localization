using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

using Localization.SqlLocalizer.DbStringLocalizer;

using JsonManager;
using JsonManager.Helpers;

namespace Localization.SqlLocalizer
{
    public class LocalizationRepository: ILocalizationRepository
    {
        private readonly IOptions<SqlContextOptions> _SqlContextOption;
        private readonly LocalizationModelContext _LocalizationContext;
        private readonly IStringExtendedLocalizerFactory _SqlLocalizerFactory;
        private readonly IJsonManagerRepository _JsonManagerRepository;
        private static IList<IEntityType> EntityList = new List<IEntityType>();
        public LocalizationRepository(IOptions<SqlContextOptions> sqlContextOption, IStringExtendedLocalizerFactory sqlLocalizerFactory,
            LocalizationModelContext localizationContext, IJsonManagerRepository jsonManagerRepository )
        {
            _SqlContextOption = sqlContextOption;
            _SqlLocalizerFactory =  sqlLocalizerFactory;
            _LocalizationContext = localizationContext;
            _JsonManagerRepository = jsonManagerRepository;
        }
#region GetEntityList
        /// <summary>
        /// Get Table/EntityName of Context
        /// </summary>
        async Task<IList<IEntityType>> ILocalizationRepository.GetEntityList(DbContext context)
        {
            // Query in a separate context so that we can attach existing entities as modified;
            // PropertyInfo[] my_tables = _SQLContext.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if(EntityList != null && EntityList.Count > 0) 
            {

            } else {
                EntityList = await GetEntityList(context);
            }
            return EntityList;
        }

#endregion
#region GetEntityAttributes
        /// <summary>
        /// Get Attribute(Field of Table) of Entity
        /// </summary>
        /// <param name="EntityName">Name of Table/Entity</param>
        
        async Task<ICollection<EntityProperty>> ILocalizationRepository.GetEntityAttributes(string EntityName)
        {
            // Query in a separate context so that we can attach existing entities as modified;

            // Type entityType = Type.GetType("AspNetCore2Angular5.Entities.Products.Product"); 
            // typeof(Product); you can look up the type name by string if you like as well, using `Type.GetType()`
            // Type abstractDAOType = typeof(MyModel<>).MakeGenericType(entityType);
            // dynamic dao = Activator.CreateInstance(abstractDAOType);

            var tbl_listname = new List<EntityProperty>();

            var i = 0;
            // var entName = "";
            if(EntityList != null) {
                var itmTo = EntityList.FirstOrDefault(item => item.Name.ToString().ToLower() == EntityName.ToLower()); 
                //.Substring(item.Name.ToString().LastIndexOf('.') + 1)
                if(itmTo != null) {
                    foreach (var prop in itmTo.GetProperties())
                    {
                        i++;
                        tbl_listname.Add(new EntityProperty() { Id = i, PropertyName = prop.Name.ToString() });
                    }
                }
            }
            tbl_listname.OrderBy(t => t.PropertyName); // Order by Property Name
            return tbl_listname;
        }

#endregion

#region GetPropertyValuesForAttributes
        /// <summary>
        /// Get Values for particular? attribute(s)(Member Variable(s)) of an Entity(Class) from Context Table.
        /// </summary>
        /// <param name="query">IQueryable of DbSet of type of DbContext</param>
        /// <param name="properties">The (;) semi-colon separated string of Selected Properties for EntityType</param>
        /// <param name="properties"></param>
        async Task<ICollection<PropertyData>> ILocalizationRepository.GetPropertyValueForAttribute(IQueryable query, string properties)
        {
            if (string.IsNullOrWhiteSpace(properties) ) {
                return null;
            }
            ICollection<PropertyData> _properties = new List<PropertyData>();
            if(query != null) {
                var selectProps = properties.Split(';');
                for (int i = 0; i < selectProps?.Length; i++)
                {
                    var prop = selectProps[i];
                    if(!string.IsNullOrWhiteSpace(prop) )
                    {
                        var objects = query?.Select(prop).Distinct();
                        if(objects != null)
                        {
                            var j = 0;
                            var _propertyData = new PropertyData(){ Id = i, PropertyName = prop, PropertyValues = new List<PropertyValue>() };
                            foreach (var item in objects)
                            {
                                j++;
                                if(item != null) {
                                    _propertyData.PropertyValues.Add(new PropertyValue( (i * 10000) + j, item.ToString(), item.ToString()) );
                                }
                            }
                            _properties.Add(_propertyData);
                        }
                    }
                }
            }
            
            _properties?.GroupBy(p => p.PropertyName); // Group property by Name
            return _properties;
        }
#endregion

#region  GetTranslateValueForValue
        /// <summary>
        /// Find translated value for a value of an Attribute(Member Variable value).
        /// </summary>
        /// <param name="translateTo">The target language.</param>
        /// <param name="propertyValue">The value to translate.</param>
        /// <param name="resourceKey">The Resource Key to find Server Resource, such as (.Net => Controller, Model, Views etc.)</param>
        async Task<PropertyValue> ILocalizationRepository.GetTransValueForValue(string translateTo, string propertyValue, string resourceKey, bool ignoreCase = false)
        {
            var _propertyValue = new PropertyValue();
            if(string.IsNullOrWhiteSpace(resourceKey)) { 
                var _file_trans = $"locale-{translateTo}.json";
                var file_path = Directory.GetCurrentDirectory() + $"\\wwwroot\\i18n\\";
                string lineToFind = "";
                if(File.Exists(file_path + _file_trans) == true) {
                    lineToFind = await _JsonManagerRepository.GetLineByKey(file_path + _file_trans, propertyValue, ignoreCase);
                    if(!string.IsNullOrWhiteSpace(lineToFind) && lineToFind.IndexOf(':') > 0 ) {
                        _propertyValue.Key = lineToFind.Split(':')[0].Replace( "\"", string.Empty).Remove(0, 1);
                        _propertyValue.Value = _propertyValue.Key;
                        _propertyValue.TranslatedValue = lineToFind.Split(':')[1].Replace( "\"", string.Empty);
                    }
                }
            } else { // if explicit resource key provided, then this resource is Rendered in Server 
                var _record = await ((ILocalizationRepository)this).GetLocalizationRecord(propertyValue, resourceKey, translateTo);
                    _propertyValue.Key = _record?.Key;
                    _propertyValue.Value = _record?.Key;
                    _propertyValue.TranslatedValue = _record?.Text;
            }
            return _propertyValue;
        }
#endregion
#region GetTransValuesForSearch
        /// <summary>
        /// Get list of transvalues by search string
        /// </summary>
        /// <param name="searchId"></param>
        /// <param name="transLang">translated language: string</param>
        /// <param name="searchString">query string: string</param>
        /// <param name="resourceKey">The server Resource key, if search in Localization DB</param>
        async Task<PropertyData> ILocalizationRepository.GetTransValuesForSearch(int searchId, string transLang, string searchString, string resourceKey, bool ignoreCase = false)
        {
            var searchResult = new PropertyData() { Id = searchId, PropertyName = searchString, PropertyValues = new List<PropertyValue>() };
            if(string.IsNullOrWhiteSpace(resourceKey)) { 
                string[] linesToKeep = null;
                var _file_trans = $"locale-{transLang}.json";
                var file_path = Directory.GetCurrentDirectory() + $"\\wwwroot\\i18n\\";

                if(File.Exists(file_path + _file_trans) == true) {
                    string queryStr = searchString;
                    if(searchString.Length <= 1) { // must be 1 // null or whitespace checked in calling from...
                        queryStr = '"' + searchString; // starts with
                    }
                    string[] lines = File.ReadAllLines(file_path + _file_trans);
                    if(lines.Count() > 2) {
                        linesToKeep = ignoreCase ? lines.Where(l => l.ToString().ToLower().IndexOf(queryStr.ToString().ToLower()) >= 0).ToArray() : 
                                        lines.Where(l => l.ToString().IndexOf(queryStr.ToString()) >= 0).ToArray();
                    }
                    var key = "";
                    var value = "";
                    for (int i = 0; i < linesToKeep.Length; i++)
                    {
                        key = linesToKeep[i].Split(':')[0].Replace( "\"", string.Empty).Remove(0, 1);
                        value = linesToKeep[i].Split(':')[1].Replace( "\"", string.Empty);
                        searchResult.PropertyValues.Add(new PropertyValue((searchId * 1000) + i, key, key, value));
                    }
                }
            } else { // if explicit resource key provided, then this resource is Rendered in Server 
                var resources = await ((ILocalizationRepository)this).SearchLocalizationRecords(searchString, transLang);
                var i = 0;
                foreach (var item in resources)
                {
                    searchResult.PropertyValues.Add(new PropertyValue((searchId * 1000) + i, item.Key, item.Key, item.Text));
                    i++;
                }
            }
            return searchResult;
        }    
#endregion

#region  UpdateTranslateValue
        /// <summary>
        /// Add entries into i18n folder's > json files
        /// </summary>
        /// <param name="translateTo">language(code i.e bn, en)</param>
        /// <param name="propertyValue">Value in English</param>
        /// <param name="transValue">Value in Translated Lang</param>
        /// <param name="resourceKey" type="optional">The resource Key implicitly given for server resource. i.e .Net Controller, Model, View etc.</param>
        
        async Task<IJsonResult> ILocalizationRepository.UpdateTranslateValue(string translateTo, string propertyValue, string transValue, string resourceKey, bool addIfNotFound, bool ignoreCase = false)
        {
            IJsonResult jsonResult = new MyJsonResult() { Message = $"Failed to update translation for '{propertyValue}' to '{transValue}'", Result= JsonResultFlag.Failed };
            if(string.IsNullOrWhiteSpace(resourceKey)) { // Client Resource
                var _file_ = $"locale-en.json";
                var _file_trans = $"locale-{translateTo}.json";
                var file_path = Directory.GetCurrentDirectory() + $"\\wwwroot\\i18n\\";
                string[] linesToKeep;
                
                JsonHelper.CreateFileIfNeeded(file_path , _file_);
                if(File.Exists(file_path + _file_) == true) {
                    var tempFile = Path.GetTempFileName();
                    var lineToAdd = await _JsonManagerRepository.GetLineByKey(file_path + _file_, propertyValue, ignoreCase);
                    var _propertyValue = propertyValue;
                    if(!string.IsNullOrWhiteSpace(lineToAdd) && (lineToAdd.Length > 0) && (lineToAdd.IndexOf(':') > 0) && (lineToAdd.IndexOf(':') + 2 < lineToAdd.Length -1))
                    {
                        _propertyValue = lineToAdd.Substring(lineToAdd.IndexOf(':') + 3, lineToAdd.Length - (lineToAdd.IndexOf(':') + 4) );
                    }
                    linesToKeep = await _JsonManagerRepository.AddOrUpdate(file_path + _file_, propertyValue, _propertyValue, true, ignoreCase);
                    if(linesToKeep != null) {
                        if(linesToKeep.Count() > 2) {
                            linesToKeep[1] = linesToKeep[1].Substring(linesToKeep[1].IndexOf(',') + 1); // important to remove ',' at the beginning of json file
                        }
                        File.WriteAllLines(tempFile, linesToKeep.ToList());
                        File.Delete(file_path + _file_);
                        File.Move(tempFile, file_path + _file_);
                    }
                }
                JsonHelper.CreateFileIfNeeded(file_path , _file_trans);
                if(File.Exists(file_path + _file_trans) == true){
                    var tempFile = Path.GetTempFileName();
                    linesToKeep = await _JsonManagerRepository.AddOrUpdate(file_path + _file_trans, propertyValue, transValue, true, ignoreCase);
                    if(linesToKeep != null) {
                        if(linesToKeep.Count() > 2) {
                            linesToKeep[1] = linesToKeep[1].Substring(linesToKeep[1].IndexOf(',') + 1); // important to remove ',' at the beginning of json file
                        }
                        File.WriteAllLines(tempFile, linesToKeep.ToList());
                        File.Delete(file_path + _file_trans);
                        File.Move(tempFile, file_path + _file_trans);
                    }
                }
                jsonResult.Message = $"Successfully updated translation for '{propertyValue}' to '{transValue}'";
                jsonResult.Result = JsonResultFlag.Succeeded;
            }
            else { // Server Resource
                jsonResult = await ((ILocalizationRepository)this).UpdateLocalizationRecord(propertyValue, transValue, resourceKey, translateTo, addIfNotFound);
            }
            return jsonResult;
        }
#endregion

#region GetLocalizationRecords
        async Task<System.Collections.IList> ILocalizationRepository.GetLocalizationRecords(string reason) 
        {
            return _SqlLocalizerFactory.GetLocalizationData(reason);
        }
    
#endregion
#region ResetLocalizationCache
        /// <summary>
        /// Reset Localization Cache, and Localization Resources will be updated in Runtime
        /// </summary>
        async Task<IJsonResult> ILocalizationRepository.ResetLocalizationCache(string cultureName, bool ignoreCase = false)
        {
            IJsonResult jsonResult = new MyJsonResult() { Message = $"Failed to Reset Localization Cache for '{cultureName}'", Result= JsonResultFlag.Failed };
            try
            {
                _SqlLocalizerFactory.ResetCache();
                jsonResult.Result = JsonResultFlag.Succeeded;
                jsonResult.Message = $"Succeeded to Reset Localization Cache for '{cultureName}'";
            }
            catch (System.Exception)
            {
                jsonResult.Result = JsonResultFlag.Error;
            }
            var jsonResult1 = await ((ILocalizationRepository)this).PushClientLocalizationData(cultureName, ignoreCase);
                jsonResult.Message += ". " + jsonResult1.Message;
            return jsonResult;
        }
#endregion
#region PushClientLocalizationData
        async Task<IJsonResult> ILocalizationRepository.PushClientLocalizationData(string cultureName, bool ignoreCase = false)
        {
            IJsonResult jsonResult = new MyJsonResult() { Message = $"Failed to Push Client Localization Cache for '{cultureName}'", Result= JsonResultFlag.Failed };
            
            string _cultureName = cultureName;
            string _twoLetterISOLanguageName = "";
            if(string.IsNullOrWhiteSpace(_cultureName)) {
                CultureInfo _culture = CultureInfo.CurrentCulture;
                _cultureName = _culture.Name;
                _twoLetterISOLanguageName = _culture.TwoLetterISOLanguageName;
            } else {
                _twoLetterISOLanguageName = _cultureName.Split('-')[0];
            }
            if(!string.IsNullOrWhiteSpace(_cultureName) && !string.IsNullOrWhiteSpace(_twoLetterISOLanguageName))
            {
                if(_twoLetterISOLanguageName == "en")  {
                    jsonResult.Result = JsonResultFlag.Succeeded;
                    jsonResult.Message = $"Default Language to Push Client Localization Cache for '{cultureName}'";
                } else {
                    var _file_trans = $"locale-{_twoLetterISOLanguageName}.json";
                    var file_path = Path.Combine( Directory.GetCurrentDirectory() , $"wwwroot/i18n");
                    IList<LocalizationRecord> _resources = new List<LocalizationRecord>();
                    if(File.Exists(Path.Combine(file_path , _file_trans)) )
                    { // Update with latest records from last time updated.
                        var lastWriteTime = File.GetLastWriteTimeUtc(Path.Combine(file_path , _file_trans) );
                        /// <summary>
                        /// This is required to get changes of the day, AddDays(-1), when lastWriteTime is today
                        /// </summary>
                        _resources = (IList<LocalizationRecord>)_SqlLocalizerFactory.GetLocalizationData(lastWriteTime.AddDays(-1), _cultureName, "export");
                    } else { // Retrieve Localization Data, from the beginning
                        JsonHelper.CreateFileIfNeeded(file_path, _file_trans);
                        _resources = (IList<LocalizationRecord>)_SqlLocalizerFactory.GetLocalizationData(DateTime.Now.AddYears(-100), _cultureName);
                    }
                    foreach (var item in _resources)
                    {
                        var partsKey = item.Key.Split('.');
                        var partsText = item.Text.Split('.');
                        if(partsKey.Length > 0) {
                            for (int i = 0; i < partsKey.Length; i++)
                            {
                                await _JsonManagerRepository.AddOrUpdateJson(partsKey[i].Trim(), partsText[i].Trim(), file_path, _file_trans, true, ignoreCase);
                            }
                        } else {
                            await _JsonManagerRepository.AddOrUpdateJson(item.Key.Trim(), item.Text.Trim(), file_path, _file_trans, true, ignoreCase);
                        }
                    }
                    jsonResult.Result = JsonResultFlag.Succeeded;
                    jsonResult.Message = $"Succeeded to Push Client Localization Cache for '{cultureName}'";
                }
            }
            return jsonResult;
        }
#endregion

#region GetServerResourceKeys
        /// <summary>
        /// Get all the keys from SQLite DB file for server resource culture/translateTo
        /// </summary>
        /// <param name="translateTo">translate lang: string </param>
        async Task<string[]> ILocalizationRepository.GetServerResourceKeys(string translateTo)
        {
            string[] resourceKeys = null;
            var _sqliteOptions = new DbContextOptionsBuilder<LocalizationModelContext>()
                                .UseSqlite(_SqlContextOption.Value.ConLocalization)
                                .Options;
            using (var localizationContext = new LocalizationModelContext(_sqliteOptions, _SqlContextOption))
            {
                resourceKeys = await localizationContext.LocalizationRecords.Where(r => 
                    r.LocalizationCulture.ToLower() == translateTo.ToLower())
                    .Select(r => r.ResourceKey).Distinct()
                    .ToArrayAsync();
            }
            return resourceKeys;
        }   
#endregion

#region SearchLocalizationRecords
        /// <summary>
        /// Find Localization Records from Localization DB, with searchKey and Culture
        /// </summary>
        /// <param name="searchKey">The term to match in Key/Value of Localization Resources</param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        async Task<ICollection<LocalizationRecord>> ILocalizationRepository.SearchLocalizationRecords(string searchKey, string localizationCulture) 
        {
            var _resources = new List<LocalizationRecord>();
            var _sqliteOptions = new DbContextOptionsBuilder<LocalizationModelContext>()
                                .UseSqlite(_SqlContextOption.Value.ConLocalization)
                                .Options;
            using (var localizationContext = new LocalizationModelContext(_sqliteOptions, _SqlContextOption))
            {
                _resources = await localizationContext.LocalizationRecords.Where(r => r.LocalizationCulture == localizationCulture &&
                    (r.Key.ToLower().Contains(searchKey.ToLower()) || r.Text.ToLower().Contains(searchKey.ToLower()) )).ToListAsync();
            }
            return _resources;
        }
#endregion
#region GetLocalizationRecord
        /// <summary>
        /// Find a Localization Record from Localization DB, with Key, ResourceKey and Culture
        /// </summary>
        /// <param name="key">Key of key-value pair in Localization Record</param>
        /// <param name="resourceKey">The Type of Record, Domain specificity </param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        async Task<LocalizationRecord> ILocalizationRepository.GetLocalizationRecord(string key, string resourceKey, string localizationCulture) 
        {
            var _resource = new LocalizationRecord();
            var _sqliteOptions = new DbContextOptionsBuilder<LocalizationModelContext>()
                                .UseSqlite(_SqlContextOption.Value.ConLocalization)
                                .Options;
            using (var localizationContext = new LocalizationModelContext(_sqliteOptions, _SqlContextOption))
            {
                _resource = await localizationContext.LocalizationRecords.FirstOrDefaultAsync(r => 
                    r.Key == key && r.ResourceKey == resourceKey && r.LocalizationCulture == localizationCulture);
            }
            return _resource;
        }
#endregion

#region AddLocalizationRecord
        /// <summary>
        /// Add new Localization Record into Localization DB, with Key, ResourceKey and Culture
        /// </summary>
        /// <param name="key">Key of key-value pair in Localization Record</param>
        /// <param name="text">Value of Key-Value pair in Localization Record</param>
        /// <param name="resourceKey">The Type of Record, Domain specificity </param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        async Task<IJsonResult> ILocalizationRepository.AddLocalizationRecord(string key, string text, string resourceKey, string localizationCulture) 
        {
            IJsonResult jsonResult = new MyJsonResult() { Message = $"Failed to Add translation for '{key}' to '{text}'", Result= JsonResultFlag.Failed };
            
            LocalizationRecord _resource = await ((ILocalizationRepository)this).GetLocalizationRecord(key, resourceKey, localizationCulture);
            if(_resource == null) {
                var _sqliteOptions = new DbContextOptionsBuilder<LocalizationModelContext>()
                                .UseSqlite(_SqlContextOption.Value.ConLocalization)
                                .Options;
                using (var localizationContext = new LocalizationModelContext(_sqliteOptions, _SqlContextOption))
                {
                    _resource = new LocalizationRecord() { Key = key, Text = text, ResourceKey = resourceKey, LocalizationCulture = localizationCulture };
                    var data = new List<LocalizationRecord>();
                        data.Add(_resource);
                        _SqlLocalizerFactory.AddNewLocalizationData(data, DateTimeOffset.UtcNow.ToString());
                    // localizationContext.Entry(_resource).State = EntityState.Modified;
                    // await localizationContext.SaveChangesAsync();
                    jsonResult.Result = JsonResultFlag.Succeeded;
                    jsonResult.Message = $"Successfully added translation for '{key}' to '{text}'";
                }
            } else {
                jsonResult.Result = JsonResultFlag.Existed;
                jsonResult.Message = $"Existed translation for '{key}' to '{text}'";
            }
            return jsonResult;
        }
#endregion

#region UpdateLocalizationRecord
        /// <summary>
        /// Update a Localization Record from Localization DB, with Key, ResourceKey and Culture
        /// </summary>
        /// <param name="key">Key of key-value pair in Localization Record</param>
        /// <param name="text">Value of Key-Value pair in Localization Record</param>
        /// <param name="resourceKey">The Type of Record, Domain specificity </param>
        /// <param name="localizationCulture">Translation Language / Current Culture</param>
        async Task<IJsonResult> ILocalizationRepository.UpdateLocalizationRecord(string key, string text, string resourceKey, string localizationCulture, bool addIfNotFound) 
        {
            IJsonResult jsonResult = new MyJsonResult() { Message = $"Failed to update translation for '{key}' to '{text}'", Result= JsonResultFlag.Failed };
            
            LocalizationRecord _resource = await ((ILocalizationRepository)this).GetLocalizationRecord(key, resourceKey, localizationCulture);
            if(_resource != null) {
                var _sqliteOptions = new DbContextOptionsBuilder<LocalizationModelContext>()
                                .UseSqlite(_SqlContextOption.Value.ConLocalization)
                                .Options;
                using (var localizationContext = new LocalizationModelContext(_sqliteOptions, _SqlContextOption))
                {
                    _resource.Text = text;
                    var data = new List<LocalizationRecord>();
                        data.Add(_resource);
                    _SqlLocalizerFactory.UpdateLocalizationData(data, DateTimeOffset.UtcNow.ToString());
                    // localizationContext.Entry(_resource).State = EntityState.Modified;
                    // await localizationContext.SaveChangesAsync();
                    jsonResult.Result = JsonResultFlag.Succeeded;
                    jsonResult.Message = $"Successfully updated translation for '{key}' to '{text}'";
                }
            } else {
                if(addIfNotFound) {
                    jsonResult = await ((ILocalizationRepository)this).AddLocalizationRecord(key, text, resourceKey, localizationCulture);
                } else {
                    jsonResult.Result = JsonResultFlag.NotFound;
                    jsonResult.Message = $"Resource not found to be updated, translation for '{key}' to '{text}'";
                }
            }
            return jsonResult;
        }
#endregion

#region Helpers

        private async Task<IList<IEntityType>> GetEntityList(DbContext context) 
        {
            
            IEnumerable<IEntityType> entities = context.Model.GetEntityTypes();
            foreach (var item in entities)
            {
                EntityList.Add(item);
            }
            // entities = _SQLiteContext.Model.GetEntityTypes();
            // foreach (var item in entities)
            // {
            //     EntityList.Add(item);
            // }
            // /// <summary>
            // /// this is to exclude from translation, because Translation Data is Provided from this Context
            // /// </summary>
            // // entities = _LocalizationContext.Model.GetEntityTypes(); 
            // // foreach (var item in entities)
            // // {
            // //     EntityList.Add(item);
            // // }
            EntityList.OrderBy(t => t.Name); // Order by Entity Name
            return EntityList;
        }
        
#endregion
#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~LocalizationRepository() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void System.IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
    #endregion

    }
}