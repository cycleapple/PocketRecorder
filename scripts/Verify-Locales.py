"""Verify locale coverage, duplicate keys and composite format placeholders."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent


def unique(pairs):
    result = {}
    for key, value in pairs:
        assert key not in result, f"Duplicate locale key: {key}"
        result[key] = value
    return result


locales = {
    p.stem: json.loads(p.read_text(encoding="utf-8-sig"), object_pairs_hook=unique)
    for p in (root / "Localization/locales").glob("*.json")
}
english = locales["en"]
for language, translations in locales.items():
    assert translations.keys() == english.keys(), f"{language}: key coverage mismatch"
    for key, value in translations.items():
        assert value.strip(), f"{language}: empty {key}"
        placeholders = lambda s: sorted(re.findall(r"\{(\d+)(?:[^{}]*)\}", s))
        assert placeholders(value) == placeholders(english[key]), f"{language}: format mismatch {key}"

for source in root.rglob("*.cs"):
    if "obj" in source.parts or "bin" in source.parts:
        continue
    for key in re.findall(r'Loc\.T\("([^"\n]+)"', source.read_text(encoding="utf-8-sig")):
        assert key in english, f"{source}: missing key {key}"

print(f"PASS: {len(locales)} locales, {len(english)} keys, placeholders and literal call sites")
