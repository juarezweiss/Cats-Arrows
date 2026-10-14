# Monetization architecture

Design task: [#29](https://github.com/juarezweiss/Cats-Arrows/issues/29), owner
requirements dated 2026-10-14. Decision:
[ADR 0002](decisions/0002-provider-agnostic-monetization.md).

## Scope and future policy

Architecture and documented policy approved by the owner on 2026-10-14 after
review of #29. The owner subsequently authorized SDK-free implementation and a
PR to develop in [#30](https://github.com/juarezweiss/Cats-Arrows/issues/30).
Current default remains free with direct unlimited hints, no monetization SDK
and no change to #21 sequencing. Real integration and activation require separate
tracked work. No provider, price, purchase, store configuration, SDK, production
traffic or release is authorized. This replaces
the earlier future suggestion of rewarded ads for enhanced hints; it does not
silently change current ad-free gameplay.

When enabled:

- Progression interstitial opportunity every fourth completed ordinary board.
- Unowned button reads **Hint (ads)**; a confirmed rewarded ad authorizes one
  existing clue-derived hint. No daily hint cap.
- Main menu offers non-consumable **Remove All Ads**, covering progression and
  hint ads, plus a separate **Restore Purchases** action.
- Owned button reads **Hint** and applies directly, including offline.
- Generation, clues, marks and exact-cat victory remain unchanged. Puzzles never
  require a hint. Ads never interrupt active deduction or trigger automatically
  at victory, launch or resume. All labels/statuses use en/pt-BR/es resources.

## Boundaries

### SDK-free implementation (#30)

`CatsAndArrows.Monetization` implements the coordinator, typed contracts, memory
and atomic file stores plus unavailable provider defaults. `GameSession.PrepareHint`
and `MarkRevision` provide a nonmutating eligibility/session seam. Shell injects
the coordinator; ordinary completions and new-board transitions are recorded
independently of presentation. The purchase adapter owns cached-evidence validation
through `VerifyCachedOwnership`; a separate verifier interface is unnecessary in
this first implementation. Callback results run on the application thread and
normalized Closed means native presentation ended and reward callbacks settled.

The file adapter uses a version-1 binary envelope with SHA-256 corruption checks,
atomic replacement, previous-good backup, exclusive commit lock and per-attempt
temporary files. Future schemas are preserved; no earlier monetization schema
exists to migrate. Reference validation is provider-owned, not checksum-owned.

Default composition injects no enabled coordinator. `--monetization-mock=1` is
compiled only in Editor/development review builds. It uses simulated half-second
ads and a separate no-charge mock store ledger under
`Application.persistentDataPath/monetization-mock-review`; state is `state.bin` and
ownership ledger is `mock-store.txt`. Mock ownership never enables a real store
entitlement. The localized menu identifies the simulation. No real ads/network,
store products, prices, SDKs or analytics events are installed. Restore remains
available during local save failure; its in-process request ID is not a store
transaction identity and may proceed without a durable local operation commit.

Mounted development-review evidence: [main menu and restore](evidence/issue30/unowned-menu-es430.png),
[rewarded Hint label](evidence/issue30/hint-ads-pt390.png), and
[direct owned Hint](evidence/issue30/owned-hint-en320.png). Exact validation is in
[TESTING.md](TESTING.md#issue-29-monetization-design-test-plan); these are simulations.

| Layer | Responsibility |
| --- | --- |
| Core/generator | Immutable truth, solvability and seeded generation; no commercial dependency |
| Gameplay/GameSession | Marks, protected cells, hint preparation/application, immediate win; no SDK or payment eligibility |
| Pure-C# monetization policy | Cadence and access decisions from explicit state |
| Application coordinators | Completion recording, serialized ad/purchase operations, reward authorization and durable commits |
| UI/GameShellView | Localized labels, intents, bounded status and input suspension; no receipt trust or durable counter ownership |
| Persistence adapter | Versioned atomic storage, migration/recovery; no policy |
| Ad/purchase adapters | Provider lifecycle and verified normalized results; never mutate GameSession |

Planned `CatsAndArrows.Monetization` pure-C# assembly may reference Gameplay for the
hint seam; Core/Gameplay never reference Monetization. UI references application
contracts. Only provider assemblies reference SDKs. Bootstrap injects adapters,
store, clock and ID source. Changing providers changes composition/adapters, not
policy or solver tests. No singleton, account backend, remote config service or
board save/resume is required.

Injected build configuration has `Disabled` (current default) and `Enabled` modes.
Disabled gives direct free hints, hides purchase UI and does not initialize
providers. Enabled with unavailable adapters follows failure rules; it does not
manufacture unpaid hints. Test fakes are restricted to tests/review builds.

## Durable state

One versioned envelope, separate from language/tutorial preferences and board data:

| Field | Meaning |
| --- | --- |
| schemaVersion / revision | Migration and serialized commit revision |
| nextBoardInstanceSequence | Monotonic ID allocated before binding a successfully generated ordinary board |
| lastCountedBoardInstanceSequence | Completion deduplication high-water mark; independent of seed |
| ordinaryBoardsCompleted | Nonnegative 64-bit count while enabled, including owned play |
| handledProgressionMilestone | Highest consumed/skipped fourth-completion opportunity, not impressions |
| entitlement | Unknown / NotOwned / Owned / Revoked; logical product RemoveAllAds, store namespace, opaque validated evidence reference and verification metadata |
| outstandingRewardGrant | Zero or one confirmed unused reward authorization: operation and origin-board IDs |
| nextOperationSequence / processedRewardSequence | Durable operation allocation and reward deduplication watermark; operations serialized |
| pendingPurchaseFinalization | Verified transaction references awaiting store finish/acknowledgement |
| pendingPurchaseOperation | Logical product and opaque pending/deferred transaction reference; no entitlement yet |

State commits are serialized and atomic with expected-revision checks. An adapter
can use flushed temporary-file replacement plus last-good backup. Evidence and
transaction payloads stay adapter-private in appropriate platform storage, never
Git, public logs or telemetry. Checksums detect damage, not ownership; an editable
PlayerPrefs boolean is not verification. Client-only evidence is not fraud-proof.

Activation starts at zero; MVP history cannot be reconstructed. Allocate a board
instance only after successful generation. Clear/Menu/re-render retain it; another
New Game with the same seed gets a different ID. Only the active ordinary session's
first incomplete-to-completed transition may count. Atomically increment and
persist its deduplication watermark immediately. Exclude guided and independent
tutorial wins, benchmarks/previews, abandoned boards and initially complete sessions.
Repeated win/UI/hint notifications do not increment twice.

Recover last-good state after an interrupted/damaged write; preserve unknown future
schemas without overwriting. If unreadable/unwritable, keep play usable and suspend
forced progression ads for that run, with bounded local diagnostics. Do not replay
debt, invent ownership or start a reward flow unable to persist its grant. Verified
in-memory paid ownership still works. Retry storage only at safe explicit boundaries.
A kill before the completion commit can lose that last win; deleting/reinstalling
app data can reset cadence. No cloud/device counter sync or board restore is promised.

## Service ports

#30 implements IAdService, IPurchaseService and IMonetizationStateStore.
Hint application uses GameSession's preparation/revision seam; cached-evidence
verification is folded into the purchase port. Time is an injected monotonic
function and operation IDs come from persisted state. Diagnostics remain optional
future work. The logical contracts below also describe future provider obligations.

| Port | Contract |
| --- | --- |
| IAdService | Availability/optional Prepare and Present(requestId, placement, cancellation); Progression or Hint; NotReady, Offline, NoFill, Restricted, Failed, Cancelled, Closed, plus distinct trusted RewardEarned evidence |
| IPurchaseService | GetOffer, Purchase, Restore, Refresh (includes ownership, pending/deferred and unfinished transactions), verified Updated events and Finish; normalized owned/not-owned/revoked/pending/cancelled/unavailable/failed results with explicit verification/inventory flags |
| IEntitlementEvidenceVerifier | Adapter-private cached/store evidence verification; authoritative ownership/revocation or Unknown |
| IMonetizationStateStore | Load/recover/migrate; atomic revision-checked commit with explicit failures |
| IHintApplication | Nonmutating preparation of an eligible clue-derived hint, then revalidation/application against same session/revision |
| IClock / IOperationIdSource | Inject deadlines and IDs unique across restarts |
| IMonetizationDiagnostics | Optional local normalized outcomes; no vendor/network dependency |

No SDK types, ad placement IDs, store product IDs, raw receipts or provider errors
cross application contracts. Versioned normalized records may hold opaque private
evidence references; raw evidence is accessed only through the verifier adapter.
Ad adapters normalize reward/dismiss callbacks in
either order and duplicates. Separate presentation/reward/closed state: dismissal
does not prove reward. Unlock only after native presentation safely ends. Timeout
or cancellation cannot be assumed to close an already presented native ad. All
loading waits have injected bounded deadlines; no wait blocks ordinary play forever.

Purchase adapters own verification, store-specific acknowledgement/finish and
replay of unfinished transactions on launch/resume. RemoveAllAds is never consumed.
Incomplete/offline inventory is Unknown, not authoritative absence. Replacing an
IAP adapter must migrate/reverify evidence for the same store product; changing
ad providers cannot affect ownership.

## Fourth-completion progression

`milestone = floor(ordinaryBoardsCompleted / 4)`. Due means milestone exceeds
handledProgressionMilestone and player is not Owned. Thus 1–3 are ad-free, 4/8/12
create opportunities regardless of starts, generation failures or hints watched.
A final rewarded hint creates the same completion event as a tap.

1. Win stays immediate and inline; completed board stays readonly and visible.
2. Attempt a due ad on the next deliberate New Game, or Start that creates a fresh
   board after completion. Resume of an active board is not this boundary.
3. Atomically consume the milestone **before** presentation. If ownership is
   resolved and ad ready, present once, then proceed after safe closure. Offline,
   no-fill, restriction or failure proceeds immediately, without retry/debt.
   Generation may prepare independently and never needs an ad result to succeed.
4. Restart before consumption preserves one opportunity for the next explicit
   fresh-board action; restart after consumption never replays it. No launch ad.

Owned completions also advance handled milestone. Granting Owned consumes any due
opportunity; verified revocation creates no catch-up debt, only the next subsequent
multiple of four. Recovery coalesces anomalous old opportunities instead of queuing.
No-fill never resets the lifetime count. On re-enabling Disabled mode reconcile
old opportunities without catch-up.

A rewarded ad does not count as a progression ad or reset cadence. If a rewarded
hint finishes the fourth board, the progression opportunity still occurs on the
next explicit New Game. This can produce two ad exposures close together; no
unrequested spacing cap changes the specified cadence. Completion viewing remains
available before the player chooses New Game. Any future spacing exception needs
an explicit policy change and tests.

## Hint flow

1. Reject completed/tutorial/loading sessions; cancel active gestures. Prepare an
   eligible clue-derived deduction without changing marks. Unavailable/NoChange
   never requests an ad. This requires a future seam beside current ApplyHint,
   not another solver inside Monetization.
2. Disabled mode or verified Owned applies one existing hint directly.
3. Unowned may redeem an existing confirmed grant on explicit Hint. Otherwise
   request one rewarded ad; suspend board mutation and serialize repeated taps.
   Cancel/skip/close without reward/no-fill/unconfirmed timeout grants no hint.
4. Persist one authorization on trusted RewardEarned before delivery. After safe
   closure, only matching request/board/revision can receive automatic delivery.
   Revalidate/apply one fact, then commit grant consumption before re-enabling input
   when storage is healthy; use the explicit failure path below otherwise.
   Deduplication survives grant consumption; duplicate callbacks never mint another.
   Record completion once if this hint wins.
5. Changed/ended board or restart never automatically receives a stale hint. Retain
   one unused authorization for the next explicit Hint on an ordinary board, with
   fresh deduction preparation. This is recovery for an already watched ad, not
   stackable currency. Show text status that the next hint is already earned.

If saving an earned grant fails after a trusted callback, retain its authorization
and deduplication in memory, attempt the originally requested hint once after safe
closure (or retain for explicit retry if no fact applies), and prohibit a second
rewarded ad while recovery is unresolved. Report recovery status without blocking
play. If saving consumption fails after application, mark it consumed in memory
and retain its processed-operation watermark: no repeat hint or reward in this
process. Restore input after safe native closure despite the write failure. Retry
the latest combined state at safe boundaries; never retry an older earned-state
snapshot after its grant was consumed. Tests inject both failures independently.
Killing the app before the recovery commit may lose an in-memory reward or expose
an older unused durable grant; no cross-process guarantee is invented.

Crash after application but before consuming the grant may retain one recovery
authorization; favor honoring the watched reward over forcing another ad. No
cross-process exactly-once mark mutation is claimed without board save. A kill
before authoritative reward notification may have no recoverable grant; future
provider contract tests must state this limit. Unknown/late unrelated operations
cannot mint grants. Ownership clears an unused grant and never applies a hint
just because a purchase completed.

## Purchase, removal and restore

Menu explains both removals, displays only the store's localized price, and keeps
Restore visible even if offer lookup fails. Pending/owned/unavailable states prevent
duplicate buys. Button presses, cancelled/failed/unverified purchases or pending
approval do not grant access. Persist a pending/deferred operation if reported by
the store and keep purchase disabled for that product. On launch/resume, refresh
pending purchases as well as ownership/unfinished grants; store updates resolve
pending to verified purchased or authoritative cancelled/failed. Restore alone is
not assumed to enumerate pending transactions. Unknown/incomplete pending query
keeps a previously pending purchase pending and never grants/revokes ownership.
If the pending-state write failed, startup must resolve store pending state before
enabling Purchase; a complete store result may clear a stale pending record.

Verified Purchased/AlreadyOwned atomically persists Owned and evidence, clears
unused reward and due progression, and journals finalization before success UI.
Then finish/acknowledge through adapter; retry unfinished finalization on launch/
resume. Finalization network failure never removes verified access. If local save
fails, retain verified in-memory access and recovery status, leave the transaction
recoverable, and do not finish before durable grant.

Ownership refreshes Hint/menu labels and cancels queued ads immediately. An already
presented native ad closes only where provider supports it. An in-flight hinted
request may deliver at most its original hint once after safe closure; all later
Hint requests are direct.

Startup loads cached verified ownership for offline use, refreshes asynchronously
on launch/resume and listens for store updates. Explicit Restore uses the same
verified grant path. Transport/verification failures preserve prior ownership.
Only authoritative revocation/refund or verified complete inventory proving no
ownership for current store account removes it; no debt is resurrected.

Fresh install/device without cache needs successful store query/restore. This is
same-store/account/product restoration, not cross-store transfer. While initial
ownership is Unknown, normal play continues and progression ads skip; Hint offers
restore/retry status rather than showing a possible owner's ad. No indefinite wait.

## Offline and failure outcomes

| Condition | Outcome |
| --- | --- |
| Unavailable progression ad | Proceed; consume opportunity, no debt |
| Unavailable rewarded ad, unowned | No new hint; concise localized status, explicit retry or continue solving |
| Owned offline | No ads; direct Hint works |
| Unknown without usable cache | Normal play; skip progression; Hint restore/retry status |
| Pending/cancelled/unverified purchase | No new grant; preserve existing verified access |
| Restore/network failure | Preserve cached access; retry explicitly or at later lifecycle refresh |
| Duplicate/stale/timeout callback | No duplicate mutation or inferred reward; never unlock beneath native presentation |
| Corrupt/write-failed state | Recover if possible; no mandatory ad loop or invented ownership |

Advertising eligibility is not presumed: Restricted is a normal unavailable
outcome. Actual privacy/store declarations wait for selected SDK behavior. No
analytics SDK or paid background service is required.

## Tests and experiment

See [TESTING.md](TESTING.md#issue-29-monetization-design-test-plan) for
deterministic/fault/mounted coverage. #30 implements SDK-free service, storage,
hint preparation and shell tests; real-provider integration cases remain future gates.
Fakes control callback order, store evidence, time, persistence faults and restart.
Replacement adapters must pass the same contract suite before activation.

Future hypothesis: these placements fund the game while players still finish and
choose another board. Compare subsequent-board starts and hint failures against
ad-free play; thresholds/cohort method need approval before activation. Manual
trial evidence comes first. Optional diagnostics are in
[ANALYTICS.md](ANALYTICS.md#issue-29-future-monetization-diagnostics); telemetry never
gates play, reward or ownership.

Store lifecycle checks (2026-10-14, guidance for future adapters): Apple exposes
non-consumable ownership through
[currentEntitlements](https://developer.apple.com/documentation/storekit/transaction/currententitlements)
and [finishes transactions after delivery](https://developer.apple.com/documentation/storekit/transaction).
Google's [billing guide](https://developer.android.com/google/play/billing/integrate)
documents pending/completed purchases, querying and acknowledging non-consumables.
No provider is selected.

No owner decision blocks architecture. Chosen defaults include tutorial exclusion,
next-board placement, skipping progression failures, unavailable unpaid offline
hints, offline verified paid access and one unused reward recovery authorization.
Pricing, provider selection, eligibility/privacy, experiment thresholds and rollout
remain future decisions. Rollback disables policy/providers without deleting state
or verified ownership. Client-only evidence and crash-before-commit limits remain.
