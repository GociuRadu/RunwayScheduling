import os
import asyncpg
from uuid import UUID


async def _connect():
    return await asyncpg.connect(
        host=os.getenv("DB_HOST", "localhost"),
        port=int(os.getenv("DB_PORT", "5433")),
        database=os.getenv("DB_NAME", "RunwayScheduling"),
        user=os.getenv("DB_USER"),
        password=os.getenv("DB_PASSWORD"),
    )


async def fetch_scenario_data(scenario_config_id: UUID) -> dict | None:
    conn = await _connect()
    try:
        scenario = await conn.fetchrow(
            'SELECT * FROM scenario_configs WHERE "Id" = $1',
            scenario_config_id,
        )
        if not scenario:
            return None

        benchmarks = await conn.fetch(
            '''SELECT * FROM benchmark_entries
               WHERE "ScenarioConfigId" = $1
               ORDER BY "Fitness" DESC
               LIMIT 20''',
            scenario_config_id,
        )

        return {
            "scenario": dict(scenario),
            "benchmarks": [dict(b) for b in benchmarks],
        }
    finally:
        await conn.close()
