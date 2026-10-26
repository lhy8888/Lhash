#!/usr/bin/env python3
"""Convert the existing Windows branding to .icns with macOS system tools."""
import argparse
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile


def png_images(data):
    if len(data) < 6:
        raise ValueError("Truncated ICO header")
    reserved, kind, count = struct.unpack_from("<3H", data)
    if reserved != 0 or kind != 1 or count == 0 or 6 + count * 16 > len(data):
        raise ValueError("Invalid ICO directory")
    images = {}
    for index in range(count):
        width, height, _, _, _, _, length, offset = struct.unpack_from("<4B2H2I", data, 6 + index * 16)
        width, height = width or 256, height or 256
        if offset < 6 + count * 16 or offset + length > len(data):
            raise ValueError("ICO image is outside the input file")
        png = data[offset:offset + length]
        if not png.startswith(b"\x89PNG\r\n\x1a\n"):
            continue
        if len(png) < 33 or png[12:16] != b"IHDR" or struct.unpack_from(">2I", png, 16) != (width, height):
            raise ValueError("ICO PNG dimensions disagree with the directory")
        if width != height:
            raise ValueError("Application icon must be square")
        images[width] = png
    if not images:
        raise ValueError("ICO has no PNG images")
    return images


def create_icon(source, output):
    images = png_images(source.read_bytes())
    sips, iconutil = shutil.which("sips"), shutil.which("iconutil")
    if not sips or not iconutil:
        raise ValueError("Creating the application icon requires macOS sips and iconutil")
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="lhash-icon-") as temporary:
        root = Path(temporary)
        iconset = root / "LHash.iconset"
        iconset.mkdir()
        source_paths = {}
        for dimension, data in images.items():
            path = root / f"source-{dimension}.png"
            path.write_bytes(data)
            source_paths[dimension] = path
        for base in (16, 32, 128, 256, 512):
            for scale in (1, 2):
                pixels = base * scale
                name = f"icon_{base}x{base}" + ("@2x" if scale == 2 else "") + ".png"
                destination = iconset / name
                if pixels in source_paths:
                    shutil.copyfile(source_paths[pixels], destination)
                else:
                    # The source ICO contains up to 256px. Larger macOS icon
                    # representations use the same artwork, scaled by sips.
                    closest = min((size for size in source_paths if size >= pixels), default=max(source_paths))
                    subprocess.run([sips, "-z", str(pixels), str(pixels), str(source_paths[closest]),
                                    "--out", str(destination)], check=True, stdout=subprocess.DEVNULL)
        intermediate = root / "LHash.icns"
        subprocess.run([iconutil, "-c", "icns", str(iconset), "-o", str(intermediate)], check=True)
        data = intermediate.read_bytes()
        if len(data) < 8 or data[:4] != b"icns" or struct.unpack_from(">I", data, 4)[0] != len(data):
            raise ValueError("iconutil did not produce a valid ICNS container")
        output.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        create_icon(args.input, args.output)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"Icon conversion failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
