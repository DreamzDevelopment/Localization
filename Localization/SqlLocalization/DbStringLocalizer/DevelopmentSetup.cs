﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Localization.SqlLocalizer.DbStringLocalizer
{
    // >dotnet ef migrations add LocalizationMigration
    public class DevelopmentSetup
    {
        private readonly LocalizationModelContext _context;
        private readonly IOptions<SqlLocalizationOptions> _options;
        public readonly IOptions<SqlContextOptions> _sqlContextOptions;
        private IOptions<RequestLocalizationOptions> _requestLocalizationOptions;

        public DevelopmentSetup(
           LocalizationModelContext context,
           IOptions<SqlLocalizationOptions> localizationOptions,
           IOptions<SqlContextOptions> sqlContextOptions,
           IOptions<RequestLocalizationOptions> requestLocalizationOptions)
        {
            _options = localizationOptions;
            _sqlContextOptions = sqlContextOptions;
            _context = context;
            _requestLocalizationOptions = requestLocalizationOptions;
        }

        public void AddNewLocalizedItem(string key, string culture, string resourceKey)
        {
            if(_requestLocalizationOptions.Value.SupportedCultures.Contains(new System.Globalization.CultureInfo(culture)))
            {
                string computedKey = $"{key}.{culture}";

                LocalizationRecord localizationRecord = new LocalizationRecord()
                {
                    LocalizationCulture = culture,
                    Key = key,
                    Text = computedKey,
                    ResourceKey = resourceKey
                };
                try {
                    _context.LocalizationRecords.Add(localizationRecord);
                    _context.SaveChanges();
                } catch(System.Exception) {

                }  
                
            }
        }
    }
}