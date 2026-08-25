#!/usr/bin/env python3
"""Extract selected JSON data tables from ICARUS Content/Data/data.pak.

The ICARUS data bundle stores table payloads as zlib streams. Some tables are
split across multiple consecutive streams, so this extractor scans for zlib
payloads, groups streams that form a complete JSON table, then writes the
tables we use in the editor.
"""

from __future__ import annotations

import argparse
import json
import re
import zlib
from pathlib import Path


DEFAULT_DATA_PAK = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\Icarus\Icarus\Content\Data\data.pak"
)

ROW_STRUCT_TO_FILE = {
    "/Script/Icarus.AICreatureType": "D_AICreatureType.json",
    "/Script/Icarus.AIGrowth": "D_AIGrowth.json",
    "/Script/Icarus.AISetup": "D_AISetup.json",
    "/Script/Icarus.CharacterGrowth": "D_CharacterGrowth.json",
    "/Script/Icarus.GeneticLineage": "D_GeneticLineages.json",
    "/Script/Icarus.GeneticValue": "D_GeneticValues.json",
    "/Script/Icarus.IcarusMount": "D_Mounts.json",
    "/Script/Icarus.IcarusTamingData": "D_Tames.json",
    "/Script/Icarus.TamedCreatureModifier": "D_TamedCreatureModifiers.json",
    "/Script/Icarus.Talent": "D_Talents.json",
    "/Script/Icarus.TalentRank": "D_TalentRanks.json",
    "/Script/Icarus.TalentTree": "D_TalentTrees.json",
}

DEFAULT_TABLES = [
    "D_AICreatureType.json",
    "D_AIGrowth.json",
    "D_AISetup.json",
    "D_CharacterGrowth.json",
    "D_GeneticLineages.json",
    "D_GeneticValues.json",
    "D_Mounts.json",
    "D_TamedCreatureModifiers.json",
    "D_Tames.json",
    "D_TalentRanks.json",
    "D_Talents.json",
    "D_TalentTrees.json",
]


def iter_zlib_payloads(data: bytes) -> list[bytes]:
    payloads: list[bytes] = []
    for match in re.finditer(rb"\x78[\x01\x5e\x9c\xda]", data):
        start = match.start()
        try:
            payloads.append(zlib.decompress(data[start:]))
        except zlib.error:
            continue
    return payloads


def table_start_indexes(payloads: list[bytes]) -> list[int]:
    return [
        index
        for index, payload in enumerate(payloads)
        if payload.lstrip().startswith(b"{") and b"RowStruct" in payload[:200]
    ]


def extract_tables(data_pak: Path) -> dict[str, dict]:
    payloads = iter_zlib_payloads(data_pak.read_bytes())
    starts = table_start_indexes(payloads)
    tables: dict[str, dict] = {}

    for start_index, payload_index in enumerate(starts):
        next_payload_index = (
            starts[start_index + 1] if start_index + 1 < len(starts) else len(payloads)
        )
        raw_table = b"".join(payloads[payload_index:next_payload_index])
        try:
            table = json.loads(raw_table.decode("utf-8-sig"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            continue

        row_struct = table.get("RowStruct")
        file_name = ROW_STRUCT_TO_FILE.get(row_struct)
        if file_name:
            tables[file_name] = table

    return tables


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract selected ICARUS JSON data tables into the repo data folder."
    )
    parser.add_argument("--pak", type=Path, default=DEFAULT_DATA_PAK, help="Path to data.pak")
    parser.add_argument("--out", type=Path, default=Path("data"), help="Output folder")
    parser.add_argument(
        "--table",
        action="append",
        dest="tables",
        help="Specific table filename to extract, such as D_Mounts.json. May be repeated.",
    )
    parser.add_argument(
        "--list",
        action="store_true",
        help="List extractable mapped table filenames without writing files.",
    )
    args = parser.parse_args()

    if not args.pak.exists():
        raise FileNotFoundError(f"data.pak was not found: {args.pak}")

    tables = extract_tables(args.pak)
    requested = args.tables or DEFAULT_TABLES

    if args.list:
        for file_name in sorted(tables):
            print(file_name)
        return 0

    args.out.mkdir(parents=True, exist_ok=True)
    missing = [file_name for file_name in requested if file_name not in tables]
    if missing:
        raise RuntimeError("Could not extract: " + ", ".join(missing))

    for file_name in requested:
        target = args.out / file_name
        target.write_text(json.dumps(tables[file_name], indent=4) + "\n", encoding="utf-8")
        print(f"Wrote {target}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
