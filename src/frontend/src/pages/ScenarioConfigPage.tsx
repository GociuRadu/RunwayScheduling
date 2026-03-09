import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";

type ScenarioConfigDto = {
  id: string;
  airportId: string;
  name: string;
  difficulty: number;
  startTime: string;
  endTime: string;
  seed: number;
  aircraftCount: number;
  aircraftDifficulty: number;
  onGroundAircraftCount: number;
  inboundAircraftCount: number;
  remainingOnGroundAircraftCount: number;
  baseSeparationSeconds: number;
  wakePercent: number;
  weatherPercent: number;
  weatherIntervalCount: number;
  minWeatherIntervalMinutes: number;
  weatherDifficulty: number;
};

type FlightDto = {
  id: string;
  scenarioConfigId: string;
  aircraftId: string;
  callsign: string;
  type: number | string;
  scheduledTime: string;
  maxDelayMinutes: number;
  maxEarlyMinutes: number;
  priority: number;
};

type WeatherIntervalsDto = {
  id: string;
  scenarioConfigId: string;
  startTime: string;
  endTime: string;
  condition?: number | string;
  weatherType?: number | string;
};

type ScenarioConfigAllDataDto = {
  id: string;
  airportId: string;
  name: string;
  difficulty: number;
  startTime: string;
  endTime: string;
  seed: number;
  aircraftCount: number;
  aircraftDifficulty: number;
  onGroundAircraftCount: number;
  inboundAircraftCount: number;
  remainingOnGroundAircraftCount: number;
  baseSeparationSeconds: number;
  wakePercent: number;
  weatherPercent: number;
  weatherIntervalCount: number;
  minWeatherIntervalMinutes: number;
  weatherDifficulty: number;
  flights: FlightDto[];
  weatherIntervals: WeatherIntervalsDto[];
};

const STORAGE_AIRPORT_ID = "selectedAirportId";
const STORAGE_AIRPORT_NAME = "selectedAirportName";
const STORAGE_SCENARIO_ID = "selectedScenarioId";
const STORAGE_SCENARIO_NAME = "selectedScenarioName";

