"""Validate conversion input against the actual shared Windows artwork."""
import importlib.util
from pathlib import Path
import struct
import unittest

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("create_macos_icon", REPO / "tools/create_macos_icon.py")
icon = importlib.util.module_from_spec(spec)
spec.loader.exec_module(icon)


class MacIconTest(unittest.TestCase):
    def test_existing_windows_icon_has_all_seven_original_png_sizes(self):
        images = icon.png_images((REPO / "trunk/source/WinMFC/res/icon1.ico").read_bytes())
        self.assertEqual(set(images), {16, 24, 32, 48, 64, 128, 256})
        self.assertEqual(struct.unpack_from(">2I", images[256], 16), (256, 256))

    def test_out_of_bounds_image_rejected(self):
        data = bytearray((REPO / "trunk/source/WinMFC/res/icon1.ico").read_bytes())
        struct.pack_into("<I", data, 6 + 12, len(data) + 1)
        with self.assertRaisesRegex(ValueError, "outside"):
            icon.png_images(data)

    def test_mismatched_directory_and_png_dimensions_rejected(self):
        data = bytearray((REPO / "trunk/source/WinMFC/res/icon1.ico").read_bytes())
        data[6] = 17
        with self.assertRaisesRegex(ValueError, "dimensions"):
            icon.png_images(data)


if __name__ == "__main__":
    unittest.main()
