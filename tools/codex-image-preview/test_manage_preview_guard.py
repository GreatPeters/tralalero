"""Exercise install and rollback against an isolated copy, never the live extension."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location(
    "guard_manager", Path(__file__).with_name("manage-preview-guard.py"))
manager = importlib.util.module_from_spec(spec)
spec.loader.exec_module(manager)


class GuardInstallerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.extension = self.root / "extension"
        self.backups = self.root / "backups"
        (self.extension / "webview").mkdir(parents=True)
        (self.extension / "package.json").write_text(json.dumps({
            "publisher": "openai", "name": "chatgpt", "version": manager.VERSION}))
        self.index = self.extension / "webview" / "index.html"
        self.original = b'<html>\r\n    <script type="module" crossorigin src="./assets/index-8e8701a2aad8.js"></script>\r\n</html>'
        self.index.write_bytes(self.original)

    def run_action(self, action):
        manager.manage(action, self.extension, self.backups)

    def test_install_is_idempotent_and_restore_is_byte_exact(self):
        self.run_action("install")
        installed = self.index.read_bytes()
        self.assertEqual(installed.count(manager.TAG.encode()), 1)
        self.assertLess(installed.index(manager.TAG.encode()), installed.index(b'type="module"'))
        self.run_action("install")
        self.assertEqual(self.index.read_bytes(), installed)
        self.run_action("restore")
        self.assertEqual(self.index.read_bytes(), self.original)
        self.run_action("install")
        self.assertEqual(self.index.read_bytes(), installed)

    def test_restore_refuses_to_overwrite_subsequent_changes(self):
        self.run_action("install")
        self.index.write_bytes(self.index.read_bytes() + b'<!-- other edit -->')
        with self.assertRaisesRegex(ValueError, "changed after"):
            self.run_action("restore")
        self.assertTrue(self.index.read_bytes().endswith(b'<!-- other edit -->'))

    def test_unknown_version_is_not_modified(self):
        package = self.extension / "package.json"
        package.write_text(json.dumps({"publisher": "openai", "name": "chatgpt", "version": "next"}))
        with self.assertRaisesRegex(ValueError, "Unverified"):
            self.run_action("install")
        self.assertEqual(self.index.read_bytes(), self.original)

    def test_changed_entrypoint_is_not_modified(self):
        self.index.write_bytes(b'<script src="new-build.js"></script>')
        with self.assertRaisesRegex(ValueError, "Unexpected entry"):
            self.run_action("install")
        self.assertEqual(self.index.read_bytes(), b'<script src="new-build.js"></script>')


if __name__ == "__main__":
    unittest.main()
