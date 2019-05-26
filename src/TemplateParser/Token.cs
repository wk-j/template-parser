using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TemplateParser {
    public class Token {

        private readonly ILogger<Token> _logger;
        public Token(ILogger<Token> logger) {
            _logger = logger;
        }

        private string[] ParseParams(string paramString) {
            _logger.LogInformation("parse params - {0}", paramString);

            paramString = paramString.Trim();
            var paramList = new List<string>();
            var sb = new StringBuilder();
            var j = 0;
            for (; j < paramString.Length; ++j) {
                var indexParam = -1;

                if (paramString[j] == ',') {
                    paramList.Add(string.Empty);
                    continue;
                } else if (paramString[j] == '\"') {
                    var nextIndex = -1;
                    try {
                        indexParam = paramString.IndexOf('\"', j + 1);
                        nextIndex = indexParam + 1;
                    } catch { }
                    if (indexParam != -1) {
                        paramList.Add(paramString.Substring(j + 1, indexParam - (j + 1)));
                        j = nextIndex;
                        while (j < paramString.Length && paramString[j] == ' ')
                            j++;
                        continue;
                    }
                } else {
                    var nextIndex = -1;
                    try {
                        indexParam = paramString.IndexOf(',', j);
                        nextIndex = indexParam;
                    } catch { }
                    if (indexParam != -1) {
                        paramList.Add(paramString.Substring(j, indexParam - (j)));
                        j = nextIndex;
                        while (j < paramString.Length && paramString[j] == ' ')
                            j++;
                        continue;
                    }
                    paramList.Add(paramString.Substring(j));
                    break;
                }
            }
            if (paramString[paramString.Length - 1] == ',')
                paramList.Add(string.Empty);
            return paramList.ToArray();
        }

        private string ParseScript(string str, Dictionary<string, string> dict, ref int index) {
            var startIndex = index;
            var strB = new StringBuilder();
            var open = false;
            for (; index < str.Length; index++) {
                if (open == false && str[index] == '{') {
                    strB.Append('{');
                    open = true;
                } else if (open == true && str[index] == '{')
                    strB.Append(ParseScript(str, dict, ref index));
                else if (open && str[index] == '}') {
                    strB.Append('}');
                    if (startIndex != 0) return ReplaceToken(strB.ToString(), dict);
                    else {
                        open = false;
                    }
                } else
                    strB.Append(str[index]);
            }
            return ReplaceToken(strB.ToString(), dict);
        }

        public string ReplaceValue(string str, Dictionary<string, string> dict, params string[] param) {
            var index = 0;
            if (str == null) return "";
            var ret = ParseScript(str, dict, ref index);
            return ret;
        }

        private string ReplaceToken(string str, Dictionary<string, string> dict) {
            if (dict == null) dict = new Dictionary<string, string>();

            _logger.LogInformation("replace token - {0}", str);

            try {
                if (str == null) return "";
                var split = str.Split('{', '}');

                _logger.LogInformation("split - {0}", string.Join("__", split));

                if (split.Length < 3) return str;
                for (var i = 1; i < split.Length; i += 2) {
                    var function = split[i].Split(new char[] { '=', '(' }, 2, StringSplitOptions.None);
                    if (function == null || function.Length == 0) return str;
                    if (split[i].Length > function[0].Length && split[i][function[0].Length] == '(') {
                        function[function.Length - 1] = function[function.Length - 1].TrimEnd(')');
                    }
                    var parameters = new string[0];
                    var returnValue = "";
                    if (function.Length > 1) {
                        function[1] = function[1].Trim();
                        parameters = ParseParams(function[1]);
                    }

                    _logger.LogInformation("result params - {0}", string.Join("__", parameters));

                    switch (function[0].ToLower().Trim()) {
                        case "text": {
                            }
                            break;


                        case "datetimeformat": {
                                var source = ReplaceToken(parameters[0].Trim(), dict);
                                var format = ReplaceToken(parameters[1].Trim(), dict);
                                var srcCulture = ReplaceToken(parameters[2].Trim(), dict);
                                var outCulture = ReplaceToken(parameters[3].Trim(), dict);

                                var culture = CultureInfo.GetCultureInfo(outCulture);
                                var dt = DateTime.Now;
                                if (string.IsNullOrEmpty(source) == false) {
                                    try {
                                        dt = DateTime.Parse(source, CultureInfo.GetCultureInfo(srcCulture).DateTimeFormat);
                                    } catch { }
                                }
                                if (string.IsNullOrEmpty(format) == true)
                                    format = "yyyy-MM-dd HHmmss";
                                returnValue = dt.ToString(format, culture.DateTimeFormat);
                            }
                            break;

                        case "converttime": {
                                var source = ReplaceToken(parameters[0].Trim(), dict);
                                var formatSrc = ReplaceToken(parameters[1].Trim(), dict);
                                var formatDest = ReplaceToken(parameters[2].Trim(), dict);
                                //var localeSrc = Config.Locale;
                                //var localeDest = Config.Locale;
                                var localeSrc = "en-US";
                                var localeDest = "en-US";
                                if (parameters.Length > 3)
                                    localeSrc = ReplaceToken(parameters[3].Trim(), dict);
                                if (parameters.Length > 4)
                                    localeDest = ReplaceToken(parameters[4].Trim(), dict);
                                var cultureSrc = CultureInfo.GetCultureInfo(localeSrc);
                                var cultureDest = CultureInfo.GetCultureInfo(localeDest);
                                var dt = DateTime.Now;
                                if (string.IsNullOrEmpty(source) == false) {
                                    try {
                                        dt = DateTime.ParseExact(source, formatSrc, cultureSrc.DateTimeFormat);
                                    } catch { }
                                }
                                if (string.IsNullOrEmpty(formatDest) == true)
                                    formatDest = "yyyy-MM-dd HHmmss";
                                returnValue = dt.ToString(formatDest, cultureDest.DateTimeFormat);
                            }
                            break;

                        case "setgenerate":
                        case "readgenerate":
                        case "globalgenerate":
                        case "cachegenerate":
                        case "relativepath":
                        case "generate": break;

                        case "replace": {
                                if (function.Length >= 1) {
                                    var sParamSplit = parameters;
                                    if (sParamSplit.Length >= 3) {
                                        returnValue = sParamSplit[0].Replace(sParamSplit[1], sParamSplit[2]);
                                    }
                                }
                            }
                            break;



                        case "time": {
                                var culture = new CultureInfo("en-US");
                                var format = "yyyy-MM-dd";

                                if (function.Length <= 1)
                                    returnValue = "" + DateTime.Now.ToString(format, culture);
                                else {
                                    var paramSplit = parameters;
                                    if (paramSplit.Length <= 1)
                                        returnValue = "" + DateTime.Now.ToString(paramSplit[0].Trim());
                                    else if (paramSplit.Length <= 2) {
                                        DateTime dt;
                                        if (DateTime.TryParse(paramSplit[1].Trim(), out dt))
                                            returnValue = "" + dt.ToString(paramSplit[0].Trim(), culture);
                                    } else if (paramSplit.Length <= 3) {
                                        DateTime dt;

                                        var timeFormat = paramSplit[0].Trim();
                                        var timeSrc = paramSplit[1].Trim();
                                        var locale = paramSplit[2].Trim();
                                        CultureInfo cul = null;
                                        if (string.IsNullOrEmpty(locale) == false)
                                            cul = CultureInfo.GetCultureInfo(locale);

                                        if (string.IsNullOrEmpty(timeSrc) == false) {
                                            if (DateTime.TryParse(paramSplit[1].Trim(), out dt))
                                                returnValue = "" + dt.ToString(timeFormat, cul);
                                        } else
                                            returnValue = DateTime.Now.ToString(timeFormat, cul);
                                    }
                                }
                            }
                            break;


                        case "substr": {
                                if (function.Length > 1) {
                                    var variable = "";
                                    int start = 0, cnt = -1;
                                    var paramSubstr = parameters;
                                    if (paramSubstr == null) break;
                                    if (paramSubstr.Length >= 1)
                                        variable = paramSubstr[0].Trim();
                                    if (paramSubstr.Length >= 2)
                                        int.TryParse(paramSubstr[1].Trim(), out start);
                                    if (paramSubstr.Length >= 3)
                                        int.TryParse(paramSubstr[2].Trim(), out cnt);

                                    var val = variable.Trim();

                                    try {
                                        if (start >= 0 && start < val.Length && cnt == -1) {
                                            if (start < val.Length)
                                                val = val.Substring(start);
                                        } else if (start >= 0 && start < val.Length && cnt > 0) {
                                            if (start < val.Length) {
                                                if (start + cnt > val.Length)
                                                    cnt = val.Length - start;
                                                val = val.Substring(start, cnt);
                                            }
                                        }
                                    } catch (Exception ex) {
                                        _logger.LogError(ex.ToString());
                                    }

                                    returnValue = val;
                                }
                            }
                            break;

                        case "aspect": {
                                var culture = new CultureInfo("en-US");
                                foreach (var property in dict) {
                                    if (property.Key != function[1]) {
                                        continue;
                                    } else {
                                        returnValue = property.Value;
                                        _logger.LogInformation("aspect - {0}", returnValue);
                                        break;
                                    }
                                }
                            }
                            break;

                    }

                    if (returnValue == null) returnValue = string.Empty;
                    split[i] = returnValue.Trim();
                }

                var join = string.Join("", split);
                return join;

            } catch (Exception e) {
                _logger.LogError(e.ToString());
            }
            return str;
        }
    }
}
