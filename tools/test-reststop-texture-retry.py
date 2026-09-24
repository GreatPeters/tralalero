import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from reststop_texture_retry import previous_attempt_allows_retry


class RetryProofTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.attempt = {'folder':str(self.folder),'state':'failed','number':1,'prompt_id':'owned'}
        self.proof = {'state':'confirmed_cancelled','prompt_id':'owned','consumed_attempt':1,
                      'worker_terminated':True,'queue_after':{'queue_running':[],'queue_pending':[]}}

    def write_proof(self):
        raw=json.dumps(self.proof).encode()
        (self.folder/'cancellation-receipt.json').write_bytes(raw)
        self.attempt['failure_receipt_sha256']=hashlib.sha256(raw).hexdigest()

    def test_confirmed_consumed_attempt(self):
        self.write_proof()
        self.assertTrue(previous_attempt_allows_retry(self.attempt))
        self.assertEqual(self.attempt['number'],1)

    def test_missing_or_edited_receipt(self):
        self.assertFalse(previous_attempt_allows_retry(self.attempt))
        self.write_proof()
        with (self.folder/'cancellation-receipt.json').open('ab') as stream:stream.write(b' ')
        self.assertFalse(previous_attempt_allows_retry(self.attempt))

    def test_foreign_prompt_rejected(self):
        self.proof['prompt_id']='foreign';self.write_proof()
        self.assertFalse(previous_attempt_allows_retry(self.attempt))

    def test_unconfirmed_or_still_queued_rejected(self):
        self.proof['worker_terminated']=False;self.write_proof()
        self.assertFalse(previous_attempt_allows_retry(self.attempt))
        self.proof['worker_terminated']=True
        self.proof['queue_after']['queue_running']=['owned'];self.write_proof()
        self.assertFalse(previous_attempt_allows_retry(self.attempt))

    def test_submitted_state_cannot_be_replayed(self):
        self.write_proof();self.attempt['state']='submitted'
        self.assertFalse(previous_attempt_allows_retry(self.attempt))

    def test_only_rejected_generated_results_allow_retry(self):
        self.attempt['state']='generated'
        for verdict,expected in [('pass',False),('uncertain',False),('mesh',True),('texture',True)]:
            (self.folder/'visual-review.json').write_text(json.dumps({'verdict':verdict}))
            self.assertEqual(previous_attempt_allows_retry(self.attempt),expected)


if __name__=='__main__':
    unittest.main()
