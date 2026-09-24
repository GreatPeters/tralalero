"""Recheck completion after the installed client's non-atomic history/queue poll."""
import time

import engine


def wait_with_history_recheck(client, base_wait, prompt_id, timeout, notify):
    deadline = time.monotonic() + timeout
    try:
        return base_wait(prompt_id, timeout, notify)
    except engine.LostPrompt:
        # Completion can occur between GET /history and GET /queue. Never submit
        # another prompt here; only recover this exact prompt's completed record.
        notify('Rechecking completion after the queue transition')
        for delay in (0., .25, .5):
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                break
            if delay:
                time.sleep(min(delay, remaining))
                if time.monotonic() >= deadline:
                    break
            history = client.request('/history/' + prompt_id)
            run = history.get(prompt_id)
            if run is None:
                continue
            if run.get('status', {}).get('status_str') != 'success':
                errors = [data for event, data in run.get('status', {}).get('messages', [])
                          if event == 'execution_error']
                message = errors[-1].get('exception_message', 'Generation failed') if errors else 'Generation failed'
                raise engine.GenerationError(message[:1500])
            return run
        raise
