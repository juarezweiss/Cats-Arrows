using System;
using System.IO;
using CatsAndArrows.Monetization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #30: real file IO, corruption/restart/revision contract (not a JSON mirror).
    public sealed class MonetizationStateStoreTests
    {
        private string directory,path;
        private FileMonetizationStateStore store;
        [SetUp]public void SetUp(){directory=Path.Combine(Path.GetTempPath(),"cats-arrows-state-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);path=Path.Combine(directory,"state.dat");store=new FileMonetizationStateStore(path);}
        [TearDown]public void TearDown(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Test]public void FreshStoreAndPersistedFieldsRoundTripAcrossRealRestart()
        {
            Assert.That(store.TryLoad(out var state),Is.True);Assert.That(state.Revision,Is.Zero);
            state.Revision=1;state.NextBoardInstance=6;state.LastCountedBoardInstance=5;state.OrdinaryBoardsCompleted=4;state.HandledProgressionMilestone=0;
            state.Access=AccessState.Owned;state.StoreNamespace="mock";state.EvidenceReference="opaque";
            state.NextOperation=3;state.ProcessedRewardOperation=2;state.RewardOperation=2;state.RewardOriginBoard=5;
            state.PendingPurchase=true;state.PendingPurchaseReference="pending";state.UnfinishedTransaction="unfinished";
            Assert.That(store.TryCommit(state,0),Is.True);
            var restarted=new FileMonetizationStateStore(path);Assert.That(restarted.TryLoad(out var read),Is.True);
            Assert.That(read.OrdinaryBoardsCompleted,Is.EqualTo(4));Assert.That(read.HandledProgressionMilestone,Is.Zero);
            Assert.That(read.Access,Is.EqualTo(AccessState.Owned));Assert.That(read.RewardOperation,Is.EqualTo(2));
            Assert.That(read.PendingPurchase,Is.True);Assert.That(read.UnfinishedTransaction,Is.EqualTo("unfinished"));
            read.EvidenceReference="edited";Assert.That(restarted.TryLoad(out var again),Is.True);Assert.That(again.EvidenceReference,Is.EqualTo("opaque"));
        }
        [Test]public void RevisionConflictCannotOverwriteAnotherCommittedSnapshot()
        {
            store.TryLoad(out var first);var second=first.Copy();first.Revision=1;first.NextBoardInstance=2;
            Assert.That(store.TryCommit(first,0),Is.True);second.Revision=1;
            Assert.That(new FileMonetizationStateStore(path).TryCommit(second,0),Is.False);
            store.TryLoad(out var result);Assert.That(result.NextBoardInstance,Is.EqualTo(2));
        }
        [Test]public void DamagedPrimaryRecoversPreviousGoodAndPreservesBackupOnNextCommit()
        {
            store.TryLoad(out var state);state.Revision=1;Assert.That(store.TryCommit(state,0),Is.True);
            state.Revision=2;state.NextBoardInstance=2;Assert.That(store.TryCommit(state,1),Is.True);
            File.WriteAllBytes(path,new byte[]{1,2,3});Assert.That(store.TryLoad(out var recovered),Is.True);Assert.That(recovered.Revision,Is.EqualTo(1));
            recovered.Revision=2;Assert.That(store.TryCommit(recovered,1),Is.True);
            File.WriteAllBytes(path,new byte[]{0});Assert.That(store.TryLoad(out var stillGood),Is.True);Assert.That(stillGood.Revision,Is.EqualTo(1));
        }
        [Test]public void FutureSchemaPreservesPrimaryAndDoesNotDowngradeFromBackup()
        {
            store.TryLoad(out var state);state.Revision=1;store.TryCommit(state,0);state.Revision=2;store.TryCommit(state,1);
            var bytes=File.ReadAllBytes(path);Array.Copy(BitConverter.GetBytes(999),0,bytes,4,4);File.WriteAllBytes(path,bytes);
            Assert.That(store.TryLoad(out _),Is.False);state.Revision=3;Assert.That(store.TryCommit(state,2),Is.False);
            Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));
        }
        [Test]public void ChecksumDamageWithoutBackupCannotFabricateOrOverwriteOwnership()
        {
            store.TryLoad(out var state);state.Revision=1;store.TryCommit(state,0);
            var bytes=File.ReadAllBytes(path);bytes[bytes.Length-1]^=0x7f;File.WriteAllBytes(path,bytes);
            Assert.That(store.TryLoad(out _),Is.False);state.Revision=2;Assert.That(store.TryCommit(state,1),Is.False);
            Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));
        }
        [Test]public void LeftoverTemporaryFileIsNeverTreatedAsCommittedState()
        {File.WriteAllText(path+".tmp","partial");Assert.That(store.TryLoad(out var state),Is.True);Assert.That(state.Revision,Is.Zero);}
        [Test]public void NonDirectoryStorageFailureIsReturnedWithoutThrowing()
        {var blocking=Path.Combine(directory,"file");File.WriteAllText(blocking,"x");var invalid=new FileMonetizationStateStore(Path.Combine(blocking,"state"));var state=new MonetizationState{Revision=1};Assert.That(invalid.TryCommit(state,0),Is.False);}
        [Test]public void InvalidCountersAndArbitraryAccessValuesCannotCommit()
        {var state=new MonetizationState{Revision=1,OrdinaryBoardsCompleted=-1};Assert.That(store.TryCommit(state,0),Is.False);state.OrdinaryBoardsCompleted=0;state.Access=(AccessState)100;Assert.That(store.TryCommit(state,0),Is.False);}
        [Test]public void LockContenderCannotDeleteAnotherWritersTemporarySnapshot()
        {
            var sentinel=path+".tmp";File.WriteAllText(sentinel,"other writer");
            using var held=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            Assert.That(store.TryCommit(new MonetizationState{Revision=1},0),Is.False);
            Assert.That(File.ReadAllText(sentinel),Is.EqualTo("other writer"));
        }
    }
}
