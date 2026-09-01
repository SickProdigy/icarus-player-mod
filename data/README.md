# ICARUS Data Tables

## Rights and License Boundary

Files extracted or derived from ICARUS are not licensed under the GNU GPL covering the original application code. ICARUS and its data belong to RocketWerkz and/or their respective rights holders; this repository claims and grants no additional rights in them.

The extraction scripts in `tools/` are original GPL-3.0-only project code. Where redistribution is inappropriate or not permitted, generate tables locally from a lawfully installed copy of ICARUS.

Bundled extracted ICARUS data files:

- `D_AICreatureType.json`
- `D_AICurves.json`
- `D_AIGrowth.json`
- `D_AISetup.json`
- `D_CharacterGrowth.json`
- `D_GeneticLineages.json`
- `D_GeneticValues.json`
- `D_Mounts.json`
- `D_TalentRanks.json`
- `D_Talents.json`
- `D_TalentTrees.json`
- `D_TamedCreatureModifiers.json`
- `D_Tames.json`

These files were extracted from the local ICARUS install so the app can provide built-in talent names, rank limits, mount variation counts, creature setup rows, growth stats, curve-key-backed health/stamina values, and genetics reference data. The raw `data.pak` and content `.pak` files are intentionally not stored in this repo.

Refresh the bundled tables after ICARUS updates:

```powershell
python .\tools\extract_icarus_data.py
python .\tools\extract_icarus_curves.py
```

To refresh only the creature and mount research tables:

```powershell
python .\tools\extract_icarus_data.py --table D_Mounts.json --table D_AISetup.json --table D_AIGrowth.json --table D_CharacterGrowth.json --table D_AICreatureType.json --table D_Tames.json --table D_GeneticValues.json --table D_GeneticLineages.json --table D_TamedCreatureModifiers.json
python .\tools\extract_icarus_curves.py
```
