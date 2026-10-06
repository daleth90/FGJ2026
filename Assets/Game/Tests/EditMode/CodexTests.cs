using System.Collections.Generic;
using NUnit.Framework;

namespace GrassRun.Tests
{
    public class CodexTests
    {
        [Test]
        public void FormEntryId_IsAppearanceTimesTenPlusVersion()
        {
            Assert.AreEqual(20, CodexRules.FormEntryId(CharacterAppearance.Speed, MoralityDecoration.None));
            Assert.AreEqual(21, CodexRules.FormEntryId(CharacterAppearance.Speed, MoralityDecoration.Angel));
            Assert.AreEqual(22, CodexRules.FormEntryId(CharacterAppearance.Speed, MoralityDecoration.Devil));
            Assert.AreEqual(100, CodexRules.FormEntryId(CharacterAppearance.ToughnessSpeed, MoralityDecoration.None));
        }

        [Test]
        public void FormEntryId_DefaultIsAlwaysZero()
        {
            Assert.AreEqual(0, CodexRules.FormEntryId(CharacterAppearance.Default, MoralityDecoration.None));
            Assert.AreEqual(0, CodexRules.FormEntryId(CharacterAppearance.Default, MoralityDecoration.Angel));
            Assert.AreEqual(0, CodexRules.FormEntryId(CharacterAppearance.Default, MoralityDecoration.Devil));
        }

        [Test]
        public void FormEntries_AreDefaultPlusThreeVersionsOfEachOtherAppearance()
        {
            var entries = CodexRules.FormEntries();
            Assert.AreEqual(31, entries.Count);
            Assert.AreEqual(0, entries[0].id);

            var ids = new HashSet<int>();
            int previous = -1;
            foreach (var entry in entries)
            {
                Assert.IsTrue(ids.Add(entry.id), $"重複的編號 {entry.id}");
                Assert.Greater(entry.id, previous);
                previous = entry.id;
            }
        }

        [Test]
        public void CodexFormName_AddsVersionSuffixOnlyForAngelAndDevil()
        {
            Assert.AreEqual("敏捷", GameText.CodexFormName("敏捷", MoralityDecoration.None));
            Assert.AreEqual("敏捷 天使 ver", GameText.CodexFormName("敏捷", MoralityDecoration.Angel));
            Assert.AreEqual("敏捷 惡魔 ver", GameText.CodexFormName("敏捷", MoralityDecoration.Devil));
        }

        [Test]
        public void Progress_DefaultFormIsUnlockedFromTheStart()
        {
            var progress = new CodexProgress();
            Assert.IsTrue(progress.IsFormUnlocked(CodexRules.DefaultFormId));
            Assert.IsFalse(progress.IsFormUnlocked(20));
            Assert.IsFalse(progress.IsTitleUnlocked(0));
        }

        [Test]
        public void Progress_UnlockReturnsTrueOnlyTheFirstTime()
        {
            var progress = new CodexProgress();
            Assert.IsTrue(progress.UnlockTitle(2008));
            Assert.IsFalse(progress.UnlockTitle(2008));
            Assert.IsTrue(progress.UnlockForm(21));
            Assert.IsFalse(progress.UnlockForm(21));
            Assert.IsFalse(progress.UnlockForm(CodexRules.DefaultFormId));
        }

        [Test]
        public void Progress_SerializeRoundTrip()
        {
            var progress = new CodexProgress();
            progress.UnlockTitle(0);
            progress.UnlockTitle(3020);
            progress.UnlockForm(21);
            progress.UnlockForm(92);

            var loaded = CodexProgress.Parse(progress.Serialize());

            Assert.IsTrue(loaded.IsTitleUnlocked(0));
            Assert.IsTrue(loaded.IsTitleUnlocked(3020));
            Assert.IsFalse(loaded.IsTitleUnlocked(1));
            Assert.IsTrue(loaded.IsFormUnlocked(21));
            Assert.IsTrue(loaded.IsFormUnlocked(92));
            Assert.IsTrue(loaded.IsFormUnlocked(CodexRules.DefaultFormId));
            Assert.AreEqual(progress.Serialize(), loaded.Serialize());
        }

        [Test]
        public void Progress_ParseIgnoresGarbageAndKeepsDefault()
        {
            var loaded = CodexProgress.Parse("t=1,x,3;zzz;f=;q=5");
            Assert.IsTrue(loaded.IsTitleUnlocked(1));
            Assert.IsTrue(loaded.IsTitleUnlocked(3));
            Assert.IsFalse(loaded.IsTitleUnlocked(5));
            Assert.IsTrue(loaded.IsFormUnlocked(CodexRules.DefaultFormId));

            Assert.IsTrue(CodexProgress.Parse(null).IsFormUnlocked(CodexRules.DefaultFormId));
        }

        [Test]
        public void Progress_ClearKeepsOnlyDefault()
        {
            var progress = new CodexProgress();
            progress.UnlockTitle(4);
            progress.UnlockForm(31);

            progress.Clear();

            Assert.IsFalse(progress.IsTitleUnlocked(4));
            Assert.IsFalse(progress.IsFormUnlocked(31));
            Assert.IsTrue(progress.IsFormUnlocked(CodexRules.DefaultFormId));
            Assert.AreEqual(0, progress.UnlockedTitleCount);
            Assert.AreEqual(1, progress.UnlockedFormCount);
        }
    }
}
