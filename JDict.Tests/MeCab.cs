﻿using System.Collections.Generic;
using System.Linq;
using DidacticalEnigma.Core.Models.LanguageService;
using DidacticalEnigma.Core.Utils;
using NMeCab;
using NUnit.Framework;
using Optional;

namespace JDict.Tests
{
    [TestFixture]
    class MeCab
    {
        private static readonly TestCaseData[] Test =
        {
            new TestCaseData("これは俺", new[]
            {
                new DummyEntry
                {
                    OriginalForm = "これ",
                    NotInflected = "これ",
                    Pronunciation = "コレ",
                    Reading = "コレ"
                },
                new DummyEntry
                {
                    OriginalForm = "は",
                    NotInflected = "は",
                    Pronunciation = "ワ",
                    Reading = "ハ"
                },
                new DummyEntry
                {
                    OriginalForm = "俺",
                    NotInflected = "俺",
                    Pronunciation = "オレ",
                    Reading = "オレ"
                }
            }),
            new TestCaseData("楽しかった", new[]
            {
                new DummyEntry
                {
                    OriginalForm = "楽しかっ",
                    NotInflected = "楽しい",
                    Pronunciation = "タノシカッ",
                    Reading = "タノシカッ"
                },
                new DummyEntry
                {
                    OriginalForm = "た",
                    NotInflected = "た",
                    Pronunciation = "タ",
                    Reading = "タ"
                }
            }), 
        };

        [TestCaseSource(nameof(Test))]
        public void BasicCompatibility(string sentence, IEnumerable<IEntry> expectedEntries)
        {
            var ipadicEntries = ipadicMecab.ParseToEntries(sentence).Where(e => e.IsRegular);
            var unidicEntries = unidicMecab.ParseToEntries(sentence).Where(e => e.IsRegular);
            // this is to make test cases fail in case the number of expecteds is less than the number of actuals
            var nullDummyEntry = new DummyEntry();
            foreach (var (i, u, e) in ipadicEntries.Zip(unidicEntries, expectedEntries.Concat(DidacticalEnigma.Core.Utils.EnumerableExt.Repeat(nullDummyEntry))))
            {
                //Assert.AreEqual(e.ConjugatedForm, i.ConjugatedForm);
                //Assert.AreEqual(e.Inflection, i.Inflection);
                Assert.AreEqual(e.OriginalForm, i.OriginalForm);
                //Assert.AreEqual(e.PartOfSpeech, i.PartOfSpeech);
                Assert.AreEqual(e.Pronunciation, i.Pronunciation);
                Assert.AreEqual(e.Reading, i.Reading);
                Assert.AreEqual(e.NotInflected, i.NotInflected);

                //Assert.AreEqual(e.ConjugatedForm, u.ConjugatedForm);
                //Assert.AreEqual(e.Inflection, u.Inflection);
                Assert.AreEqual(e.OriginalForm, u.OriginalForm);
                //Assert.AreEqual(e.PartOfSpeech, u.PartOfSpeech);
                Assert.AreEqual(e.Pronunciation, u.Pronunciation);
                Assert.AreEqual(e.Reading, u.Reading);
                Assert.AreEqual(e.NotInflected, u.NotInflected);
            }
        }

        private static IMorphologicalAnalyzer<IEntry> ipadicMecab;

        private static IMorphologicalAnalyzer<IEntry> unidicMecab;

        [OneTimeSetUp]
        public void SetUp()
        {
            ipadicMecab = new MeCabIpadic(new MeCabParam
            {
                DicDir = TestDataPaths.Ipadic
            });
            unidicMecab = new MeCabUnidic(new MeCabParam
            {
                DicDir = TestDataPaths.Unidic
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            ipadicMecab.Dispose();
            unidicMecab.Dispose();
        }
    }

    public class DummyEntry : IEntry
    {
        public string ConjugatedForm { get; set; }
        public string Inflection { get; set; }
        public bool? IsIndependent { get; set; }
        public bool IsRegular { get; set; } = true;
        public string OriginalForm { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }
        public IEnumerable<PartOfSpeechInfo> PartOfSpeechInfo { get; set; }
        public IEnumerable<string> PartOfSpeechSections { get; set; }
        public string Pronunciation { get; set; }
        public string Reading { get; set; }
        public string NotInflected { get; set; }
        public Option<EdictType> Type { get; set; }
    }
}
