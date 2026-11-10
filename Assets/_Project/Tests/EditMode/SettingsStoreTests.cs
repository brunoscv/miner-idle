using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9, E4 (D-200): os AJUSTES no <c>settings.json</c>, à parte do save. Abrem de fábrica sem arquivo ou com um
    /// ilegível, um arquivo sem um campo abre com o valor de fábrica dele, e Apagar save não mexe no arquivo dos ajustes.
    /// </summary>
    public class SettingsStoreTests
    {
        string _dir;
        FileSaveStore _file;
        SettingsStore _settings;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleSettings_" + Guid.NewGuid().ToString("N"));
            _file = new FileSaveStore(_dir, SettingsStore.FileName);
            _settings = new SettingsStore(_file);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void NoFile_IsNull_AndTheDefaultsAreTheGameBeforeTheSettings()
        {
            Assert.That(_settings.Load(), Is.Null);
            var defaults = new SettingsData();
            Assert.That(defaults.effectsVolume, Is.EqualTo(0.8f), "o volume do SoundBoard antes dos AJUSTES");
            Assert.That(defaults.screenShake, Is.True);
            Assert.That(defaults.vibration, Is.True);
        }

        [Test]
        public void SavedSettings_ComeBack()
        {
            _settings.Save(new SettingsData { effectsVolume = 0.35f, screenShake = false, vibration = false });

            var loaded = new SettingsStore(new FileSaveStore(_dir, SettingsStore.FileName)).Load();

            Assert.That(loaded.effectsVolume, Is.EqualTo(0.35f).Within(1e-6));
            Assert.That(loaded.screenShake, Is.False);
            Assert.That(loaded.vibration, Is.False);
            Assert.That(File.Exists(Path.Combine(_dir, SettingsStore.FileName)), Is.True);
        }

        [Test]
        public void MissingField_OpensWithItsDefault()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file.FilePath, "{ \"version\": 1, \"screenShake\": false }");

            var loaded = _settings.Load();

            Assert.That(loaded.screenShake, Is.False);
            Assert.That(loaded.effectsVolume, Is.EqualTo(0.8f), "sem o campo, o de fábrica");
            Assert.That(loaded.vibration, Is.True);
        }

        [Test]
        public void UnreadableFile_IsNull_WithoutBreaking()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file.FilePath, "isto não é json {{{");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Ajustes ilegíveis"));

            Assert.That(_settings.Load(), Is.Null);
        }

        [Test]
        public void DeletingTheSave_KeepsTheSettings()
        {
            _settings.Save(new SettingsData { screenShake = false });
            var save = new SaveService(new FileSaveStore(_dir));
            save.Save(new GameState());

            save.Delete();

            Assert.That(File.Exists(Path.Combine(_dir, FileSaveStore.FileName)), Is.False);
            Assert.That(_settings.Load().screenShake, Is.False, "D-200: Apagar save não mexe nos ajustes");
        }
    }
}
