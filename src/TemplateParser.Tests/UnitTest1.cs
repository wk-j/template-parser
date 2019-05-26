using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace TemplateParser.Tests
{
    public class UnitTest1
    {
        private Token CreateToken()
        {
            var loggerFactory = new LoggerFactory().AddConsole();
            var logger = loggerFactory.CreateLogger<Token>();
            var token = new Token(logger);
            return token;
        }

        [Fact]
        public void Test1()
        {
            var token = CreateToken();

            var dict = new Dictionary<string, string>() {
                { "DoHome:OwnerId", "wk-j" },
                { "DoHome:Branch", "bcircle" }
            };

            var input = "{aspect(DoHome:OwnerId)} {aspect(DoHome:Branch)} {aspect(DoHome:TitleDeedsNo)} {aspect(DoHome:initials)}";

            var result = token.ReplaceValue(input, dict);

            Assert.True(result.Contains("wk-j"));
            Assert.True(result.Contains("bcircle"));
        }

        [Fact]
        public void Test2()
        {
            var dict = new Dictionary<string, string>() {
                { "CustomProperty:DocumentDate", "2019-10-10" },
                { "CustomProperty:Category", "CAT"},
                { "CustomProperty:DocumentType", "TYPE"},
                { "DoHome:Branch", "BB"},
                { "CustomProperty:Preference", "aaaaaaaaaaaaaaaaaaaaaaaaa" }
            };

            var input = "/DoHome/เอกสารจากระบบ/{aspect(CustomProperty:Category)}/{aspect(CustomProperty:DocumentType)}/{aspect(DoHome:Branch)}/{substr({aspect(CustomProperty:DocumentDate)},0,4)}/{substr({aspect(CustomProperty:DocumentDate)} ,5,2)}";
            var token = CreateToken();
            var result = token.ReplaceValue(input, dict);

            Assert.Contains("CAT", result);
        }

        [Fact]
        public void Test3()
        {
            var dict = new Dictionary<string, string>() { };
            var input = "PACKING DETAIL-{time(yyMM)}-{aspect(CustomProperty:Reference)}";
            var token = CreateToken();
            var result = token.ReplaceValue(input, dict);
            Assert.Contains("19", result);
        }
    }
}