export default function ScenarioConfigPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const [airportId, setAirportId] = useState(
    searchParams.get("airportId") ||
      localStorage.getItem(STORAGE_AIRPORT_ID) ||
      "",
  );
  const [airportName, setAirportName] = useState(
    localStorage.getItem(STORAGE_AIRPORT_NAME) || "",
  );

  const [selectedScenarioId, setSelectedScenarioId] = useState(
    searchParams.get("scenarioId") ||
      localStorage.getItem(STORAGE_SCENARIO_ID) ||
      "",
  );
  const [selectedScenarioName, setSelectedScenarioName] = useState(
    localStorage.getItem(STORAGE_SCENARIO_NAME) || "",
  );

  const [scenarios, setScenarios] = useState<ScenarioConfigDto[]>([]);
  const [scenarioData, setScenarioData] =
    useState<ScenarioConfigAllDataDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [scenariosLoaded, setScenariosLoaded] = useState(false);
  const [loadingScenarios, setLoadingScenarios] = useState(false);
  const [loadingScenarioData, setLoadingScenarioData] = useState(false);

  const [showCreateScenario, setShowCreateScenario] = useState(false);
  const [creatingScenario, setCreatingScenario] = useState(false);

  const [showFlightsModal, setShowFlightsModal] = useState(false);
  const [showWeatherModal, setShowWeatherModal] = useState(false);

  const [name, setName] = useState("");
  const [difficulty, setDifficulty] = useState(1);
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [seed, setSeed] = useState(0);
  const [aircraftCount, setAircraftCount] = useState(20);
  const [aircraftDifficulty, setAircraftDifficulty] = useState(1);
  const [onGroundAircraftCount, setOnGroundAircraftCount] = useState(10);
  const [inboundAircraftCount, setInboundAircraftCount] = useState(10);
  const [remainingOnGroundAircraftCount, setRemainingOnGroundAircraftCount] =
    useState(5);
  const [baseSeparationSeconds, setBaseSeparationSeconds] = useState(45);
  const [wakePercent, setWakePercent] = useState(100);
  const [weatherPercent, setWeatherPercent] = useState(100);
  const [weatherIntervalCount, setWeatherIntervalCount] = useState(4);
  const [minWeatherIntervalMinutes, setMinWeatherIntervalMinutes] =
    useState(60);
  const [weatherDifficulty, setWeatherDifficulty] = useState(1);

  const selectedScenario = useMemo(() => {
    return scenarios.find((s) => s.id === selectedScenarioId) ?? null;
  }, [scenarios, selectedScenarioId]);

  const hasValidSelectedScenario = !!selectedScenario;

  useEffect(() => {
    const airportIdFromUrl = searchParams.get("airportId");
    const scenarioIdFromUrl = searchParams.get("scenarioId");

    if (airportIdFromUrl) {
      setAirportId(airportIdFromUrl);
      localStorage.setItem(STORAGE_AIRPORT_ID, airportIdFromUrl);
    }

    if (scenarioIdFromUrl) {
      setSelectedScenarioId(scenarioIdFromUrl);
      localStorage.setItem(STORAGE_SCENARIO_ID, scenarioIdFromUrl);
    }

    const storedAirportName = localStorage.getItem(STORAGE_AIRPORT_NAME) || "";
    const storedScenarioName =
      localStorage.getItem(STORAGE_SCENARIO_NAME) || "";

    if (storedAirportName) setAirportName(storedAirportName);
    if (storedScenarioName) setSelectedScenarioName(storedScenarioName);
  }, [searchParams]);

  useEffect(() => {
    const params = new URLSearchParams();

    if (airportId) {
      params.set("airportId", airportId);
      localStorage.setItem(STORAGE_AIRPORT_ID, airportId);
    } else {
      localStorage.removeItem(STORAGE_AIRPORT_ID);
    }

    if (selectedScenarioId) {
      params.set("scenarioId", selectedScenarioId);
      localStorage.setItem(STORAGE_SCENARIO_ID, selectedScenarioId);
    } else {
      localStorage.removeItem(STORAGE_SCENARIO_ID);
    }

    setSearchParams(params, { replace: true });
  }, [airportId, selectedScenarioId, setSearchParams]);

  useEffect(() => {
    if (!selectedScenario) return;
    setSelectedScenarioName(selectedScenario.name);
    localStorage.setItem(STORAGE_SCENARIO_NAME, selectedScenario.name);
  }, [selectedScenario]);

  function toUtcString(localValue: string) {
    if (!localValue) return null;
    return new Date(localValue).toISOString();
  }

  function formatDate(value: string) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleString();
  }

  function flightTypeLabel(type: number | string) {
    if (type === 0 || type === "0") return "Arrival";
    if (type === 1 || type === "1") return "Departure";
    return String(type);
  }

  async function getScenarioDataById(scenarioId: string) {
    try {
      setError(null);
      setLoadingScenarioData(true);

      if (!scenarioId.trim()) {
        throw new Error("Select a scenario first");
      }

      const res = await fetch(`/api/scenarios/configs/${scenarioId}`);
      if (!res.ok) {
        throw new Error(`Failed to load scenario data: ${res.status}`);
      }

      const data = (await res.json()) as ScenarioConfigAllDataDto;
      setScenarioData(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setLoadingScenarioData(false);
    }
  }
  function weatherConditionLabel(value?: number | string) {
    if (value === undefined || value === null || value === "") return "-";
    return String(value);
  }

  function clearScenarioSelection() {
    setSelectedScenarioId("");
    setSelectedScenarioName("");
    setScenarioData(null);
    setShowFlightsModal(false);
    setShowWeatherModal(false);
    localStorage.removeItem(STORAGE_SCENARIO_ID);
    localStorage.removeItem(STORAGE_SCENARIO_NAME);
  }

  async function selectScenario(s: ScenarioConfigDto) {
    setSelectedScenarioId(s.id);
    setSelectedScenarioName(s.name);
    setShowFlightsModal(false);
    setShowWeatherModal(false);

    localStorage.setItem(STORAGE_SCENARIO_ID, s.id);
    localStorage.setItem(STORAGE_SCENARIO_NAME, s.name);

    await getScenarioDataById(s.id);
  }

  async function showScenarios() {
    try {
      setError(null);
      setLoadingScenarios(true);

      if (!airportId.trim()) {
        throw new Error("No airport selected");
      }

      const res = await fetch("/api/scenarios/configs");
      if (!res.ok) {
        throw new Error(`Failed to load scenarios: ${res.status}`);
      }

      const data = (await res.json()) as ScenarioConfigDto[];
      const filtered = data.filter((s) => s.airportId === airportId.trim());

      setScenarios(filtered);
      setScenariosLoaded(true);

      if (selectedScenarioId) {
        const found = filtered.find((s) => s.id === selectedScenarioId);
        if (!found) {
          clearScenarioSelection();
        }
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setLoadingScenarios(false);
    }
  }

  async function createScenario() {
    try {
      setError(null);
      setCreatingScenario(true);

      if (!airportId.trim()) {
        throw new Error("No airport selected");
      }

      const res = await fetch("/api/scenarios/configs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          airportId: airportId.trim(),
          name: name.trim(),
          difficulty,
          startTime: toUtcString(startTime),
          endTime: toUtcString(endTime),
          seed,
          aircraftCount,
          aircraftDifficulty,
          onGroundAircraftCount,
          inboundAircraftCount,
          remainingOnGroundAircraftCount,
          baseSeparationSeconds,
          wakePercent,
          weatherPercent,
          weatherIntervalCount,
          minWeatherIntervalMinutes,
          weatherDifficulty,
        }),
      });

      if (!res.ok) {
        throw new Error(`Failed to create scenario: ${res.status}`);
      }

      const created = (await res.json()) as ScenarioConfigDto;

      setShowCreateScenario(false);
      setName("");
      setDifficulty(1);
      setStartTime("");
      setEndTime("");
      setSeed(0);
      setAircraftCount(20);
      setAircraftDifficulty(1);
      setOnGroundAircraftCount(10);
      setInboundAircraftCount(10);
      setRemainingOnGroundAircraftCount(5);
      setBaseSeparationSeconds(45);
      setWakePercent(100);
      setWeatherPercent(100);
      setWeatherIntervalCount(4);
      setMinWeatherIntervalMinutes(60);
      setWeatherDifficulty(1);

      if (scenariosLoaded) {
        setScenarios((prev) => [created, ...prev]);
      }

      selectScenario(created);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setCreatingScenario(false);
    }
  }

  async function deleteScenario(scenarioId: string) {
    try {
      setError(null);

      const res = await fetch(`/api/scenarios/configs/${scenarioId}`, {
        method: "DELETE",
      });

      if (!res.ok && res.status !== 204) {
        throw new Error(`Failed to delete scenario: ${res.status}`);
      }

      setScenarios((prev) => prev.filter((s) => s.id !== scenarioId));

      if (selectedScenarioId === scenarioId) {
        clearScenarioSelection();
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    }
  }

async function generateWeatherIntervals() {
  try {
    setError(null);

    if (!selectedScenarioId.trim()) {
      throw new Error("Select a scenario first");
    }

    const res = await fetch(
      `/api/weatherintervals/generate/${selectedScenarioId}`,
      {
        method: "POST",
      },
    );

    if (!res.ok) {
      throw new Error(`Failed to generate weather intervals: ${res.status}`);
    }

    await getScenarioDataById(selectedScenarioId);
    setShowWeatherModal(true);
  } catch (e) {
    setError(e instanceof Error ? e.message : "Unknown error");
  }
}

 async function generateFlights() {
  try {
    setError(null);

    if (!selectedScenarioId.trim()) {
      throw new Error("Select a scenario first");
    }

    const res = await fetch(`/api/flights/generate/${selectedScenarioId}`, {
      method: "POST",
    });

    if (!res.ok) {
      throw new Error(`Failed to generate flights: ${res.status}`);
    }

    await getScenarioDataById(selectedScenarioId);
    setShowFlightsModal(true);
  } catch (e) {
    setError(e instanceof Error ? e.message : "Unknown error");
  }
}

  async function openFlightsModal() {
  if (!selectedScenarioId.trim()) {
    setError("Select a scenario first");
    return;
  }

  if (!scenarioData || scenarioData.id !== selectedScenarioId) {
    await getScenarioDataById(selectedScenarioId);
  }

  setShowFlightsModal(true);
}

async function openWeatherModal() {
  if (!selectedScenarioId.trim()) {
    setError("Select a scenario first");
    return;
  }

  if (!scenarioData || scenarioData.id !== selectedScenarioId) {
    await getScenarioDataById(selectedScenarioId);
  }

  setShowWeatherModal(true);
}

  return (
    <div style={pageStyle}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "16px",
          marginBottom: "18px",
          flexWrap: "wrap",
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Scenario Config</h1>
        </div>

        <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
          <button
            onClick={showScenarios}
            style={secondaryBtn}
            disabled={loadingScenarios}
          >
            Show scenarios
          </button>

          <button
            onClick={() => setShowCreateScenario(true)}
            style={primaryBtn}
            disabled={!airportId}
          >
            Create scenario
          </button>
        </div>
      </div>

      {error && (
        <div
          style={{
            ...cardStyle,
            borderColor: "rgba(255,80,80,0.45)",
            color: "#ff9d9d",
            marginBottom: "16px",
          }}
        >
          {error}
        </div>
      )}

      <div
        style={{
          ...cardStyle,
          marginBottom: "16px",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "18px",
          flexWrap: "wrap",
          padding: "16px 18px",
        }}
      >
        <div>
          <div style={{ fontSize: "13px", opacity: 0.7 }}>Selected airport</div>
          <div style={{ fontSize: "20px", fontWeight: 800, marginTop: "4px" }}>
            {airportName || "No airport selected"}
          </div>

          <div style={{ marginTop: "10px" }}>
            <div style={{ fontSize: "13px", opacity: 0.7 }}>
              Selected scenario
            </div>
            <div
              style={{ fontSize: "17px", fontWeight: 700, marginTop: "4px" }}
            >
              {selectedScenario?.name ||
                selectedScenarioName ||
                "No scenario selected"}
            </div>
          </div>
        </div>

        <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
          <button
            onClick={openFlightsModal}
            style={secondaryBtn}
            disabled={!hasValidSelectedScenario || loadingScenarioData}
          >
            View flights
          </button>

          <button
            onClick={openWeatherModal}
            style={secondaryBtn}
            disabled={!hasValidSelectedScenario || loadingScenarioData}
          >
            View weather
          </button>

          <button
            onClick={generateWeatherIntervals}
            style={secondaryBtn}
            disabled={!hasValidSelectedScenario}
          >
            Generate weather
          </button>

          <button
            onClick={generateFlights}
            style={primaryBtn}
            disabled={!hasValidSelectedScenario}
          >
            Generate flights
          </button>

          <button
            onClick={clearScenarioSelection}
            style={dangerBtn}
            disabled={!hasValidSelectedScenario}
          >
            Clear selection
          </button>
        </div>
      </div>

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr",
          gap: "16px",
          alignItems: "start",
        }}
      >
        <div>
          <div
            style={{
              fontSize: "19px",
              fontWeight: 800,
              marginBottom: "12px",
            }}
          >
            Scenarios
          </div>

          {!airportId ? (
            <div style={cardStyle}>
              No airport selected. Go back to Airports page first.
            </div>
          ) : !scenariosLoaded ? (
            <div style={cardStyle}>
              Scenarios are hidden by default. Click <b>Show scenarios</b>.
            </div>
          ) : scenarios.length === 0 ? (
            <div style={cardStyle}>No scenarios for the selected airport.</div>
          ) : (
            <div style={{ display: "grid", gap: "12px" }}>
              {scenarios.map((s) => {
                const isSelected = selectedScenarioId === s.id;

                return (
                  <div
                    key={s.id}
                    style={{
                      ...cardStyle,
                      borderColor: isSelected
                        ? "rgba(15,118,110,0.95)"
                        : "rgba(255,255,255,0.12)",
                      padding: "14px",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        gap: "12px",
                        alignItems: "flex-start",
                      }}
                    >
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div
                          style={{
                            fontSize: "17px",
                            fontWeight: 800,
                            wordBreak: "break-word",
                          }}
                        >
                          {s.name}
                        </div>

                        <div
                          style={{
                            marginTop: "8px",
                            opacity: 0.86,
                            fontSize: "13px",
                            display: "grid",
                            gap: "3px",
                          }}
                        >
                          <div>Difficulty: {s.difficulty}</div>
                          <div>Start: {formatDate(s.startTime)}</div>
                          <div>End: {formatDate(s.endTime)}</div>
                          <div>Seed: {s.seed}</div>
                          <div>Aircraft count: {s.aircraftCount}</div>
                          <div>Aircraft difficulty: {s.aircraftDifficulty}</div>
                          <div>On ground: {s.onGroundAircraftCount}</div>
                          <div>Inbound: {s.inboundAircraftCount}</div>
                          <div>
                            Remaining on ground:{" "}
                            {s.remainingOnGroundAircraftCount}
                          </div>
                          <div>Base separation: {s.baseSeparationSeconds}s</div>
                          <div>Wake: {s.wakePercent}%</div>
                          <div>Weather: {s.weatherPercent}%</div>
                          <div>Weather intervals: {s.weatherIntervalCount}</div>
                          <div>
                            Min interval minutes: {s.minWeatherIntervalMinutes}
                          </div>
                          <div>Weather difficulty: {s.weatherDifficulty}</div>
                        </div>
                      </div>

                      <div
                        style={{
                          display: "flex",
                          flexDirection: "column",
                          gap: "8px",
                          flexShrink: 0,
                        }}
                      >
                        <button
                          onClick={() => selectScenario(s)}
                          style={
                            isSelected ? primaryBtnSmall : secondaryBtnSmall
                          }
                          disabled={loadingScenarioData}
                        >
                          {isSelected ? "Selected" : "Select"}
                        </button>

                        <button
                          onClick={() => deleteScenario(s.id)}
                          style={dangerBtnSmall}
                        >
                          Delete
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {showFlightsModal && (
        <LargeModal title="Flights" onClose={() => setShowFlightsModal(false)}>
          {!scenarioData || scenarioData.flights.length === 0 ? (
            <div style={{ opacity: 0.8 }}>No flights.</div>
          ) : (
            <TableWrapper>
              <table style={tableStyle}>
                <thead>
                  <tr>
                    <th style={thStyle}>Callsign</th>
                    <th style={thStyle}>Type</th>
                    <th style={thStyle}>Scheduled</th>
                    <th style={thStyle}>Max Delay</th>
                    <th style={thStyle}>Max Early</th>
                    <th style={thStyle}>Priority</th>
                  </tr>
                </thead>
                <tbody>
                  {scenarioData.flights.map((f) => (
                    <tr key={f.id}>
                      <td style={tdStyle}>{f.callsign}</td>
                      <td style={tdStyle}>{flightTypeLabel(f.type)}</td>
                      <td style={tdStyle}>{formatDate(f.scheduledTime)}</td>
                      <td style={tdStyle}>{f.maxDelayMinutes} min</td>
                      <td style={tdStyle}>{f.maxEarlyMinutes} min</td>
                      <td style={tdStyle}>{f.priority}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableWrapper>
          )}
        </LargeModal>
      )}

      {showWeatherModal && (
        <LargeModal
          title="Weather Intervals"
          onClose={() => setShowWeatherModal(false)}
        >
          {!scenarioData || scenarioData.weatherIntervals.length === 0 ? (
            <div style={{ opacity: 0.8 }}>No weather intervals.</div>
          ) : (
            <TableWrapper>
              <table style={tableStyle}>
                <thead>
                  <tr>
                    <th style={thStyle}>Start</th>
                    <th style={thStyle}>End</th>
                    <th style={thStyle}>Condition</th>
                  </tr>
                </thead>
                <tbody>
                  {scenarioData.weatherIntervals.map((w) => (
                    <tr key={w.id}>
                      <td style={tdStyle}>{formatDate(w.startTime)}</td>
                      <td style={tdStyle}>{formatDate(w.endTime)}</td>
                      <td style={tdStyle}>
                        {weatherConditionLabel(
                          w.condition ?? w.weatherType ?? "",
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableWrapper>
          )}
        </LargeModal>
      )}

      {showCreateScenario && (
        <Modal
          onClose={() => setShowCreateScenario(false)}
          title="Create Scenario"
        >
          <div style={{ display: "grid", gap: "12px" }}>
            <Field label="Scenario name">
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                style={inputStyle}
              />
            </Field>

            <Field label="Difficulty">
              <input
                type="number"
                value={difficulty}
                onChange={(e) => setDifficulty(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="Start time">
              <input
                type="datetime-local"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                style={inputStyle}
              />
            </Field>

            <Field label="End time">
              <input
                type="datetime-local"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                style={inputStyle}
              />
            </Field>

            <Field label="Seed">
              <input
                type="number"
                value={seed}
                onChange={(e) => setSeed(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="Aircraft count">
              <input
                type="number"
                value={aircraftCount}
                onChange={(e) => setAircraftCount(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="Aircraft difficulty">
              <input
                type="number"
                value={aircraftDifficulty}
                onChange={(e) => setAircraftDifficulty(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="On ground aircraft count">
              <input
                type="number"
                value={onGroundAircraftCount}
                onChange={(e) =>
                  setOnGroundAircraftCount(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Inbound aircraft count">
              <input
                type="number"
                value={inboundAircraftCount}
                onChange={(e) =>
                  setInboundAircraftCount(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Remaining on ground aircraft count">
              <input
                type="number"
                value={remainingOnGroundAircraftCount}
                onChange={(e) =>
                  setRemainingOnGroundAircraftCount(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Base separation seconds">
              <input
                type="number"
                value={baseSeparationSeconds}
                onChange={(e) =>
                  setBaseSeparationSeconds(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Wake percent">
              <input
                type="number"
                value={wakePercent}
                onChange={(e) => setWakePercent(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="Weather percent">
              <input
                type="number"
                value={weatherPercent}
                onChange={(e) => setWeatherPercent(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <Field label="Weather interval count">
              <input
                type="number"
                value={weatherIntervalCount}
                onChange={(e) =>
                  setWeatherIntervalCount(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Min weather interval minutes">
              <input
                type="number"
                value={minWeatherIntervalMinutes}
                onChange={(e) =>
                  setMinWeatherIntervalMinutes(Number(e.target.value))
                }
                style={inputStyle}
              />
            </Field>

            <Field label="Weather difficulty">
              <input
                type="number"
                value={weatherDifficulty}
                onChange={(e) => setWeatherDifficulty(Number(e.target.value))}
                style={inputStyle}
              />
            </Field>

            <div
              style={{
                display: "flex",
                justifyContent: "flex-end",
                gap: "10px",
                marginTop: "8px",
              }}
            >
              <button
                onClick={() => setShowCreateScenario(false)}
                style={secondaryBtn}
              >
                Cancel
              </button>

              <button
                onClick={createScenario}
                disabled={
                  creatingScenario || !airportId || name.trim().length === 0
                }
                style={{
                  ...primaryBtn,
                  opacity:
                    creatingScenario || !airportId || name.trim().length === 0
                      ? 0.6
                      : 1,
                }}
              >
                {creatingScenario ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

function Modal({
  title,
  children,
  onClose,
}: {
  title: string;
  children: React.ReactNode;
  onClose: () => void;
}) {
  return (
    <div
      onClick={onClose}
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.55)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 999,
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "720px",
          maxWidth: "calc(100vw - 24px)",
          maxHeight: "85vh",
          overflowY: "auto",
          borderRadius: "16px",
          padding: "18px",
          background: "#111",
          border: "1px solid rgba(255,255,255,0.12)",
        }}
      >
        <div
          style={{ fontWeight: 900, fontSize: "20px", marginBottom: "14px" }}
        >
          {title}
        </div>
        {children}
      </div>
    </div>
  );
}

function LargeModal({
  title,
  children,
  onClose,
}: {
  title: string;
  children: React.ReactNode;
  onClose: () => void;
}) {
  return (
    <div
      onClick={onClose}
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.62)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 1200,
        padding: "20px",
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "1200px",
          maxWidth: "95vw",
          maxHeight: "88vh",
          overflow: "hidden",
          borderRadius: "18px",
          padding: "18px",
          background: "#111",
          border: "1px solid rgba(255,255,255,0.12)",
          display: "flex",
          flexDirection: "column",
          gap: "14px",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            gap: "12px",
          }}
        >
          <div style={{ fontWeight: 900, fontSize: "22px" }}>{title}</div>

          <button onClick={onClose} style={secondaryBtn}>
            Close
          </button>
        </div>

        <div style={{ minHeight: 0, flex: 1 }}>{children}</div>
      </div>
    </div>
  );
}

function TableWrapper({ children }: { children: React.ReactNode }) {
  return (
    <div
      style={{
        border: "1px solid rgba(255,255,255,0.12)",
        borderRadius: "14px",
        overflow: "auto",
        maxHeight: "68vh",
        background: "rgba(255,255,255,0.03)",
      }}
    >
      {children}
    </div>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div style={{ marginBottom: "6px", fontWeight: 700 }}>{label}</div>
      {children}
    </div>
  );
}

const pageStyle: React.CSSProperties = {
  maxWidth: "1500px",
  margin: "0 auto",
  padding: "8px 6px 36px",
};

const cardStyle: React.CSSProperties = {
  border: "1px solid rgba(255,255,255,0.12)",
  borderRadius: "16px",
  background: "rgba(255,255,255,0.04)",
  padding: "18px",
};

const inputStyle: React.CSSProperties = {
  width: "100%",
  padding: "10px 12px",
  borderRadius: "10px",
  border: "1px solid rgba(255,255,255,0.16)",
  background: "rgba(0,0,0,0.28)",
  color: "white",
  outline: "none",
};

const tableStyle: React.CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  minWidth: "760px",
};

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "12px 14px",
  borderBottom: "1px solid rgba(255,255,255,0.12)",
  position: "sticky",
  top: 0,
  background: "#151515",
  fontSize: "13px",
};

const tdStyle: React.CSSProperties = {
  padding: "12px 14px",
  borderBottom: "1px solid rgba(255,255,255,0.08)",
  fontSize: "13px",
  verticalAlign: "top",
};

const primaryBtn: React.CSSProperties = {
  background: "#0f766e",
  color: "white",
  border: "none",
  borderRadius: "12px",
  padding: "10px 14px",
  cursor: "pointer",
  fontWeight: 700,
};

const secondaryBtn: React.CSSProperties = {
  background: "transparent",
  color: "white",
  border: "1px solid rgba(255,255,255,0.16)",
  borderRadius: "12px",
  padding: "10px 14px",
  cursor: "pointer",
  fontWeight: 700,
};

const dangerBtn: React.CSSProperties = {
  background: "transparent",
  color: "#ff7a7a",
  border: "1px solid rgba(255,122,122,0.5)",
  borderRadius: "12px",
  padding: "10px 14px",
  cursor: "pointer",
  fontWeight: 700,
};

const primaryBtnSmall: React.CSSProperties = {
  background: "#0f766e",
  color: "white",
  border: "none",
  borderRadius: "10px",
  padding: "8px 10px",
  cursor: "pointer",
  fontWeight: 700,
  fontSize: "13px",
};

const secondaryBtnSmall: React.CSSProperties = {
  background: "transparent",
  color: "white",
  border: "1px solid rgba(255,255,255,0.16)",
  borderRadius: "10px",
  padding: "8px 10px",
  cursor: "pointer",
  fontWeight: 700,
  fontSize: "13px",
};

const dangerBtnSmall: React.CSSProperties = {
  background: "transparent",
  color: "#ff7a7a",
  border: "1px solid rgba(255,122,122,0.5)",
  borderRadius: "10px",
  padding: "8px 10px",
  cursor: "pointer",
  fontWeight: 700,
  fontSize: "13px",
};
