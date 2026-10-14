using System;

namespace CatsAndArrows.Monetization
{
    // Issue #30: detached snapshots; only the coordinator mutates its working copy.
    public sealed class MonetizationState
    {
        public const int CurrentSchema = 1;
        public int SchemaVersion = CurrentSchema;
        public long Revision;
        public long NextBoardInstance = 1;
        public long LastCountedBoardInstance;
        public long OrdinaryBoardsCompleted;
        public long HandledProgressionMilestone;
        public AccessState Access = AccessState.Unknown;
        public string StoreNamespace = "";
        public string EvidenceReference = "";
        public long NextOperation = 1;
        public long ProcessedRewardOperation;
        public long RewardOperation;
        public long RewardOriginBoard;
        public bool PendingPurchase;
        public string PendingPurchaseReference = "";
        public string UnfinishedTransaction = "";
        public string LastFinishedTransaction = "";

        public MonetizationState Copy() => (MonetizationState)MemberwiseClone();

        public bool IsValid() => SchemaVersion == CurrentSchema && Revision >= 0 &&
            NextBoardInstance > 0 && LastCountedBoardInstance >= 0 && LastCountedBoardInstance < NextBoardInstance &&
            OrdinaryBoardsCompleted >= 0 && HandledProgressionMilestone >= 0 &&
            HandledProgressionMilestone <= OrdinaryBoardsCompleted / 4 && Enum.IsDefined(typeof(AccessState), Access) &&
            NextOperation > 0 && ProcessedRewardOperation >= 0 && ProcessedRewardOperation < NextOperation &&
            RewardOperation >= 0 && RewardOperation <= ProcessedRewardOperation && RewardOriginBoard >= 0 &&
            ((RewardOperation == 0 && RewardOriginBoard == 0) || (RewardOperation > 0 && RewardOriginBoard > 0)) &&
            ValidText(StoreNamespace) && ValidText(EvidenceReference) && ValidText(PendingPurchaseReference) &&
            ValidText(UnfinishedTransaction) && ValidText(LastFinishedTransaction);

        private static bool ValidText(string text) => text != null && text.Length <= 4096;
    }
}
