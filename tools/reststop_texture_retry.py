"""Require a reviewed result or a tamper-evident confirmed cancellation."""
import hashlib
import json
from pathlib import Path


def previous_attempt_allows_retry(attempt):
    folder = Path(attempt['folder'])
    try:
        if attempt['state'] == 'generated':
            review = json.loads((folder/'visual-review.json').read_text(encoding='utf8'))
            return review.get('verdict') in ('mesh','texture')
        if attempt['state'] != 'failed':
            return False
        receipt = folder/'cancellation-receipt.json'
        raw = receipt.read_bytes()
        if hashlib.sha256(raw).hexdigest() != attempt.get('failure_receipt_sha256'):
            return False
        proof = json.loads(raw)
        return (proof.get('state') == 'confirmed_cancelled'
                and proof.get('prompt_id') == attempt.get('prompt_id')
                and proof.get('consumed_attempt') == attempt.get('number')
                and proof.get('worker_terminated') is True
                and proof.get('queue_after') == {'queue_running': [], 'queue_pending': []})
    except (OSError,ValueError,KeyError):
        return False
