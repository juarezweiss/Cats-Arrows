import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location('probe_analysis', Path(__file__).resolve().parents[1] / 'analyze-apple-iap-probe.py')
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class NativeProbeAnalysisTests(unittest.TestCase):
    def events(self, anomaly=False):
        clean = {'event': 'snapshot-end', 'verifiedCurrentCount': 0, 'verifiedUnfinishedCount': 0, 'unverifiedCount': 0, 'unexpectedEnvironmentCount': 0}
        rows = [
            dict(clean, reason='baseline'),
            {'event': 'updates-listener-started'},
            {'event': 'purchase-result', 'result': 'pending'},
            dict(clean, reason='purchase-return'),
            {'event': 'launch', 'phase': 'pending'},
            dict(clean, reason='restart-before-approval'),
        ]
        transaction = {'event': 'transaction', 'source': 'currentEntitlements', 'reason': 'restart-before-approval', 'verified': True, 'eligible': True, 'transactionRef': 'a' * 64, 'environment': 'xcode'}
        if anomaly:
            rows.append(transaction.copy())
        rows.append(dict(clean, reason='owner-before-approval', verifiedCurrentCount=1 if anomaly else 0))
        rows.append({'event': 'before-approval-checkpoint-complete'})
        rows.append({'event': 'owner-reports-approved'})
        rows.append(dict(transaction, reason='owner-after-approval'))
        rows.append({'event': 'snapshot-end', 'phase': 'owner-reports-approved', 'reason': 'owner-after-approval', 'verifiedCurrentCount': 1, 'verifiedUnfinishedCount': 1, 'unverifiedCount': 0, 'unexpectedEnvironmentCount': 0})
        return [dict(schema='paws-native-ask-to-buy-v1', runId='one-run', sequence=i + 1, timeUTC='2026-10-22T00:00:00Z', **row) for i, row in enumerate(rows)]

    def test_native_exposure_before_owner_checkpoint_is_anomaly(self):
        result = MODULE.analyze(self.events(True))
        self.assertEqual('native-storekit-test-anomaly', result['outcome'])
        self.assertTrue(result['verifiedCurrentEntitlementBeforeApproval'])

    def test_correct_native_sequence_requires_post_approval_entitlement(self):
        result = MODULE.analyze(self.events())
        self.assertEqual('native-correct-before-approval', result['outcome'])
        self.assertFalse(result['verifiedCurrentEntitlementBeforeApproval'])

    def test_missing_after_approval_cannot_pass(self):
        self.assertEqual('incomplete', MODULE.analyze(self.events()[:-1])['outcome'])

    def test_verified_unfinished_is_not_current_ownership(self):
        rows = self.events()
        rows[-2]['source'] = 'unfinished'
        rows[-1]['verifiedCurrentCount'] = 0
        self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])
        self.assertIsNone(MODULE.analyze(rows)['firstVerifiedCurrentEntitlement'])

    def test_nonclean_baseline_or_nonpending_result_cannot_pass(self):
        rows = self.events(); rows[0]['verifiedCurrentCount'] = 1
        self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])
        rows = self.events(); next(e for e in rows if e['event'] == 'purchase-result')['result'] = 'success'
        self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])

    def test_unverified_baseline_or_wrong_environment_cannot_pass(self):
        rows = self.events(); rows[0]['unverifiedCount'] = 1
        self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])
        rows = self.events(); rows[-2]['environment'] = 'sandbox'
        self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])

    def test_partial_preapproval_snapshots_cannot_report_correctness(self):
        for reason in ('purchase-return', 'restart-before-approval', 'owner-before-approval'):
            rows = self.events()
            next(e for e in rows if e.get('reason') == reason and e['event'] == 'snapshot-end')['unverifiedCount'] = 1
            self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])
            rows = [e for e in self.events() if not (e.get('reason') == reason and e['event'] == 'snapshot-end')]
            for index, e in enumerate(rows): e['sequence'] = index + 1
            self.assertEqual('incomplete', MODULE.analyze(rows)['outcome'])

    def test_unordered_or_provider_payload_is_rejected(self):
        rows = self.events(); rows[1]['sequence'] = 20
        with self.assertRaises(ValueError): MODULE.analyze(rows)
        rows = self.events(); rows[1]['jws'] = 'not-exportable'
        with self.assertRaises(ValueError): MODULE.analyze(rows)


if __name__ == '__main__':
    unittest.main()
