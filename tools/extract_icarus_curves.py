#!/usr/bin/env python3
"""Extract AI CurveFloat values from ICARUS content paks.

The data tables reference Unreal CurveFloat assets for creature health,
stamina, damage, genetics, lineage, and character growth. This script scans
the content paks for those curve packages and writes a compact JSON lookup
table with decoded rich-curve keys.
"""

from __future__ import annotations

import argparse
import json
import struct
from pathlib import Path


DEFAULT_PAKS = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\Icarus\Icarus\Content\Paks"
)
PACKAGE_MAGIC = b"\xc1\x83\x2a\x9e"
CURVE_PATH_PREFIXES = (
    b"/Game/Data/AI/Curves/",
    b"/Game/Data/AI/Genetics/",
    b"/Game/Data/AI/Lineage/",
    b"/Game/Data/Character/",
)


def read_fstring(data: bytes, offset: int) -> tuple[str, int]:
    length = struct.unpack_from("<i", data, offset)[0]
    offset += 4
    if length < 0:
        byte_length = -length * 2
        value = data[offset : offset + byte_length - 2].decode("utf-16le", "replace")
        offset += byte_length
    else:
        value = data[offset : offset + length - 1].decode("utf-8", "replace")
        offset += length

    return value, offset


def parse_curve_keys(serial_data: bytes) -> list[dict[str, float]]:
    key_start = 188
    if len(serial_data) < key_start + 21:
        return []

    if len(serial_data) == 209:
        time = struct.unpack_from("<f", serial_data, key_start + 3)[0]
        value = struct.unpack_from("<f", serial_data, key_start + 7)[0]
        return [{"Time": round(time, 6), "Value": value}]

    best_keys: list[dict[str, float]] = []
    for key_count in range(1, 128):
        tail_length = len(serial_data) - key_start - (key_count * 27)
        if tail_length < 0 or tail_length > 64:
            continue

        keys: list[dict[str, float]] = []
        valid = True
        previous_time: float | None = None
        for index in range(key_count):
            offset = key_start + (index * 27)
            interp_mode = serial_data[offset]
            if interp_mode > 5:
                valid = False
                break

            time = struct.unpack_from("<f", serial_data, offset + 3)[0]
            value = struct.unpack_from("<f", serial_data, offset + 7)[0]
            if not -1000000 <= time <= 1000000 or not -10000000 <= value <= 10000000:
                valid = False
                break

            if previous_time is not None and time < previous_time:
                valid = False
                break

            keys.append({"Time": round(time, 6), "Value": value})
            previous_time = time

        if valid and len(keys) > len(best_keys):
            best_keys = keys

    return best_keys


def parse_package(data: bytes, start: int) -> tuple[str, dict] | None:
    if data[start : start + 4] != PACKAGE_MAGIC:
        return None

    package = data[start : start + 2000]
    if len(package) < 900:
        return None

    try:
        offset = 28
        _folder, offset = read_fstring(package, offset)
        (
            _flags,
            name_count,
            name_offset,
            _gatherable_count,
            _gatherable_offset,
            export_count,
            export_offset,
            _import_count,
            _import_offset,
            _depends_offset,
        ) = struct.unpack_from("<IIIIIIIIII", package, offset)

        if name_count <= 0 or export_count <= 0:
            return None

        names: list[str] = []
        offset = name_offset
        for _ in range(name_count):
            name, offset = read_fstring(package, offset)
            offset += 4
            names.append(name)

        curve_path = next(
            (
                name
                for name in names
                if any(name.startswith(prefix.decode("ascii")) for prefix in CURVE_PATH_PREFIXES)
            ),
            None,
        )
        if curve_path is None or "CurveFloat" not in names:
            return None

        serial_size = struct.unpack_from("<q", package, export_offset + 28)[0]
        serial_offset = struct.unpack_from("<q", package, export_offset + 36)[0]
        if serial_size < 199 or serial_offset <= 0:
            return None

        serial_data = package[serial_offset : serial_offset + serial_size]
        keys = parse_curve_keys(serial_data)
        if not keys:
            return None

        is_constant = len({round(key["Value"], 6) for key in keys}) <= 1
        return curve_path, {
            "DefaultValue": keys[0]["Value"],
            "IsConstant": is_constant,
            "Keys": keys,
        }
    except (struct.error, UnicodeDecodeError):
        return None


def extract_curves(paks_dir: Path) -> dict[str, dict]:
    curves: dict[str, dict] = {}
    for pak_path in sorted(paks_dir.glob("*.pak")):
        data = pak_path.read_bytes()
        offset = 0
        while True:
            hits = [
                hit
                for prefix in CURVE_PATH_PREFIXES
                if (hit := data.find(prefix, offset)) != -1
            ]
            if not hits:
                break

            hit = min(hits)

            package_start = data.rfind(PACKAGE_MAGIC, max(0, hit - 4096), hit)
            if package_start != -1:
                parsed = parse_package(data, package_start)
                if parsed is not None:
                    curve_path, value = parsed
                    curves[curve_path] = value

            offset = hit + 1

    return curves


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract ICARUS CurveFloat values into data/D_AICurves.json."
    )
    parser.add_argument("--paks", type=Path, default=DEFAULT_PAKS, help="Path to Content/Paks")
    parser.add_argument("--out", type=Path, default=Path("data/D_AICurves.json"), help="Output JSON file")
    args = parser.parse_args()

    if not args.paks.exists():
        raise FileNotFoundError(f"Content/Paks folder was not found: {args.paks}")

    curves = extract_curves(args.paks)
    if not curves:
        raise RuntimeError("No AI curve values were extracted.")

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(dict(sorted(curves.items())), indent=4) + "\n", encoding="utf-8")
    print(f"Wrote {args.out} with {len(curves)} curves")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
