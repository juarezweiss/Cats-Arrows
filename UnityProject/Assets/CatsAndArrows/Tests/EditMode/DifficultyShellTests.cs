using System;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    // Issue #32: selection belongs to the next game, tutorial truth stays fixed Easy.
    public sealed class DifficultyShellTests
    {
        private bool hadLanguage;
        private string language;
        [SetUp] public void PreserveLanguage()
        { hadLanguage=PlayerPrefs.HasKey(LanguagePreference.Key);language=PlayerPrefs.GetString(LanguagePreference.Key); }
        [TearDown] public void RestoreLanguage()
        { if(hadLanguage)PlayerPrefs.SetString(LanguagePreference.Key,language);else PlayerPrefs.DeleteKey(LanguagePreference.Key);PlayerPrefs.Save(); }

        [TestCase("en","Easy","Medium","Hard")]
        [TestCase("pt-BR","Fácil","Médio","Difícil")]
        [TestCase("es","Fácil","Medio","Difícil")]
        public void MenuExposesAllThreeLocalizedChoicesAndSelectedState(string locale,string easy,string medium,string hard)
        {
            var shell=new GameShellView(14,locale,enableTutorial:false);
            Assert.That(shell.Q<Button>("menu-difficulty-easy").text,Is.EqualTo(easy));
            Assert.That(shell.Q<Button>("menu-difficulty-medium").text,Is.EqualTo(medium));
            Assert.That(shell.Q<Button>("menu-difficulty-hard").text,Is.EqualTo(hard));
            Assert.That(shell.Q<Button>("menu-difficulty-easy").enabledSelf,Is.False);
            shell.SelectDifficulty("medium");
            Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo("medium"));
            Assert.That(shell.Q<Button>("menu-difficulty-medium").enabledSelf,Is.False);
            Assert.That(shell.Q<Button>("menu-difficulty-easy").enabledSelf,Is.True);
        }
        [TestCase("easy",5)] [TestCase("medium",6)] [TestCase("hard",7)]
        public void SelectedDifficultyDrivesStartAndNewGameWithoutChangingTapDragOrWin(string id,int size)
        {
            var shell=new GameShellView(14,"en",enableTutorial:false);shell.SelectDifficulty(id);
            Assert.That(shell.StartNewGame(),Is.True);var session=shell.ActiveSession;
            Assert.That(session.Puzzle.Size,Is.EqualTo(size));Assert.That(shell.Loop.SessionDifficulty.Id,Is.EqualTo(id));
            Assert.That(shell.Q<Label>("board-difficulty").text,Does.Contain(size+" × "+size));
            var normal=session.Marks.Keys.First();
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.X));
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.Cat));
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.Empty));
            var row=session.Marks.Keys.Where(c=>c.Row==normal.Row).ToArray();
            Assert.That(session.ApplyDrag(row),Is.EqualTo(row.Length));
            foreach(var arrow in session.Puzzle.Arrows.Keys)Assert.That(session.Tap(arrow),Is.False);
            foreach(var fixedCell in session.ProtectedCells)Assert.That(session.Tap(fixedCell),Is.False);
            shell.ClearBoard();Assert.That(session.Marks.Values,Is.All.EqualTo(PlayerMark.Empty));
            foreach(var cat in session.Puzzle.SolutionCats){session.Tap(cat);session.Tap(cat);}
            Assert.That(session.IsCompleted,Is.True);Assert.That(session.Tap(normal),Is.False);Assert.That(session.Clear(),Is.False);
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(size));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(15));Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo(id));
        }
        [Test] public void ChoosingNextDifficultyPreservesLiveBoardAndItsHeaderAcrossMenuAndLocale()
        {
            var shell=new GameShellView(14,"en",enableTutorial:false);Assert.That(shell.StartNewGame(),Is.True);
            var first=shell.ActiveSession;shell.ShowMenu();shell.SelectDifficulty("hard");shell.SelectLanguage("es");shell.BackToGame();
            Assert.That(shell.ActiveSession,Is.SameAs(first));Assert.That(first.Puzzle.Size,Is.EqualTo(5));
            Assert.That(shell.Q<Label>("board-difficulty").text,Is.EqualTo("Fácil · 5 × 5"));
            shell.SelectDifficulty("medium");Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo("hard"),"choice is a menu-only intent");
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(7));
        }
        [TestCase("medium")] [TestCase("hard")]
        public void FirstPlayAndReplayStayTheExactFixedEasyTutorialAfterHigherSelection(string id)
        {
            var seen=false;var shell=new GameShellView(14,"en",tutorialSeen:()=>seen,markTutorialSeen:()=>seen=true);
            shell.SelectDifficulty(id);shell.StartPlaying();
            Assert.That(shell.Tutorial,Is.Not.Null);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(5));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(TutorialFixture.Seed));
            Assert.That(GenerationFingerprints.ForBoard(shell.ActiveSession.Puzzle),Is.EqualTo(GenerationFingerprints.ForBoard(TutorialFixture.CreatePuzzle())));
            Assert.That(shell.Loop.RequestedSeed,Is.Null);Assert.That(shell.Q<Label>("board-difficulty").text,Is.EqualTo("Easy · 5 × 5"));
            shell.StartTutorial();Assert.That(shell.Tutorial.Step,Is.EqualTo(TutorialStep.Welcome));
            shell.SkipTutorial();Assert.That(shell.StartNewGame(),Is.True);
            Assert.That(shell.Tutorial,Is.Null);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(id=="medium"?6:7));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(14));
        }
        [TestCase("medium")] [TestCase("hard")]
        public void FailureAndRetryRetainRequestedDifficultyAndConsumeNextSeed(string id)
        {
            var generator=new RetryGenerator();var loop=new EasyGameLoop(14,generator:generator);
            var shell=new GameShellView(14,"en",loop,enableTutorial:false);shell.SelectDifficulty(id);
            Assert.That(shell.StartNewGame(),Is.False);Assert.That(shell.Screen,Is.EqualTo("failure"));
            Assert.That(loop.SelectedDifficulty.Id,Is.EqualTo(id));Assert.That(shell.Q<Label>("board-difficulty").text,Does.StartWith(id=="medium"?"Medium":"Hard"));
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(loop.RequestedSeed,Is.EqualTo(15));
            Assert.That(loop.SessionDifficulty.Id,Is.EqualTo(id));Assert.That(generator.Requested,Is.EqualTo(new[]{id,id}));
        }
        [Test] public void UnsupportedMenuChoiceCannotChangeCurrentDifficulty()
        { var loop=new EasyGameLoop(14);Assert.Throws<ArgumentException>(()=>loop.SelectDifficulty("unknown"));Assert.That(loop.SelectedDifficulty.Id,Is.EqualTo("easy")); }
        [Test] public void WrongProfileInjectedSuccessCannotReachGameplay()
        {
            var loop=new EasyGameLoop(14,seed=>new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy,seed));
            loop.SelectDifficulty("hard");Assert.That(loop.NewGame(),Is.False);Assert.That(loop.Session,Is.Null);
        }
        private sealed class RetryGenerator:IPuzzleGenerator
        {
            public readonly System.Collections.Generic.List<string> Requested=new System.Collections.Generic.List<string>();
            public GenerationResult Generate(DifficultyProfile profile,int seed,GenerationOptions options=null,System.Threading.CancellationToken cancellationToken=default)
            { Requested.Add(profile.Id);return Requested.Count==1?null:new SeededPuzzleGenerator().Generate(profile,seed,options,cancellationToken); }
        }
    }
}
