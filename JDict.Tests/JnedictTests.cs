﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DidacticalEnigma.Core.Models.LanguageService;
using NMeCab;
using NUnit.Framework;

namespace JDict.Tests
{
    [TestFixture]
    class JnedictTests
    {
        private static Jnedict jnedict;

        [OneTimeSetUp]
        public void SetUp()
        {
            jnedict = JDict.Jnedict.Create(TestDataPaths.JMnedict, TestDataPaths.JMnedictCache);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            jnedict.Dispose();
        }

        [Test]
        public void LookupBasic()
        {
            var entries = jnedict.Lookup("南") ?? Enumerable.Empty<JnedictEntry>();
            Assert.True(entries.Any(e =>
                e.Reading.Contains("みなみ") &&
                e.Translation.Any(t => t.Type.Contains(JnedictType.fem) && t.Translation.Contains("Minami"))));
        }
    }
}
