#!/usr/bin/env python3
"""#64: analyze sanitized native observations; never infer a pass from missing data."""
import argparse
import json
import re
from pathlib import Path

FIELDS = set('schema runId process sequence timeUTC uptime phase event unitySDK pawsOwnershipLogic cacheAccess reason verifiedCurrentCount verifiedUnfinishedCount unverifiedCount unexpectedEnvironmentCount source verified environment transactionRef revoked eligible available result error'.split())


def analyze(events):
    if not events:
        raise ValueError('No native events')
    runs = {e.get('runId') for e in events}
    if len(runs) != 1 or None in runs:
        raise ValueError('Exactly one run is required')
    previous = 0
    for e in events:
        if set(e) - FIELDS:
            raise ValueError('Unexpected event fields; do not export provider payloads')
        if e.get('schema') != 'paws-native-ask-to-buy-v1' or e.get('sequence', 0) != previous + 1:
            raise ValueError('Malformed, truncated or unordered journal')
        previous = e['sequence']
        if 'transactionRef' in e and not re.fullmatch('[0-9a-f]{64}', e['transactionRef']):
            raise ValueError('Transaction reference must be an opaque hash')
    baseline = next((e for e in events if e.get('event') == 'snapshot-end' and e.get('reason') == 'baseline'), None)
    clean = baseline is not None and all(baseline.get(k) == 0 for k in ('verifiedCurrentCount', 'verifiedUnfinishedCount', 'unverifiedCount', 'unexpectedEnvironmentCount'))
    results = [e for e in events if e.get('event') == 'purchase-result']
    pending = len(results) == 1 and results[0].get('result') == 'pending'
    before = [e for e in events if e.get('event') == 'before-approval-checkpoint-complete']
    after = next((e for e in events if e.get('event') == 'snapshot-end' and e.get('reason') == 'owner-after-approval'), None)
    boundary = before[-1]['sequence'] if before else 0
    prior = [e for e in events if e['sequence'] <= boundary and e.get('event') == 'transaction' and e.get('source') == 'currentEntitlements' and e.get('verified') is True and e.get('eligible') is True]
    all_current = [e for e in events if e.get('event') == 'transaction' and e.get('source') == 'currentEntitlements' and e.get('verified') is True and e.get('eligible') is True]
    first = all_current[0] if all_current else None
    restart = any(e.get('event') == 'launch' and e.get('phase') == 'pending' for e in events)
    streams = {e.get('source') for e in events if e.get('event') == 'transaction'}
    wrong_environment = any(e.get('event') == 'transaction' and e.get('environment') != 'xcode' for e in events)
    required_reasons = {'purchase-return', 'restart-before-approval', 'owner-before-approval'}
    pre_snapshots = [e for e in events if e['sequence'] <= boundary and e.get('event') == 'snapshot-end' and e.get('reason') in required_reasons]
    pre_complete = required_reasons <= {e['reason'] for e in pre_snapshots} and all(e.get('unverifiedCount') == 0 and e.get('unexpectedEnvironmentCount') == 0 for e in pre_snapshots)
    restarts = [e for e in events if e.get('event') == 'launch' and e.get('phase') == 'pending']
    checkpoint_after_restart = bool(restarts) and boundary > restarts[-1]['sequence']
    approvals = [e for e in events if e.get('event') == 'owner-reports-approved']
    approval_sequence = approvals[0]['sequence'] if approvals else len(events) + 1
    post_snapshots = [e for e in events if e['sequence'] > approval_sequence and e.get('event') == 'snapshot-end']
    approval_ordered = bool(approvals) and after is not None and boundary < approvals[0]['sequence'] < after['sequence']
    listener_started = any(e.get('event') == 'updates-listener-started' and e['sequence'] < results[0]['sequence'] for e in events) if results else False
    complete = not wrong_environment and clean and pending and pre_complete and checkpoint_after_restart and approval_ordered and listener_started and after is not None and after.get('unverifiedCount') == 0 and after.get('unexpectedEnvironmentCount') == 0 and after.get('verifiedCurrentCount', 0) > 0 and restart
    outcome = ('native-storekit-test-anomaly' if prior else 'native-correct-before-approval') if complete else 'incomplete'
    return {
        'schema': 'paws-native-ask-to-buy-analysis-v1', 'runId': next(iter(runs)),
        'outcome': outcome, 'cleanBaseline': clean, 'purchaseResultPending': pending,
        'ownerRestartRecorded': restart, 'verificationCleanPreapprovalSnapshots': pre_complete, 'updatesListenerStartedBeforePurchaseResult': listener_started, 'beforeApprovalCheckpointSequence': boundary or None,
        'verifiedCurrentEntitlementBeforeApproval': bool(prior),
        'beforeApprovalEntitlementReferences': sorted({e['transactionRef'] for e in prior}),
        'afterOwnerApprovalVerifiedCurrentCount': after.get('verifiedCurrentCount') if after else None,
        'postApprovalSnapshots': [{k: e[k] for k in ('sequence', 'timeUTC', 'phase', 'reason', 'verifiedCurrentCount', 'verifiedUnfinishedCount')} for e in post_snapshots],
        'verifiedUnfinishedOrUpdatesBeforeApproval': any(e['sequence'] <= boundary and e.get('event') == 'transaction' and e.get('source') in ('unfinished', 'updates') and e.get('verified') is True for e in events),
        'firstVerifiedCurrentEntitlement': {k: first[k] for k in ('sequence', 'timeUTC', 'source', 'reason', 'transactionRef', 'environment')} if first else None,
        'observedTransactionSources': sorted(streams),
        'scope': 'Native observation only; approval ordering also requires owner/manager evidence; no Paws or sandbox acceptance inferred',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('journal', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    events = [json.loads(line) for line in args.journal.read_text().splitlines() if line.strip()]
    result = analyze(events)
    rendered = json.dumps(result, indent=2) + '\n'
    if args.output:
        args.output.write_text(rendered)
    else:
        print(rendered, end='')


if __name__ == '__main__':
    main()
