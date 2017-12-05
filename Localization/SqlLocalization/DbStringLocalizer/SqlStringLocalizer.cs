using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Localization.SqlLocalizer.DbStringLocalizer
{
    public class SqlStringLocalizer : IStringLocalizer
    {
        private readonly Dictionary<string, string> _localizations;
        private readonly DevelopmentSetup _developmentSetup;
        private readonly string _resourceKey;
        private bool _returnKeyOnlyIfNotFound;
        private bool _createNewRecordWhenLocalisedStringDoesNotExist;
        private string _fullStop = ".";

        public SqlStringLocalizer(Dictionary<string, string> localizations, DevelopmentSetup developmentSetup,
            string resourceKey, bool returnKeyOnlyIfNotFound, bool createNewRecordWhenLocalisedStringDoesNotExist)
        {
            _localizations = localizations;
            _developmentSetup = developmentSetup;
            _fullStop = developmentSetup?._sqlContextOptions?.Value?.FullStop ?? ".";
            _resourceKey = resourceKey;
            _returnKeyOnlyIfNotFound = returnKeyOnlyIfNotFound;
            _createNewRecordWhenLocalisedStringDoesNotExist = createNewRecordWhenLocalisedStringDoesNotExist;
        }
        public LocalizedString this[string name]
        {
            get
            {
                bool notSucceed;
                var text = GetText(name, out notSucceed);
                return new LocalizedString(name, text, notSucceed);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {                
                return this[name];
            }
        }
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
            
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetText(string key,out bool notSucceed)
        {

#if NET451
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();
#elif NET46
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();
#else
            var culture = CultureInfo.CurrentCulture.ToString();
#endif
            KeyValuePair<string,bool>[] _values = PurifyTranslate(key, culture);
            string _result = "";
            string result;
            int countNotFound = 0;
            if(_values != null && _values.Length > 0) 
            {
                for (int i = 0; i < _values.Length; i++)
                {
                    if(_values[i].Value == true && !string.IsNullOrWhiteSpace(_values[i].Key) ) {
                        string computedKey = $"{_values[i].Key}.{culture}";
                        if (_localizations.TryGetValue(computedKey, out result))
                        {
                            // notSucceed = false;
                            _result = _result + result;
                        }
                        else
                        {
                            // notSucceed = true;
                            countNotFound++;
                            /// <summary>
                            /// Add non-existed key-value if only in development environment and not to add if default Culture ('En-US')
                            /// </summary>
                            if (_createNewRecordWhenLocalisedStringDoesNotExist && (culture != "en-US") )
                            {
                                _developmentSetup.AddNewLocalizedItem(_values[i].Key, culture, _resourceKey);

#if NET_STANDARD
                                _localizations.Add(computedKey, computedKey);
#elif NET_CORE_11
                                _localizations.Add(computedKey, computedKey);
#else 
                                _localizations.TryAdd(computedKey, computedKey);
#endif
                                _result = _result + computedKey;
                            } 
                            else if (_returnKeyOnlyIfNotFound)
                            {
                                _result = _result + _values[i].Key;
                            } 
                            else {
                                _result = _result + _resourceKey + "." + computedKey;
                            }
                        }

                    } else {
                        _result = _result + _values[i].Key;
                    }
                }
            }
            notSucceed = countNotFound > 0;
            return _result;
        }
        // Because of dissimilarity of Sentence Structure for different language,
        // best practice to translate the sentence until break, such breaks are ('.', ',', '!' ) etc.
        private KeyValuePair<string,bool>[] PurifyTranslate(string key, string culture)
        {
            string _value = (string)key;
            int length = _value.Length;
            // Key value pair optimize for Translation requirement
            KeyValuePair<string,bool>[] _values = new KeyValuePair<string,bool>[length];
            char[] stringBuilder = new char[length];

            // var prevdash: boolean = false;
            char c = char.Parse(" ");
            char prevC;
            char nextC;
            int j = 0;
            int k = 0;
            // I interpreted @CodeInChaos and Ben's notes as follows:

            // fixed (char* pString = longString) {
            //     char* pChar = pString;
            //     for (int i = 0; i < strLength; i++) {
            //         c = *pChar ;
            //         pChar++;
            //     }
            // }
                for (var i = 0; i < _value.Length; i++) 
                {
                    prevC = c;
                    c = _value[i];
                    nextC = i == (_value.Length - 1) ? ' ' : _value[i + 1]; // Set implicit ' ', if end of sentence
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || 
                        (c == '\'') || (c == '-') || (c == '/') || (c == '(') || (c == ')') || 
                        (c == ' ' && !(prevC == '!' || prevC == '.' || prevC == ',')) )
                    {
                        stringBuilder[k] = c;
                        k++;
                        // prevdash = false;
                    }
                    else if (c == '.' || c == ',' || c == '!' || c == '"' || (c == ' ' && nextC == ' ') ) { // End of a sentence, or breaks
                        if(stringBuilder.Length > 0) {
                            string res = "";
                            for (int l = 0; l < k; l++)
                            {
                                res = res + stringBuilder[l];
                            }
                            _values[j] = new KeyValuePair<string,bool>( res, true); // string.Join(string.Empty, stringBuilder)
                            j++;
                        }
                        if(culture == "bn-BD" && c == '.' && nextC == ' ') {
                            /// <summary>
                            /// AppSettings.json in "SqlContextOptions" section, change with the char is used to end of sentence in your locale for full-stop (.)
                            /// </summary>
                            _values[j] = new KeyValuePair<string,bool>( _fullStop ?? ".", false);
                            j++;
                        } else {
                            _values[j] = new KeyValuePair<string,bool>(c.ToString(), false);
                            j++;
                        }
                        stringBuilder = new char[length]; // Reset string builder
                        k = 0;
                        // prevdash = false;
                    }
                    // else if (c >= '0' && c <= '9') // Numbers are translated for each digit
                    // {
                    //     stringBuilder.length > 0 ? 
                    //         _values.push(new KeyValuePair<string,boolean>(<string>''.concat.apply('', stringBuilder), true)) : '';
                    //         stringBuilder = []; // Reset string builder
                    //         _values.push(new KeyValuePair<string,boolean>(<string>c, true));
                    //         // prevdash = false;
                    // } 
                    else {
                        // else if(c) {
                        // stringBuilder.length > 0 ? 
                        // _values.push(new KeyValuePair<string,boolean>(<string>''.concat.apply('', stringBuilder), true)) : '';
                        // stringBuilder = []; // Reset string builder
                        // const $safeHtml = $('<span [data-safe]="' + super.transform(c) +'"></span>');
                        //     console.log($safeHtml);

                        _values[j] = new KeyValuePair<string,bool>(c.ToString(), false);
                        j++;
                        // if(prevdash && c == ' ') { // if already a dash(space) applied and current char is ' '/dash
                            
                        // } else if(c == ' ') { // A space/dash applied before
                        //     _values.push(new KeyValuePair<string,boolean>(<string>c, false));
                        //     prevdash = true;
                        // } else { // Other type of char
                        //     _values.push(new KeyValuePair<string,boolean>(<string>c, false));
                        //     prevdash = false;
                        // }
                    } 
                    length--;
                }
                if(stringBuilder.Length > 0) {
                    string res = "";
                    for (int l = 0; l < k; l++)
                    {
                        res = res + stringBuilder[l];
                    }
                    _values[j] = new KeyValuePair<string,bool>(res, true);
                    j++;
                }
                stringBuilder = new char[0];
                
            return _values;
        }
            

    }
}
