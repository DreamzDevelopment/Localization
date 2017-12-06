# <img src="https://github.com/DreamzDevelopment/Localization/tree/master/Icons/Dreamz.Development.png" width="48" />Dev.Localization.Sql

    AspNet Core Localization using SQL database for KEY-VALUE pair and share common storage for both SERVER and Client App.
    This repository, SQL Localization, inspired by Damien Bod's - https://damienbod.com/, AspNetCore Localization project.

## Base version of Shared Localization

    ASP.NET Core MVC razor views and view models can use localized strings from a shared resource. This saves you creating many different files and duplicating translations for the different views and models. This makes it much easier to manage your translations, and also reduces the effort required to export, import the translations.

## So why use <img src="https://github.com/DreamzDevelopment/Localization/tree/master/Icons/Dreamz.Development.png" width="48" />Dev.Localization.Sql

    1. When user changes current culture to other, the localization caches rebuild [ this is available in original version ],
            in addition to that, this also rebuild client [Angular or any other Client App ] repository such as 'i18n'.
    2. One of the major change is, the local version translates according to the sentence
    3. English (US), is using as base [culture = language], so the keys are the english version of translate content
         In a result, it does not require to add database entry KEY-VALUE pair for base language [ which you can modify to your choice of language = culture ]
    4. There is single database for both Server [ AspNetCore ] and Client [ Angular or any other Client App ], central management of key-value pair

    * Microsoft SQL Server,
    * SQLite,
    * MySQL (Coming Soon),

## Compatibility

    <img src="https://github.com/DreamzDevelopment/Localization/tree/master/Icons/Dreamz.Development.png" width="48" />Dev.Localization.Sql is **compatible** with **.NET Core** and **.NET 4.6.2 ?? or greater**.
    [full dotnet framework test could not done]

