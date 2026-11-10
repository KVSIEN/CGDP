using System;
using System.Collections.Generic;
using NUnit.Framework;
using CGD.DevTools;

namespace CGD.Tests
{
    public class DevConsoleTests
    {
        private static readonly string[] Categories = { "AssaultRifle", "Bow", "Crossbow", "Shotgun" };
        private static readonly string[] Tiers      = { "Common", "Rare", "Legendary" };

        private DevConsole _console;
        private string[] _lastArgs;

        [SetUp]
        public void SetUp()
        {
            _console = new DevConsole();
            _console.Register("weapon", "<category> [seed] [tier]", "Add a weapon", args => { _lastArgs = args; return "ok"; },
                example: "/weapon bow 42 rare",
                complete: (_, index) => index == 0 ? Categories : index == 2 ? Tiers : (IEnumerable<string>)null);
            _console.Register("god", "[on|off]", "Toggle god mode", _ => "god");
            _console.Register("give", "<item>", "Add an item", _ => throw new UsageException(), example: "/give scrap");
        }

        [Test]
        public void CompletesCommandNames()
        {
            Completion completion = _console.Complete("/we");

            CollectionAssert.AreEqual(new[] { "/weapon" }, completion.Candidates);
            Assert.AreEqual("/weapon ", completion.Apply(0) + " ");
        }

        [Test]
        public void ListsEveryCommandForABareSlash()
        {
            Assert.That(_console.Complete("/").Candidates, Is.SupersetOf(new[] { "/weapon", "/god", "/give", "/help" }));
        }

        [Test]
        public void CompletesTheCurrentArgument()
        {
            Completion completion = _console.Complete("/weapon ");

            CollectionAssert.AreEqual(Categories, completion.Candidates);
            Assert.AreEqual("/weapon <category> [seed] [tier]", completion.Signature);
        }

        [Test]
        public void PrefersWordsStartingWithTheTextOverOnesContainingIt()
        {
            CollectionAssert.AreEqual(new[] { "Bow" }, _console.Complete("/weapon bow").Candidates);
        }

        [Test]
        public void FallsBackToContainsWhenNothingStartsWithTheText()
        {
            CollectionAssert.AreEqual(new[] { "AssaultRifle" }, _console.Complete("/weapon rifle").Candidates);
        }

        [Test]
        public void ArgumentIndexSelectsWhichOptionsAreOffered()
        {
            CollectionAssert.AreEqual(Tiers, _console.Complete("/weapon bow 42 ").Candidates);
            Assert.IsEmpty(_console.Complete("/weapon bow ").Candidates);
        }

        [Test]
        public void ApplyReplacesOnlyTheWordBeingTyped()
        {
            Completion completion = _console.Complete("/weapon bow 42 leg");

            Assert.AreEqual("/weapon bow 42 Legendary", completion.Apply(0));
        }

        [Test]
        public void CommonPrefixIsTheSharedStartOfTheOptions()
        {
            Assert.AreEqual("/g", _console.Complete("/g").CommonPrefix);
            Assert.AreEqual("/g", new Completion("/g", 0, new[] { "/god", "/give" }, null).CommonPrefix);
        }

        [Test]
        public void NothingToCompleteWithoutTheSlashOrForUnknownCommands()
        {
            Assert.IsEmpty(_console.Complete("weapon ").Candidates);
            Assert.IsEmpty(_console.Complete("/nope ").Candidates);
            Assert.IsNull(_console.Complete("/nope ").Signature);
        }

        [Test]
        public void UnknownCommandSuggestsTheClosestOnes()
        {
            string result = _console.Execute("/wepon");

            StringAssert.Contains("Did you mean /weapon", result);
        }

        [Test]
        public void UnknownCommandWithNoNearMatchPointsToHelp()
        {
            StringAssert.Contains("/help", _console.Execute("/zzzzzzzz"));
        }

        [Test]
        public void UsageErrorShowsTheSignatureAndAnExample()
        {
            string result = _console.Execute("/give");

            StringAssert.Contains("Usage: /give <item>", result);
            StringAssert.Contains("e.g. /give scrap", result);
        }

        [Test]
        public void HelpForACommandListsExampleAndFirstArgumentOptions()
        {
            string result = _console.Execute("/help weapon");

            StringAssert.Contains("/weapon <category> [seed] [tier]", result);
            StringAssert.Contains("e.g. /weapon bow 42 rare", result);
            StringAssert.Contains("AssaultRifle, Bow, Crossbow, Shotgun", result);
        }

        [Test]
        public void HelpAcceptsASlashInFrontOfTheCommand()
        {
            StringAssert.Contains("e.g. /weapon", _console.Execute("/help /weapon"));
        }

        [Test]
        public void HelpListsEveryCommandWithItsDescription()
        {
            string result = _console.Execute("/help");

            StringAssert.Contains("/weapon <category> [seed] [tier]  — Add a weapon", result);
            StringAssert.Contains("/god [on|off]  — Toggle god mode", result);
        }

        [Test]
        public void QuotedArgumentsStayTogether()
        {
            _console.Execute("/weapon \"Assault Rifle\" 42");

            CollectionAssert.AreEqual(new[] { "Assault Rifle", "42" }, _lastArgs);
        }
    }
}
