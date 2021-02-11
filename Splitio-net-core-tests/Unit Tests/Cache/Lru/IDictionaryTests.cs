﻿/*
https://github.com/mwdavis84/LruCacheNet
Copyright (c) 2018 Mark Davis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Cache.Lru;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache.Lru
{
    [TestClass]
    public class IDictionaryTests
    {
        /// <summary>
        /// Tests that a handful of IDictionary interface methods work
        /// </summary>
        [TestMethod, TestCategory("IDictionary")]
        public void DictionaryTests()
        {
            IDictionary<int, int> data = new LruCache<int, int>(10);
            data[0] = 1;
            Assert.AreEqual(1, data.Count);
            Assert.AreEqual(1, data[0]);
            Assert.AreEqual(1, data.Keys.Count);
            Assert.AreEqual(1, data.Values.Count);
        }
    }
}