## How do I get started

    Our [Sample Project](https://github.com/DreamzDevelopment/AspNetCore2Angular5) demonstrates how to use DreamzDev.Localization.Sql and gives you some starting points for learning more. Additionally, the [SHARED LOCALIZATION IN ASP.NET CORE MVC](https://damienbod.com/2017/11/01/shared-localization-in-asp-net-core-mvc/) tutorial will provide more advanced knowledge of using localization within AspNet Core app.

## Get Packages

    You can get DreamzDev.Localization.Sql by [grabbing the latest NuGet package](https://www.nuget.org/packages/DreamzDev.Localization.Sql/). If you're feeling adventurous, [continuous integration builds are on GitHub] (https://github.com/DreamzDevelopment/Localization).

    [Release notes](https://github.com/DreamzDevelopment/Localization/wiki/release-notes) are available on the wiki.

## Get Help

    **Need help with DreamzDev.Localization.Sql?** We're ready to answer your questions on [Stack Overflow](http://stackoverflow.com/questions/tagged/dreamzDev.localization). Alternatively ask a question [here](https://github.com/DreamzDevelopment/Localization/issues).

## Super-duper quick start

### Create DreamzDev.Localization.Sql database

    In your Startup.cs -> Member Variable

    ```C#
        /// <summary>
        /// SQL Server Database used for common localization of ASP.Net Core app and integrated angular app
        /// </summary>
        private bool _createNewRecordWhenLocalisedStringDoesNotExist = false;
        private bool useTypeFullNames = true;
        private bool useOnlyPropertyNames = false;
        private bool returnOnlyKeyIfNotFound = true;
    ```

    In your Startup.cs -> Constructor Method

    ```C#
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            ...
            if (env.IsDevelopment())
            {
                ...
                useTypeFullNames = false;
                returnOnlyKeyIfNotFound = false;
                _createNewRecordWhenLocalisedStringDoesNotExist = true;
                ...
            } else {
                ...
                // builder.build() is required to complete adding of AppSettings.json to the Configuration
                Configuration = builder.Build();
                _createNewRecordWhenLocalisedStringDoesNotExist = string.IsNullOrWhiteSpace(Configuration["Localization:CreateNewRecordWhenLocalisedStringDoesNotExist"]) ?
                                                                    false : bool.Parse((Configuration["Localization:CreateNewRecordWhenLocalisedStringDoesNotExist"]));
                ...
            }
            ...
        }
    ```

    In your Startup.cs -> ConfigureServices Method

    ```C#
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ...
            // No Schema
            // services.AddLocalizationSqlSchema("translations");
            services.AddDbContext<LocalizationModelContext>(options =>
                options.UseSqlite(
                    Connection.GetConnection(ConnectionType.SQLiteLocalization),
                    /// Required
                    b => b.MigrationsAssembly("NameOfApplication") // Required *****
                ),
                ServiceLifetime.Singleton
            );
            ...
            /// <summary>
            /// SQL Server Database used for common localization of ASP.Net Core app and integrated angular app
            /// </summary>
            services.AddSqlLocalization(options => 
                options.UseSettings(useTypeFullNames, useOnlyPropertyNames, returnOnlyKeyIfNotFound, _createNewRecordWhenLocalisedStringDoesNotExist)
            );
            // Language Culture Filter from URI
            // services.AddScoped<LanguageActionFilter>();

            // Configure supported cultures and localization options
            services.Configure<RequestLocalizationOptions>(options => {
                var supportedCultures = new [] {
                    new CultureInfo("en-US"),
                    new CultureInfo("bn-BD")
                };
                // State what the default culture for your application is. This will be used if no specific culture
                // can be determined for a given request.
                options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");
                // You must explicitly state which cultures your application supports.
                // These are the cultures the app supports for formatting numbers, dates, etc.
                options.SupportedCultures = supportedCultures;

                // These are the cultures the app supports for UI strings, i.e. we have localized resources for.
                options.SupportedUICultures = supportedCultures;

                // You can change which providers are configured to determine the culture for requests, or even add a custom
                // provider with your own logic. The providers will be asked in order to provide a culture for each request,
                // and the first to provide a non-null result that is in the configured supported cultures list will be used.
                // By default, the following built-in providers are configured:
                // - QueryStringRequestCultureProvider, sets culture via "culture" and "ui-culture" query string values, useful for testing
                // - CookieRequestCultureProvider, sets culture via "ASPNET_CULTURE" cookie
                // - AcceptLanguageHeaderRequestCultureProvider, sets culture via the "Accept-Language" request header
                // options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
                // {
                //  // My custom request culture logic
                //  return new ProviderCultureResult("en");
                // }));
            });
            services.Configure<SqlContextOptions>(Startup.Configuration.GetSection("SqlContextOptions"));
            ...
        }
    ```

    In your Startup.cs -> Configure Method

    ```C#
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            ...
            // Get localization information
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);
            ...
        }
    ```

    In your AppSettings.json

    ```JSON
        ...
        "Localization": {
            "CreateNewRecordWhenLocalisedStringDoesNotExist": "false" // If this is set, then the Context will add any not found localized key-value pair into database
                                                                // this ignores, application running environment, the default is only in development mode
                                                                // in development mode, this flag is ignored, and Context is automatically adding not found kay-value into database
        },
        "SqlContextOptions": {
            "SqlSchemaName": "",
            /// <summary>
            /// Connection String for Localization Model Context Database
            /// </summary>
            "ConLocalization": "Data Source=LocalizationRecords.sqlite",
            /// <summary>
            /// Replace end of sentence (full-stop) in English with local Symbol of end of Sentence
            /// </summary>
            "FullStop": "|"
        },
        ...
    ```

### Apply Localization Migrations and Update Database

    Execute following commands from Root Directory using your preferred cmd, bash or powershell
    [ dotnet ef migrations add Initial -c LocalizationModelContext -o Migrations/Localization ]
    [ dotnet ef database update -c LocalizationModelContext ]

## Project

    - [DreamzDev.Localization.Sql](https://www.nuget.org/packages/DreamzDev.Localization.Sql/) - [this repo](https://github.com/DreamzDevelopment/Localization)