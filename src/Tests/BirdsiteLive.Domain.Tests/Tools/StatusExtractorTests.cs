﻿using System;
using System.Linq;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Domain.Tools;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Domain.Tests.Tools
{
    [TestClass]
    public class StatusExtractorTests
    {
        private readonly InstanceSettings _settings;

        #region Ctor
        public StatusExtractorTests()
        {
            _settings = new InstanceSettings
            {
                Domain = "domain.name"
            };
        }
        #endregion

        [TestMethod]
        public void Extract_ReturnLines_Test()
        {
            #region Stubs
            var message = "Bla.\n\n@Mention blo. https://t.co/pgtrJi9600";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.IsTrue(result.content.Contains("Bla."));
            Assert.IsTrue(result.content.Contains("</p><p>"));
            #endregion
        }

        [TestMethod]
        public void Extract_ReturnSingleLines_Test()
        {
            #region Stubs
            var message = "Bla.\n@Mention blo. https://t.co/pgtrJi9600";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.IsTrue(result.content.Contains("Bla."));
            Assert.IsTrue(result.content.Contains("<br/>"));
            #endregion
        }

        [TestMethod]
        public void Extract_FormatUrl_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}https://t.co/L8BpyHgg25";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(0, result.tags.Length);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://t.co/L8BpyHgg25"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://</span><span class=""ellipsis"">t.co/L8BpyHgg25</span><span class=""invisible""></span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_FormatUrl_Long_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}https://www.eff.org/deeplinks/2020/07/pact-act-not-solution-problem-harmful-online-content";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(0, result.tags.Length);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://www.eff.org/deeplinks/2020/07/pact-act-not-solution-problem-harmful-online-content"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://www.</span><span class=""ellipsis"">eff.org/deeplinks/2020/07/pact</span><span class=""invisible"">-act-not-solution-problem-harmful-online-content</span></a>"));
            #endregion
        }
        [TestMethod]
        public void Extract_FormatUrl_Long2_Test()
        {
            #region Stubs
            var message = $"https://twitterisgoinggreat.com/#twitters-first-dollar15bn-interest-payment-could-be-due-in-two-weeks";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(result.content, @"<a href=""https://twitterisgoinggreat.com/#twitters-first-dollar15bn-interest-payment-could-be-due-in-two-weeks"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://</span><span class=""ellipsis"">twitterisgoinggreat.com/#twitt</span><span class=""invisible"">ers-first-dollar15bn-interest-payment-could-be-due-in-two-weeks</span></a>");
            Assert.AreEqual(0, result.tags.Length);

            #endregion
        }
        
        [TestMethod]
        public void Extract_FormatUrl_Long3_Test()
        {
            #region Stubs
            var message = $"https://domain.name/@WeekInEthNews/1668684659855880193";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(result.content, @"<a href=""https://domain.name/@WeekInEthNews/1668684659855880193"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://</span><span class=""ellipsis"">domain.name/@WeekInEthNews/166</span><span class=""invisible"">8684659855880193</span></a>");
            Assert.AreEqual(0, result.tags.Length);

            #endregion
        }

        [TestMethod]
        public void Extract_FormatUrl_Exact_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}https://www.eff.org/deeplinks/2020/07/pact";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(0, result.tags.Length);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://www.eff.org/deeplinks/2020/07/pact"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://www.</span><span class=""ellipsis"">eff.org/deeplinks/2020/07/pact</span><span class=""invisible""></span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_MultiUrls_Test()
        {
            #region Stubs
            var message = $"https://t.co/L8BpyHgg25 Bla!{Environment.NewLine}https://www.eff.org/deeplinks/2020/07/pact-act-not-solution-problem-harmful-online-content";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(0, result.tags.Length);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://t.co/L8BpyHgg25"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://</span><span class=""ellipsis"">t.co/L8BpyHgg25</span><span class=""invisible""></span></a>"));

            Assert.IsTrue(result.content.Contains(@"<a href=""https://www.eff.org/deeplinks/2020/07/pact-act-not-solution-problem-harmful-online-content"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">https://www.</span><span class=""ellipsis"">eff.org/deeplinks/2020/07/pact</span><span class=""invisible"">-act-not-solution-problem-harmful-online-content</span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SmallUrl_Test()
        {
            #region Stubs
            var message = @"🚀 test http://GOV.UK date 🎉 data http://GOV.UK woopsi.";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            Assert.AreEqual(@"🚀 test <a href=""http://GOV.UK"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">http://</span><span class=""ellipsis"">GOV.UK</span><span class=""invisible""></span></a> date 🎉 data <a href=""http://GOV.UK"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">http://</span><span class=""ellipsis"">GOV.UK</span><span class=""invisible""></span></a> woopsi.", result.content);
            #endregion
        }

        [TestMethod]
        public void Extract_SmallUrl_2_Test()
        {
            #region Stubs
            var message = @"🚀http://GOV.UK";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            Assert.AreEqual(@"🚀<a href=""http://GOV.UK"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">http://</span><span class=""ellipsis"">GOV.UK</span><span class=""invisible""></span></a>", result.content);
            #endregion
        }

        [TestMethod]
        public void Extract_SmallUrl_3_Test()
        {
            #region Stubs
            var message = @"🚀http://GOV.UK.";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            Assert.AreEqual(@"🚀<a href=""http://GOV.UK"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">http://</span><span class=""ellipsis"">GOV.UK</span><span class=""invisible""></span></a>.", result.content);
            #endregion
        }

        [TestMethod]
        public void Extract_UrlRegexChars_Test()
        {
            #region Stubs
            var message = @"🐣 juniors & tech(http://tech.guru maker)";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            Assert.AreEqual(@"🐣 juniors & tech(<a href=""http://tech.guru"" rel=""nofollow noopener noreferrer"" target=""_blank""><span class=""invisible"">http://</span><span class=""ellipsis"">tech.guru</span><span class=""invisible""></span></a> maker)", result.content);
            #endregion
        }

        [TestMethod]
        public void Extract_SingleHashTag_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}#mytag";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("#mytag", result.tags.First().name);
            Assert.AreEqual("Hashtag", result.tags.First().type);
            Assert.AreEqual("https://domain.name/tags/mytag", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag"" class=""mention hashtag"" rel=""tag"">#<span>mytag</span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleHashTag_AtStart_Test()
        {
            #region Stubs
            var message = "#mytag Bla!";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("#mytag", result.tags.First().name);
            Assert.AreEqual("Hashtag", result.tags.First().type);
            Assert.AreEqual("https://domain.name/tags/mytag", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag"" class=""mention hashtag"" rel=""tag"">#<span>mytag</span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleHashTag_SpecialChar_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}#COVID_19";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("#COVID_19", result.tags.First().name);
            Assert.AreEqual("Hashtag", result.tags.First().type);
            Assert.AreEqual("https://domain.name/tags/COVID_19", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/COVID_19"" class=""mention hashtag"" rel=""tag"">#<span>COVID_19</span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_MultiHashTags_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}#mytag #mytag2 #mytag3{Environment.NewLine}Test #bal Test";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(4, result.tags.Length);
            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag"" class=""mention hashtag"" rel=""tag"">#<span>mytag</span></a>"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag2"" class=""mention hashtag"" rel=""tag"">#<span>mytag2</span></a>"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag3"" class=""mention hashtag"" rel=""tag"">#<span>mytag3</span></a>"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/bal"" class=""mention hashtag"" rel=""tag"">#<span>bal</span></a>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}@mynickname";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@mynickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/mynickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            #endregion
        }
        [TestMethod]
        public void Extract_TagsWithPunctuations_Test()
        {
            #region Stubs
            var message = $"this: @amberfloio. @VP—and @StellantisNA’s";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(3, result.tags.Length);

            Assert.AreEqual("@vp", result.tags[0].name);
            Assert.AreEqual("Mention", result.tags[0].type);
            Assert.AreEqual("https://domain.name/users/vp", result.tags[0].href);

            Assert.AreEqual("@amberfloio", result.tags[1].name);
            Assert.AreEqual("Mention", result.tags[1].type);
            Assert.AreEqual("https://domain.name/users/amberfloio", result.tags[1].href);


            Assert.AreEqual("@stellantisna", result.tags.Last().name);
            Assert.AreEqual("Mention", result.tags.Last().type);
            Assert.AreEqual("https://domain.name/users/stellantisna", result.tags.Last().href);

            Assert.IsTrue(result.content.Contains("this:"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/amberfloio"" class=""u-url mention"">@<span>amberfloio</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_MultiMentionTag_MultiOccurrence_Test()
        {
            #region Stubs
            var message = $"[RT @yamenbousrih]{Environment.NewLine}@KiwixOffline @photos_floues Bla. Cc @Pyb75 @photos_floues @KiwixOffline";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(4, result.tags.Length);
            Assert.AreEqual("Mention", result.tags.First().type);

            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/photos_floues"" class=""u-url mention"">@<span>photos_floues</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/kiwixoffline"" class=""u-url mention"">@<span>kiwixoffline</span></a></span> <span class=""h-card""><a href=""https://domain.name/users/photos_floues"" class=""u-url mention"">@<span>photos_floues</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"Cc <span class=""h-card""><a href=""https://domain.name/users/pyb75"" class=""u-url mention"">@<span>pyb75</span></a></span> <span class=""h-card""><a href=""https://domain.name/users/photos_floues"" class=""u-url mention"">@<span>photos_floues</span></a></span> <span class=""h-card""><a href=""https://domain.name/users/kiwixoffline"" class=""u-url mention"">@<span>kiwixoffline</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_RT_Test()
        {
            #region Stubs
            var message = $"[RT @mynickname]{Environment.NewLine}Bla!";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@mynickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/mynickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_Dot_Test()
        {
            #region Stubs
            var message = $".@mynickname Bla!{Environment.NewLine}Blo";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@mynickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/mynickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains("Blo"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_SpecialChar_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}@my___nickname";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@my___nickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/my___nickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/my___nickname"" class=""u-url mention"">@<span>my___nickname</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_SpecialChar_Test2()
        {
            #region Stubs
            var message = $"Bla! @my___nickname's thing";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@my___nickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/my___nickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/my___nickname"" class=""u-url mention"">@<span>my___nickname</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_SingleMentionTag_AtStart_Test()
        {
            #region Stubs
            var message = $"@myNickName Bla!";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.AreEqual("@mynickname", result.tags.First().name);
            Assert.AreEqual("Mention", result.tags.First().type);
            Assert.AreEqual("https://domain.name/users/mynickname", result.tags.First().href);

            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            #endregion
        }
        
        [TestMethod]
        public void Extract_MultiMentionTag_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}@mynickname⁠ @mynickname2 @mynickname3{Environment.NewLine}Test @dada Test";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(4, result.tags.Length);
            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname2"" class=""u-url mention"">@<span>mynickname2</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname3"" class=""u-url mention"">@<span>mynickname3</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/dada"" class=""u-url mention"">@<span>dada</span></a></span>"));
            #endregion
        }

        [TestMethod]
        public void Extract_HeterogeneousTag_Test()
        {
            #region Stubs
            var message = $"Bla!{Environment.NewLine}@mynickname⁠ #mytag2 @mynickname3{Environment.NewLine}Test @dada #dada Test";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(5, result.tags.Length);
            Assert.IsTrue(result.content.Contains("Bla!"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/mytag2"" class=""mention hashtag"" rel=""tag"">#<span>mytag2</span></a>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/mynickname3"" class=""u-url mention"">@<span>mynickname3</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<span class=""h-card""><a href=""https://domain.name/users/dada"" class=""u-url mention"">@<span>dada</span></a></span>"));
            Assert.IsTrue(result.content.Contains(@"<a href=""https://domain.name/tags/dada"" class=""mention hashtag"" rel=""tag"">#<span>dada</span></a>"));
            #endregion
        }
        
        [TestMethod]
        public void Extract_Emoji_Test()
        {
            #region Stubs
            var message = $"😤 @mynickname 😎😍🤗🤩😘";
            //var message = $"tests@mynickname";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.IsTrue(result.content.Contains(
                @"😤 <span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span>"));

            Assert.IsTrue(result.content.Contains(@"😎😍🤗🤩😘"));
            #endregion
        }

        [Ignore]
        [TestMethod]
        public void Extract_Parenthesis_Test()
        {
            #region Stubs
            var message = $"bla (@mynickname test)";
            //var message = $"tests@mynickname";
            #endregion

            #region Mocks
            var logger = new Mock<ILogger<StatusExtractor>>();
            #endregion

            var service = new StatusExtractor(_settings, logger.Object);
            var result = service.Extract(message);

            #region Validations
            logger.VerifyAll();
            Assert.AreEqual(1, result.tags.Length);
            Assert.IsTrue(result.content.Equals(@"bla (<span class=""h-card""><a href=""https://domain.name/users/mynickname"" class=""u-url mention"">@<span>mynickname</span></a></span> test)"));
            #endregion
        }
    }
}