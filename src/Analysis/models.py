from sqlalchemy.orm import DeclarativeBase, relationship
from sqlalchemy import Column, String, Integer, Float, Boolean, DateTime, ForeignKey
from sqlalchemy.dialects.postgresql import UUID


class Base(DeclarativeBase):
    pass


class ScenarioConfig(Base):
    __tablename__ = "scenario_configs"

    id = Column(UUID(as_uuid=True), primary_key=True)
    name = Column(String)
    difficulty = Column(Integer)
    aircraft_count = Column(Integer)
    start_time = Column(DateTime)
    end_time = Column(DateTime)
    weather_difficulty = Column(Integer)

    benchmark_entries = relationship("BenchmarkEntry", back_populates="scenario_config")


class BenchmarkEntry(Base):
    __tablename__ = "benchmark_entries"

    id = Column(UUID(as_uuid=True), primary_key=True)
    scenario_config_id = Column(UUID(as_uuid=True), ForeignKey("scenario_configs.id"))
    algorithm_type = Column(String)
    config_index = Column(Integer)
    run_timestamp_utc = Column(DateTime)
    fitness = Column(Float)
    solve_time_ms = Column(Float)

    # GA params
    population_size = Column(Integer)
    max_generations = Column(Integer)
    crossover_rate = Column(Float)
    mutation_rate_local = Column(Float)
    mutation_rate_memetic = Column(Float)
    tournament_size = Column(Integer)
    elite_count = Column(Integer)
    no_improvement_generations = Column(Integer)
    random_seed = Column(Integer)

    # CP-SAT params
    enable_cp_sat_refinement = Column(Boolean)
    cp_sat_micro_enabled = Column(Boolean)
    cp_sat_micro_every_n_generations = Column(Integer)
    cp_sat_macro_enabled = Column(Boolean)
    cp_sat_macro_every_n_generations = Column(Integer)
    cp_sat_elite_count = Column(Integer)
    cp_sat_random_count = Column(Integer)
    cp_sat_macro_window_count = Column(Integer)
    cp_sat_time_limit_ms_micro = Column(Integer)
    cp_sat_time_limit_ms_macro = Column(Integer)
    cp_sat_neighborhood_size = Column(Integer)

    scenario_config = relationship("ScenarioConfig", back_populates="benchmark_entries")
