using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CatsAndArrows.Monetization
{
    // Pure C# adapter: one checksummed, versioned file plus previous-good backup.
    // A checksum is corruption detection, never entitlement verification.
    public sealed class FileMonetizationStateStore : IMonetizationStateStore
    {
        private const int Magic = 0x43414D53;
        private readonly string path;
        private readonly object gate = new object();
        public FileMonetizationStateStore(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !Path.IsPathRooted(absolutePath))
                throw new ArgumentException("State path must be absolute.", nameof(absolutePath));
            path = absolutePath;
        }

        public bool TryLoad(out MonetizationState state)
        {
            lock (gate) return Load(out state);
        }

        private bool Load(out MonetizationState state)
        {
            state = null;
            try
            {
                if (!File.Exists(path) && !File.Exists(path + ".bak")) { state = new MonetizationState(); return true; }
                var primary = Read(path, out var future);
                // Never roll back/overwrite a schema from a newer application.
                if (future) return false;
                state = primary ?? Read(path + ".bak", out future);
                return !future && state != null;
            }
            catch (Exception error) when (StorageError(error)) { return false; }
        }

        public bool TryCommit(MonetizationState state, long expectedRevision)
        {
            if (state == null || !state.IsValid() || state.Revision != expectedRevision + 1) return false;
            lock (gate)
            {
                var temporary = path + ".tmp." + Guid.NewGuid().ToString("N");
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(directory);
                    // Protect compare-and-replace across adapter instances/processes.
                    using var commitLock = new FileStream(path + ".lock", FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.None);
                    if (!Load(out var current) || current.Revision != expectedRevision) return false;
                    var bytes = Encode(state);
                    using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                    var primary = Read(path, out var future);
                    if (future) return false;
                    if (File.Exists(path) && primary != null)
                        File.Replace(temporary, path, path + ".bak");
                    else
                    {
                        // Preserve a corrupt primary and a known-good backup rather
                        // than replacing the backup with corrupt data.
                        if (File.Exists(path)) File.Move(path, path + ".damaged." + Guid.NewGuid().ToString("N"));
                        File.Move(temporary, path);
                    }
                    return true;
                }
                catch (Exception error) when (StorageError(error)) { return false; }
                finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception error) when (StorageError(error)) { } }
            }
        }

        private static bool StorageError(Exception error) => error is IOException || error is UnauthorizedAccessException ||
            error is ArgumentException || error is NotSupportedException || error is System.Security.SecurityException;

        private static MonetizationState Read(string filename, out bool future)
        {
            future = false;
            if (!File.Exists(filename)) return null;
            var info = new FileInfo(filename);
            if (info.Length < 40 || info.Length > 128 * 1024) return null;
            var bytes = File.ReadAllBytes(filename);
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadInt32() != Magic) return null;
            var schema = reader.ReadInt32();
            if (schema > MonetizationState.CurrentSchema) { future = true; return null; }
            if (schema != MonetizationState.CurrentSchema) return null;
            var digest = reader.ReadBytes(32);
            var payload = reader.ReadBytes((int)(stream.Length - stream.Position));
            using var sha = SHA256.Create();
            var actual = sha.ComputeHash(payload);
            for (var i = 0; i < 32; i++) if (digest[i] != actual[i]) return null;
            try
            {
                using var data = new MemoryStream(payload, false);
                using var fields = new BinaryReader(data, Encoding.UTF8);
                var state = new MonetizationState {
                    Revision = fields.ReadInt64(), NextBoardInstance = fields.ReadInt64(),
                    LastCountedBoardInstance = fields.ReadInt64(), OrdinaryBoardsCompleted = fields.ReadInt64(),
                    HandledProgressionMilestone = fields.ReadInt64(), Access = (AccessState)fields.ReadInt32(),
                    StoreNamespace = fields.ReadString(), EvidenceReference = fields.ReadString(),
                    NextOperation = fields.ReadInt64(), ProcessedRewardOperation = fields.ReadInt64(),
                    RewardOperation = fields.ReadInt64(), RewardOriginBoard = fields.ReadInt64(),
                    PendingPurchase = fields.ReadBoolean(), PendingPurchaseReference = fields.ReadString(),
                    UnfinishedTransaction = fields.ReadString(), LastFinishedTransaction = fields.ReadString()
                };
                return data.Position == data.Length && state.IsValid() ? state : null;
            }
            catch (Exception error) when (error is IOException || error is ArgumentException) { return null; }
        }

        private static byte[] Encode(MonetizationState state)
        {
            using var payload = new MemoryStream();
            using (var writer = new BinaryWriter(payload, Encoding.UTF8, true))
            {
                writer.Write(state.Revision); writer.Write(state.NextBoardInstance); writer.Write(state.LastCountedBoardInstance);
                writer.Write(state.OrdinaryBoardsCompleted); writer.Write(state.HandledProgressionMilestone);
                writer.Write((int)state.Access); writer.Write(state.StoreNamespace); writer.Write(state.EvidenceReference);
                writer.Write(state.NextOperation); writer.Write(state.ProcessedRewardOperation); writer.Write(state.RewardOperation);
                writer.Write(state.RewardOriginBoard); writer.Write(state.PendingPurchase); writer.Write(state.PendingPurchaseReference);
                writer.Write(state.UnfinishedTransaction); writer.Write(state.LastFinishedTransaction);
            }
            var content = payload.ToArray();
            using var sha = SHA256.Create();
            using var envelope = new MemoryStream();
            using (var writer = new BinaryWriter(envelope, Encoding.UTF8, true))
            { writer.Write(Magic); writer.Write(state.SchemaVersion); writer.Write(sha.ComputeHash(content)); writer.Write(content); }
            return envelope.ToArray();
        }
    }
}
