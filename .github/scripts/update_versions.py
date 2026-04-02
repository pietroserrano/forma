#!/usr/bin/env python3
"""Register a documentation version in versions.json.

Usage:
    python3 update_versions.py <versions_file> <version>

The script reads <versions_file> (a JSON array), adds <version> if it is not
already present, and writes the updated array back to the file.

Each entry has the shape:
    {"version": "v2.1", "label": "v2.1 (stable)", "url": "/forma/v2.1/"}

Known versions and their labels:
    "latest"  -> "latest"
    "dev"     -> "dev (preview)"
    "vX.Y"    -> "vX.Y (stable)"
"""

import json
import sys


def version_label(version: str) -> str:
    if version == "dev":
        return "dev (preview)"
    if version == "latest":
        return "latest"
    return f"{version} (stable)"


def update_versions(versions_file: str, version: str) -> None:
    with open(versions_file) as f:
        versions = json.load(f)

    existing = [v["version"] for v in versions]
    if version not in existing:
        versions.insert(
            0,
            {
                "version": version,
                "label": version_label(version),
                "url": f"/forma/{version}/",
            },
        )
        print(f"Added version: {version}")
    else:
        print(f"Version already registered: {version}")

    with open(versions_file, "w") as f:
        json.dump(versions, f, indent=2)
        f.write("\n")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <versions_file> <version>", file=sys.stderr)
        sys.exit(1)
    update_versions(sys.argv[1], sys.argv[2])
