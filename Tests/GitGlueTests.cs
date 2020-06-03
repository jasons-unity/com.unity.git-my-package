using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    //TODO: Add Tests
    public class GitMyPackageTests
    {
        [Test]
        public void NewTestScriptSimplePasses()
        {
            Assert.IsTrue(true);
        }

        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            yield return null;
            Assert.IsTrue(true);
        }
    }
}
