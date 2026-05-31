import json
import os
import re

import anthropic

client = anthropic.Anthropic(api_key=os.getenv("ANTHROPIC_API_KEY"))


def _build_prompt(description: str, data: dict) -> str:
    s = data["scenario"]
    benchmarks = data["benchmarks"]

    scenario_block = f"""Scenario: {s.get('Name')}
- Difficulty: {s.get('Difficulty')}
- AircraftCount: {s.get('AircraftCount')}
- WeatherPercent: {s.get('WeatherPercent')} | WeatherDifficulty: {s.get('WeatherDifficulty')}
- WakePercent: {s.get('WakePercent')}
- BaseSeparationSeconds: {s.get('BaseSeparationSeconds')}"""

    bench_lines = []
    for i, b in enumerate(benchmarks, 1):
        line = (
            f"  [{i:2}] fitness={b.get('Fitness', 0):.4f}  time={b.get('SolveTimeMs', 0):.0f}ms"
            f"  pop={b.get('PopulationSize')}  gen={b.get('MaxGenerations')}"
            f"  mutLocal={b.get('MutationRateLocal')}  mutMem={b.get('MutationRateMemetic')}"
            f"  tournament={b.get('TournamentSize')}  elite={b.get('EliteCount')}"
            f"  noImprove={b.get('NoImprovementGenerations')}"
            f"  cpSat={b.get('EnableCpSatRefinement')}"
            f"  cpSatMicro={b.get('CpSatMicroEnabled')}(every {b.get('CpSatMicroEveryNGenerations')}gen)"
            f"  cpSatMacro={b.get('CpSatMacroEnabled')}(every {b.get('CpSatMacroEveryNGenerations')}gen)"
            f"  neighborhood={b.get('CpSatNeighborhoodSize')}"
        )
        bench_lines.append(line)

    bench_block = "\n".join(bench_lines)

    return f"""You are an expert in genetic algorithm optimization for airport runway scheduling.

User description of the target scenario:
{description}

Scenario configuration in the database:
{scenario_block}

Top {len(benchmarks)} benchmark runs (sorted by fitness descending, higher = better scheduling):
{bench_block}

Based on the scenario characteristics and the benchmark data above, recommend the optimal GA parameters.

Respond ONLY with a JSON object (no markdown fences) with these fields:
{{
  "recommended_params": {{
    "PopulationSize": int,
    "MaxGenerations": int,
    "CrossoverRate": float,
    "MutationRateLocal": float,
    "MutationRateMemetic": float,
    "TournamentSize": int,
    "EliteCount": int,
    "NoImprovementGenerations": int,
    "EnableCpSatRefinement": bool,
    "CpSatMicroEnabled": bool,
    "CpSatMicroEveryNGenerations": int,
    "CpSatMacroEnabled": bool,
    "CpSatMacroEveryNGenerations": int,
    "CpSatEliteCount": int,
    "CpSatRandomCount": int,
    "CpSatMacroWindowCount": int,
    "CpSatTimeLimitMsMicro": int,
    "CpSatTimeLimitMsMacro": int,
    "CpSatNeighborhoodSize": int
  }},
  "reasoning": "string",
  "expected_fitness_range": "string",
  "key_tradeoffs": ["string"]
}}"""


def get_optimal_params(description: str, data: dict) -> dict:
    prompt = _build_prompt(description, data)

    message = client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=1500,
        messages=[{"role": "user", "content": prompt}],
    )

    text = message.content[0].text.strip()

    match = re.search(r"\{[\s\S]*\}", text)
    if match:
        try:
            return json.loads(match.group())
        except json.JSONDecodeError:
            pass

    return {"raw_response": text}
