import pandas as pd
from sqlalchemy.orm import Session
from models import BenchmarkEntry, ScenarioConfig
from exceptions import NoDataError, DatabaseError


class BenchmarkService:
    def __init__(self, session: Session):
        self._session = session  # DB session accessible in all methods via self._session

    def get_all(self) -> pd.DataFrame:
        try:
            # JOIN between benchmark_entries and scenario_configs, equivalent to SQL JOIN
            rows = (
                self._session.query(BenchmarkEntry, ScenarioConfig)
                .join(ScenarioConfig, BenchmarkEntry.scenario_config_id == ScenarioConfig.id)
                .all()
            )
        except Exception as e:
            raise DatabaseError(detail=str(e))

        if not rows:
            raise NoDataError()

        data = []
        # each row is a tuple (BenchmarkEntry, ScenarioConfig)
        for entry, scenario in rows:
            # key = column name in DataFrame, value = data from ORM object
            data.append({
                "scenario_name": scenario.name,
                "difficulty": scenario.difficulty,
                "aircraft_count": scenario.aircraft_count,
                "algorithm_type": entry.algorithm_type,
                "fitness": entry.fitness,
                "solve_time_ms": entry.solve_time_ms,
                "population_size": entry.population_size,
                "max_generations": entry.max_generations,
                "crossover_rate": entry.crossover_rate,
                "mutation_rate_local": entry.mutation_rate_local,
                "mutation_rate_memetic": entry.mutation_rate_memetic,
                "tournament_size": entry.tournament_size,
                "elite_count": entry.elite_count,
                "no_improvement_generations": entry.no_improvement_generations,
                "cp_sat_neighborhood_size": entry.cp_sat_neighborhood_size,
                "cp_sat_time_limit_ms_micro": entry.cp_sat_time_limit_ms_micro,
                "cp_sat_time_limit_ms_macro": entry.cp_sat_time_limit_ms_macro,
            })

        # convert list of dicts to pandas DataFrame (in-memory table)
        return pd.DataFrame(data)

    def get_scenarios(self) -> list[str]:
        try:
            # get unique scenario names — each row is a tuple ("Scenario A",)
            rows = self._session.query(ScenarioConfig.name).distinct().all()
        except Exception as e:
            raise DatabaseError(detail=str(e))
        return [r[0] for r in rows]  # extract string from tuple
