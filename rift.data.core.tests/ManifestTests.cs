using System;
using Assets;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace rift.data.core.tests
{
    [TestFixture]
    public class ManifestTests
    {
        [Test]
        public void ManifestThrowsErrorWhenNoDirectorySet()
        {
            Assert.That(() => AssetDatabaseFactory.Manifest, Throws.Exception);
        }
    }
}
