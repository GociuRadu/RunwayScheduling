import pandas as pd
from exceptions import NoDataError

# GA parameters used for correlation and best params analysis
GA_PARAMS = [
    "population_size", "max_generations", "crossover_rate",
    "mutation_rate_local", "mutation_rate_memetic", "tournament_size",
    "elite_count", "no_improvement_generations",
]


class AnalysisService:
    def __init__(self, df: pd.DataFrame):
        self._df = df  # full DataFrame from BenchmarkService.get_all()

    def overview(self) -> pd.DataFrame:
        # average fitness and solve time grouped by difficulty and algorithm
        return (
            self._df.groupby(["difficulty", "algorithm_type"])
            .agg(
                avg_fitness=("fitness", "mean"),
                best_fitness=("fitness", "max"),
                avg_solve_time_ms=("solve_time_ms", "mean"),
                runs=("fitness", "count"),
            )
            .reset_index()
            .round(2)
        )

    def compare(self, scenario_name: str | None = None) -> pd.DataFrame:
        # GA vs Greedy comparison, optionally filtered by scenario
        df = self._df if scenario_name is None else self._df[self._df["scenario_name"] == scenario_name]
        if df.empty:
            raise NoDataError(detail=f"No data for scenario: {scenario_name}")
        return (
            df.groupby(["scenario_name", "algorithm_type"])
            .agg(
                avg_fitness=("fitness", "mean"),
                avg_solve_time_ms=("solve_time_ms", "mean"),
                runs=("fitness", "count"),
            )
            .reset_index()
            .round(2)
        )

    def param_correlation(self, difficulty: int | None = None) -> pd.DataFrame:
        # correlation between GA params and fitness — higher = param matters more
        df = self._df if difficulty is None else self._df[self._df["difficulty"] == difficulty]
        ga_df = df[df["algorithm_type"] != "Greedy"][GA_PARAMS + ["fitness"]].dropna()
        if ga_df.empty:
            raise NoDataError(detail="No GA data for correlation")
        return ga_df.corr()[["fitness"]].drop("fitness").round(3)

    def best_params(self, difficulty: int, aircraft_count: int, top_n: int = 5) -> pd.DataFrame:
        # top N GA runs by fitness for a given difficulty and aircraft count
        df = self._df[
            (self._df["difficulty"] == difficulty)
            & (self._df["aircraft_count"] == aircraft_count)
            & (self._df["algorithm_type"] != "Greedy")
        ]
        if df.empty:
            raise NoDataError(detail=f"No GA data for difficulty={difficulty}, aircraft={aircraft_count}")
        return (
            df.nlargest(top_n, "fitness")[GA_PARAMS + ["fitness", "solve_time_ms", "scenario_name"]]
            .reset_index(drop=True)
        )
