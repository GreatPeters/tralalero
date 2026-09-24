"""Deterministic regression checks; no backend calls or generation requests."""
from pathlib import Path
import sys
import unittest
from unittest.mock import Mock, patch

app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
sys.path.insert(0, str(app))
import engine
from reststop_prompt_wait import wait_with_history_recheck


class PromptWaitTests(unittest.TestCase):
    def test_successful_wait_is_unchanged(self):
        client = Mock()
        result = {'status': {'status_str': 'success'}}
        self.assertIs(wait_with_history_recheck(client, Mock(return_value=result), 'owned', 120, Mock()), result)
        client.request.assert_not_called()

    def test_completion_between_history_and_queue_is_recovered_without_resubmit(self):
        result = {'status': {'status_str': 'success', 'completed': True}}
        client = Mock()
        client.request.side_effect = [{}, {'owned': result}]
        with patch('reststop_prompt_wait.time.sleep'):
            actual = wait_with_history_recheck(client, Mock(side_effect=engine.LostPrompt('gone')), 'owned', 120, Mock())
        self.assertIs(actual, result)
        self.assertEqual(client.request.call_args_list, [(('/history/owned',),), (('/history/owned',),)])
        client.submit.assert_not_called()

    def test_real_missing_prompt_stays_missing(self):
        client = Mock()
        client.request.return_value = {'unrelated': {'status': {'status_str': 'success'}}}
        with patch('reststop_prompt_wait.time.sleep'), self.assertRaises(engine.LostPrompt):
            wait_with_history_recheck(client, Mock(side_effect=engine.LostPrompt('gone')), 'owned', 120, Mock())
        self.assertEqual(client.request.call_count, 3)
        client.submit.assert_not_called()

    def test_recovered_error_is_not_accepted(self):
        client = Mock()
        client.request.return_value = {'owned': {'status': {'status_str': 'error',
            'messages': [['execution_error', {'exception_message': 'actual backend error'}]]}}}
        with self.assertRaisesRegex(engine.GenerationError, 'actual backend error'):
            wait_with_history_recheck(client, Mock(side_effect=engine.LostPrompt('gone')), 'owned', 120, Mock())

    def test_expired_deadline_does_not_start_new_history_reads(self):
        client = Mock()
        with patch('reststop_prompt_wait.time.monotonic', side_effect=[0., 121.]), self.assertRaises(engine.LostPrompt):
            wait_with_history_recheck(client, Mock(side_effect=engine.LostPrompt('gone')), 'owned', 120, Mock())
        client.request.assert_not_called()

    def test_deadline_reached_during_grace_sleep_stops_reads(self):
        client = Mock()
        client.request.return_value = {}
        with patch('reststop_prompt_wait.time.monotonic', side_effect=[0., 119.8, 119.99, 120.01]), \
                patch('reststop_prompt_wait.time.sleep'), self.assertRaises(engine.LostPrompt):
            wait_with_history_recheck(client, Mock(side_effect=engine.LostPrompt('gone')), 'owned', 120, Mock())
        self.assertEqual(client.request.call_count, 1)


if __name__ == '__main__':
    unittest.main()
