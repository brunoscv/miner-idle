using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Persistence;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9i E4 (D-337): a versão do app vai em cada save e volta ao carregar; a comparação é por partes numéricas, e a
    /// versão vazia (um save de antes da E4) é mais velha que qualquer outra.
    /// </summary>
    public class AppVersionTests
    {
        [TestCase("0.1.0.2611181200", "0.1.0.2611191530", ExpectedResult = true)]
        [TestCase("0.1.0.2611191530", "0.1.0.2611191530", ExpectedResult = false)]
        [TestCase("0.1.0.2611191530", "0.1.0.2611181200", ExpectedResult = false)]
        [TestCase("0.1.0", "0.1.0.2611191530", ExpectedResult = true)]
        [TestCase("0.1.0.2611191530", "0.1.0", ExpectedResult = false)]
        [TestCase("0.1.9", "0.1.10", ExpectedResult = true)]
        [TestCase("0.2.0", "0.1.0.2611191530", ExpectedResult = false)]
        [TestCase("", "0.1.0", ExpectedResult = true)]
        [TestCase(null, "0.1.0", ExpectedResult = true)]
        [TestCase("0.1.0", "", ExpectedResult = false)]
        public bool IsOlder(string saved, string current) => AppVersion.IsOlder(saved, current);

        [Test]
        public void TheSave_CarriesTheAppVersion()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleAppVersion_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var economy = new EconomyRules(
                    pickaxe: new UpgradeDefinition(10, 4, 19, 2), speed: new UpgradeDefinition(15, 3, 10, 0.05),
                    mining: new UpgradeDefinition(10, 1.2, 100, 0.01), doublePickaxe: new UpgradeDefinition(500, 1, 1, 2),
                    boots: new UpgradeDefinition(10, 2, 10, 0.1), minHitInterval: 0.5);

                var fresh = new SaveService(store);
                fresh.Load(economy, out var status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.NewGame));
                Assert.That(fresh.LoadedAppVersion, Is.Null, "sem save, sem versão");

                new SaveService(store) { AppVersion = "0.1.0.2611191530" }.Save(new GameState());
                var reopened = new SaveService(store);
                reopened.Load(economy, out status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(reopened.LoadedAppVersion, Is.EqualTo("0.1.0.2611191530"));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }
    }
}
