# stdlib smoke for ingest_still.py. Writes only under tempfile, never Resources.
from __future__ import annotations

import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ingest_still as ing


class IngestStillTests(unittest.TestCase):
    def test_refuse_c001(self):
        with self.assertRaises(SystemExit):
            ing.validate(ing.parse_args(["--id", "C001", "--name", "x", "--presenter", "a.png"]))

    def test_require_a_source(self):
        with self.assertRaises(SystemExit):
            ing.validate(ing.parse_args(["--id", "C007", "--name", "x"]))

    def test_invalid_id(self):
        with self.assertRaises(SystemExit):
            ing.validate(ing.parse_args(["--id", "007", "--name", "x", "--presenter", "a.png"]))

    def test_missing_source_before_folders(self):
        with tempfile.TemporaryDirectory() as tmp:
            missing = Path(tmp) / "nope.jpg"
            with mock.patch.object(ing, "ASSETS", Path(tmp) / "Assets"):
                with mock.patch.object(ing, "ART_CHARACTERS", Path(tmp) / "art"):
                    with mock.patch.object(ing, "UNITY_CHARACTERS", Path(tmp) / "Assets" / "Chars"):
                        with self.assertRaises(SystemExit):
                            ing.main(["--id", "C007", "--name", "x", "--presenter", str(missing)])
                        self.assertFalse((Path(tmp) / "Assets" / "Chars" / "C007").exists())

    def test_contain_and_stable_guid(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            src = tmp / "p.png"
            Image.new("RGB", (200, 300), (10, 20, 30)).save(src)
            assets = tmp / "Assets"
            art = tmp / "art"
            unity = tmp / "Assets" / "Chars"
            with mock.patch.object(ing, "ASSETS", assets):
                with mock.patch.object(ing, "ART_CHARACTERS", art):
                    with mock.patch.object(ing, "UNITY_CHARACTERS", unity):
                        ing.main(["--id", "C007", "--name", "day", "--presenter", str(src)])
                        out = unity / "C007" / "presenter.png"
                        with Image.open(out) as im:
                            self.assertEqual(im.size, (1024, 1536))
                            self.assertEqual(im.mode, "RGBA")
                        meta = (unity / "C007" / "presenter.png.meta").read_text(encoding="utf-8")
                        guid = ing.GUID_RE.search(meta).group(1).lower()
                        (unity / "C007" / "presenter.png.meta").write_text(
                            meta.replace("nPOTScale: 0", "nPOTScale: 9"), encoding="utf-8"
                        )
                        ing.main(["--id", "C007", "--name", "day", "--presenter", str(src)])
                        meta2 = (unity / "C007" / "presenter.png.meta").read_text(encoding="utf-8")
                        self.assertEqual(ing.GUID_RE.search(meta2).group(1).lower(), guid)
                        self.assertIn("nPOTScale: 0", meta2)


if __name__ == "__main__":
    os.chdir(Path(__file__).resolve().parent)
    raise SystemExit(unittest.main())
