import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { apiFetch } from "../lib/api";
import { C, S } from "../styles/tokens";
import { Modal } from "../components/Modal";
import { SkeletonCard } from "../components/Skeleton";
import { useToast } from "../hooks/useToast";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { NumberInput } from "../components/NumberInput";

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

type ScenarioConfigAllDataDto = ScenarioConfigDto & {
  flights: FlightDto[];
  weatherIntervals: WeatherIntervalsDto[];
};

const STORAGE_AIRPORT_ID = "selectedAirportId";
const STORAGE_AIRPORT_NAME = "selectedAirportName";
const STORAGE_SCENARIO_ID = "selectedScenarioId";
const STORAGE_SCENARIO_NAME = "selectedScenarioName";

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

function weatherConditionLabel(val: number | string | undefined): string {
  const num = Number(val);
  switch (num) {
    case 0: return "Clear";
    case 1: return "Cloud";
    case 2: return "Rain";
    case 3: return "Snow";
    case 4: return "Fog";
    case 5: return "Storm";
    default: return String(val ?? "-");
  }
}

function toUtcString(localValue: string) {
  if (!localValue) return null;
  return new Date(localValue).toISOString();
}

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "10px 14px",
  borderBottom: `1px solid ${C.border}`,
  position: "sticky",
  top: 0,
  background: "#0a0a0a",
  fontSize: "11px",
  color: C.textSub,
  textTransform: "uppercase",
  letterSpacing: "1px",
  fontWeight: 700,
};

const tdStyle: React.CSSProperties = {
  padding: "10px 14px",
  borderBottom: `1px solid ${C.border}`,
  fontSize: "13px",
  verticalAlign: "top",
  color: C.text,
};

function LargeModal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return (
    <div
      className="glass-modal-backdrop"
      style={{ padding: "20px" }}
      onClick={onClose}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        className="glass-modal-panel"
        style={{ width: "1200px", maxWidth: "95vw", maxHeight: "88vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <div style={{ fontWeight: 800, fontSize: "16px" }}>{title}</div>
          <button onClick={onClose} className="glass-btn-ghost">Close</button>
        </div>
        <div style={{ minHeight: 0, flex: 1, overflowY: "auto" }}>{children}</div>
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div style={{ ...S.label, marginBottom: "6px" }}>{label}</div>
      {children}
    </div>
  );
}

export default function ScenarioConfigPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { showToast } = useToast();

  const [confirmDeleteScenario, setConfirmDeleteScenario] = useState<{ id: string; name: string } | null>(null);

  const [airportId, setAirportId] = useState(
    searchParams.get("airportId") || localStorage.getItem(STORAGE_AIRPORT_ID) || "",
  );
  const [airportName, setAirportName] = useState(localStorage.getItem(STORAGE_AIRPORT_NAME) || "");

  const [selectedScenarioId, setSelectedScenarioId] = useState(
    searchParams.get("scenarioId") || localStorage.getItem(STORAGE_SCENARIO_ID) || "",
  );
  const [selectedScenarioName, setSelectedScenarioName] = useState(localStorage.getItem(STORAGE_SCENARIO_NAME) || "");

  const [scenarios, setScenarios] = useState<ScenarioConfigDto[]>([]);
  const [scenarioData, setScenarioData] = useState<ScenarioConfigAllDataDto | null>(null);

  const [loadingScenarios, setLoadingScenarios] = useState(false);
  const [loadingScenarioData, setLoadingScenarioData] = useState(false);

  const [showCreateScenario, setShowCreateScenario] = useState(false);
  const [creatingScenario, setCreatingScenario] = useState(false);

  const [showFlightsModal, setShowFlightsModal] = useState(false);
  const [showWeatherModal, setShowWeatherModal] = useState(false);

  // Form state
  const [name, setName] = useState("");
  const [difficulty, setDifficulty] = useState(1);
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [seed, setSeed] = useState(0);
  const [aircraftCount, setAircraftCount] = useState(20);
  const [aircraftDifficulty, setAircraftDifficulty] = useState(1);
  const [onGroundAircraftCount, setOnGroundAircraftCount] = useState(10);
  const [inboundAircraftCount, setInboundAircraftCount] = useState(10);
  const [remainingOnGroundAircraftCount, setRemainingOnGroundAircraftCount] = useState(5);
  const [baseSeparationSeconds, setBaseSeparationSeconds] = useState(45);
  const [wakePercent, setWakePercent] = useState(100);
  const [weatherPercent, setWeatherPercent] = useState(100);
  const [weatherIntervalCount, setWeatherIntervalCount] = useState(4);
  const [minWeatherIntervalMinutes, setMinWeatherIntervalMinutes] = useState(60);
  const [weatherDifficulty, setWeatherDifficulty] = useState(1);

  const selectedScenario = useMemo(
    () => scenarios.find((s) => s.id === selectedScenarioId) ?? null,
    [scenarios, selectedScenarioId],
  );

  const hasValidSelectedScenario = !!selectedScenario;

  useEffect(() => {
    const airportIdFromUrl = searchParams.get("airportId");
    const scenarioIdFromUrl = searchParams.get("scenarioId");
    if (airportIdFromUrl) { setAirportId(airportIdFromUrl); localStorage.setItem(STORAGE_AIRPORT_ID, airportIdFromUrl); }
    if (scenarioIdFromUrl) { setSelectedScenarioId(scenarioIdFromUrl); localStorage.setItem(STORAGE_SCENARIO_ID, scenarioIdFromUrl); }
    const storedAirportName = localStorage.getItem(STORAGE_AIRPORT_NAME) || "";
    const storedScenarioName = localStorage.getItem(STORAGE_SCENARIO_NAME) || "";
    if (storedAirportName) setAirportName(storedAirportName);
    if (storedScenarioName) setSelectedScenarioName(storedScenarioName);
  }, [searchParams]);

  useEffect(() => {
    const params = new URLSearchParams();
    if (airportId) { params.set("airportId", airportId); localStorage.setItem(STORAGE_AIRPORT_ID, airportId); }
    else { localStorage.removeItem(STORAGE_AIRPORT_ID); }
    if (selectedScenarioId) { params.set("scenarioId", selectedScenarioId); localStorage.setItem(STORAGE_SCENARIO_ID, selectedScenarioId); }
    else { localStorage.removeItem(STORAGE_SCENARIO_ID); }
    setSearchParams(params, { replace: true });
  }, [airportId, selectedScenarioId, setSearchParams]);

  useEffect(() => {
    if (!selectedScenario) return;
    setSelectedScenarioName(selectedScenario.name);
    localStorage.setItem(STORAGE_SCENARIO_NAME, selectedScenario.name);
  }, [selectedScenario]);

  // Auto-load scenarios when airportId is available
  useEffect(() => {
    if (airportId) showScenarios();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [airportId]);

  async function getScenarioDataById(scenarioId: string) {
    try {
      setLoadingScenarioData(true);
      if (!scenarioId.trim()) throw new Error("Select a scenario first");
      const res = await apiFetch(`/api/scenarios/configs/${scenarioId}`);
      if (!res.ok) throw new Error(`Failed to load scenario data (${res.status})`);
      const data = (await res.json()) as ScenarioConfigAllDataDto;
      setScenarioData(data);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load scenario data", "error");
    } finally {
      setLoadingScenarioData(false);
    }
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
      setLoadingScenarios(true);
      if (!airportId.trim()) throw new Error("No airport selected");
      const res = await apiFetch("/api/scenarios/configs");
      if (!res.ok) throw new Error(`Failed to load scenarios (${res.status})`);
      const data = (await res.json()) as ScenarioConfigDto[];
      const filtered = data.filter((s) => s.airportId === airportId.trim());
      setScenarios(filtered);
      if (selectedScenarioId) {
        const found = filtered.find((s) => s.id === selectedScenarioId);
        if (!found) clearScenarioSelection();
      }
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load scenarios", "error");
    } finally {
      setLoadingScenarios(false);
    }
  }

  async function createScenario() {
    try {
      setCreatingScenario(true);
      if (!airportId.trim()) throw new Error("No airport selected");
      const res = await apiFetch("/api/scenarios/configs", {
        method: "POST",
        body: JSON.stringify({
          airportId: airportId.trim(), name: name.trim(), difficulty,
          startTime: toUtcString(startTime), endTime: toUtcString(endTime), seed,
          aircraftCount, aircraftDifficulty, onGroundAircraftCount, inboundAircraftCount,
          remainingOnGroundAircraftCount, baseSeparationSeconds, wakePercent,
          weatherPercent, weatherIntervalCount, minWeatherIntervalMinutes, weatherDifficulty,
        }),
      });
      if (!res.ok) throw new Error(`Failed to create scenario (${res.status})`);
      const created = (await res.json()) as ScenarioConfigDto;
      setShowCreateScenario(false);
      setName(""); setDifficulty(1); setStartTime(""); setEndTime(""); setSeed(0);
      setAircraftCount(20); setAircraftDifficulty(1); setOnGroundAircraftCount(10);
      setInboundAircraftCount(10); setRemainingOnGroundAircraftCount(5);
      setBaseSeparationSeconds(45); setWakePercent(100); setWeatherPercent(100);
      setWeatherIntervalCount(4); setMinWeatherIntervalMinutes(60); setWeatherDifficulty(1);
      setScenarios((prev) => [created, ...prev]);
      await selectScenario(created);
      showToast("Scenario created", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to create scenario", "error");
    } finally {
      setCreatingScenario(false);
    }
  }

  async function deleteScenario(scenarioId: string) {
    try {
      const res = await apiFetch(`/api/scenarios/configs/${scenarioId}`, { method: "DELETE" });
      if (!res.ok && res.status !== 204) throw new Error(`Failed to delete scenario (${res.status})`);
      setScenarios((prev) => prev.filter((s) => s.id !== scenarioId));
      if (selectedScenarioId === scenarioId) clearScenarioSelection();
      showToast("Scenario deleted", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to delete scenario", "error");
    }
  }

  async function generateWeatherIntervals() {
    try {
      if (!selectedScenarioId.trim()) throw new Error("Select a scenario first");
      const res = await apiFetch(`/api/weatherintervals/generate/${selectedScenarioId}`, { method: "POST" });
      if (!res.ok) throw new Error(`Failed to generate weather intervals (${res.status})`);
      await getScenarioDataById(selectedScenarioId);
      setShowWeatherModal(true);
      showToast("Weather intervals generated", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to generate weather", "error");
    }
  }

  async function generateFlights() {
    try {
      if (!selectedScenarioId.trim()) throw new Error("Select a scenario first");
      const res = await apiFetch(`/api/flights/generate/${selectedScenarioId}`, { method: "POST" });
      if (!res.ok) throw new Error(`Failed to generate flights (${res.status})`);
      await getScenarioDataById(selectedScenarioId);
      setShowFlightsModal(true);
      showToast("Flights generated", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to generate flights", "error");
    }
  }

  async function openFlightsModal() {
    if (!selectedScenarioId.trim()) { showToast("Select a scenario first", "error"); return; }
    if (!scenarioData || scenarioData.id !== selectedScenarioId) await getScenarioDataById(selectedScenarioId);
    setShowFlightsModal(true);
  }

  async function openWeatherModal() {
    if (!selectedScenarioId.trim()) { showToast("Select a scenario first", "error"); return; }
    if (!scenarioData || scenarioData.id !== selectedScenarioId) await getScenarioDataById(selectedScenarioId);
    setShowWeatherModal(true);
  }

  return (
    <div style={{ maxWidth: "1500px", margin: "0 auto" }}>
      {/* Page header */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "24px", flexWrap: "wrap", gap: "12px" }}>
        <div>
          <div style={S.label}>SCENARIO MANAGEMENT</div>
          <h1 style={{ margin: "6px 0 0", fontSize: "22px", fontWeight: 800 }}>Scenarios</h1>
        </div>
        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
          <button onClick={() => setShowCreateScenario(true)} className="glass-btn-primary" style={{ opacity: !airportId ? 0.5 : 1 }} disabled={!airportId}>
            + Create scenario
          </button>
        </div>
      </div>

      {/* Context bar */}
      <div className="glass-card--selected" style={{ marginBottom: "20px" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "16px" }}>
          <div style={{ display: "flex", gap: "32px", flexWrap: "wrap" }}>
            <div>
              <div style={S.label}>Airport</div>
              <div style={{ fontSize: "16px", fontWeight: 700, marginTop: "4px", color: airportName ? C.text : C.textMuted }}>
                {airportName || "None selected"}
              </div>
            </div>
            <div>
              <div style={S.label}>Scenario</div>
              <div style={{ fontSize: "16px", fontWeight: 700, marginTop: "4px", color: selectedScenario ? C.text : C.textMuted }}>
                {selectedScenario?.name || selectedScenarioName || "None selected"}
              </div>
            </div>
            {selectedScenario && (
              <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: "8px" }}>
                {[
                  { label: "Aircraft", value: selectedScenario.aircraftCount },
                  { label: "Difficulty", value: selectedScenario.difficulty },
                  { label: "Sep (s)", value: selectedScenario.baseSeparationSeconds },
                ].map((stat) => (
                  <div key={stat.label} className="glass-card" style={{ padding: "6px 10px", textAlign: "center" }}>
                    <div style={S.label}>{stat.label}</div>
                    <div style={{ color: C.primary, fontSize: "14px", fontWeight: 800, marginTop: "2px" }}>{stat.value}</div>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
            <button onClick={openFlightsModal} className="glass-btn-ghost" disabled={!hasValidSelectedScenario || loadingScenarioData}>
              Flights
            </button>
            <button onClick={openWeatherModal} className="glass-btn-ghost" disabled={!hasValidSelectedScenario || loadingScenarioData}>
              Weather
            </button>
            <button onClick={generateWeatherIntervals} className="glass-btn-ghost" disabled={!hasValidSelectedScenario}>
              Gen. weather
            </button>
            <button onClick={generateFlights} className="glass-btn-primary" disabled={!hasValidSelectedScenario}>
              Gen. flights
            </button>
            <button onClick={clearScenarioSelection} className="glass-btn-danger" disabled={!hasValidSelectedScenario}>
              Clear
            </button>
          </div>
        </div>
      </div>

      {/* Scenario list */}
      <div style={S.sectionTitle}>Scenarios</div>

      {!airportId ? (
        <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>
          No airport selected. Go to <a href="/airports" style={{ color: C.primary }}>Airports</a> first.
        </div>
      ) : loadingScenarios ? (
        <div style={{ display: "grid", gap: "12px" }}>
          {[0, 1, 2].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : scenarios.length === 0 ? (
        <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>No scenarios for this airport.</div>
      ) : (
        <div style={{ display: "grid", gap: "10px" }}>
          {scenarios.map((s, index) => {
            const isSelected = selectedScenarioId === s.id;
            return (
              <div
                key={s.id}
                className={isSelected ? "glass-card--selected" : "glass-card"}
                style={{ animation: `fadeInUp 0.3s ease ${index * 60}ms both` }}
              >
                {/* Header row */}
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: "12px", marginBottom: "12px" }}>
                  <div>
                    <div style={{ fontSize: "15px", fontWeight: 700 }}>{s.name}</div>
                    <div style={{ color: C.textSub, fontSize: "11px", marginTop: "3px" }}>
                      {formatDate(s.startTime)} → {formatDate(s.endTime)}
                      <span style={{ color: C.textMuted, marginLeft: "10px" }}>seed {s.seed}</span>
                    </div>
                  </div>
                  <div style={{ display: "flex", gap: "6px", flexShrink: 0 }}>
                    <button onClick={() => selectScenario(s)} className={isSelected ? "glass-btn-primary" : "glass-btn-ghost"} disabled={loadingScenarioData}>
                      {isSelected ? "✓ Selected" : "Select"}
                    </button>
                    <button onClick={() => setConfirmDeleteScenario({ id: s.id, name: s.name })} className="glass-btn-danger">Delete</button>
                  </div>
                </div>

                {/* Stat groups */}
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: "10px" }}>

                  {/* General */}
                  <div>
                    <div style={{ ...S.label, marginBottom: "6px", color: C.primary }}>General</div>
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "5px" }}>
                      {[
                        { label: "Difficulty", value: s.difficulty },
                        { label: "Sep (s)", value: s.baseSeparationSeconds },
                        { label: "Wake %", value: `${s.wakePercent}%` },
                      ].map((stat) => (
                        <div key={stat.label} className="glass-card" style={{ padding: "5px 8px" }}>
                          <div style={{ ...S.label, fontSize: "8px" }}>{stat.label}</div>
                          <div style={{ color: C.text, fontSize: "12px", fontWeight: 700, marginTop: "1px" }}>{stat.value}</div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Aircraft */}
                  <div>
                    <div style={{ ...S.label, marginBottom: "6px", color: C.primary }}>Aircraft</div>
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "5px" }}>
                      {[
                        { label: "Total", value: s.aircraftCount },
                        { label: "Difficulty", value: s.aircraftDifficulty },
                        { label: "On ground", value: s.onGroundAircraftCount },
                        { label: "Inbound", value: s.inboundAircraftCount },
                        { label: "Rem. ground", value: s.remainingOnGroundAircraftCount },
                      ].map((stat) => (
                        <div key={stat.label} className="glass-card" style={{ padding: "5px 8px" }}>
                          <div style={{ ...S.label, fontSize: "8px" }}>{stat.label}</div>
                          <div style={{ color: C.text, fontSize: "12px", fontWeight: 700, marginTop: "1px" }}>{stat.value}</div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Weather */}
                  <div>
                    <div style={{ ...S.label, marginBottom: "6px", color: C.primary }}>Weather</div>
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "5px" }}>
                      {[
                        { label: "Weather %", value: `${s.weatherPercent}%` },
                        { label: "Difficulty", value: s.weatherDifficulty },
                        { label: "Intervals", value: s.weatherIntervalCount },
                        { label: "Min interval", value: `${s.minWeatherIntervalMinutes}m` },
                      ].map((stat) => (
                        <div key={stat.label} className="glass-card" style={{ padding: "5px 8px" }}>
                          <div style={{ ...S.label, fontSize: "8px" }}>{stat.label}</div>
                          <div style={{ color: C.text, fontSize: "12px", fontWeight: 700, marginTop: "1px" }}>{stat.value}</div>
                        </div>
                      ))}
                    </div>
                  </div>

                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Flights modal */}
      {showFlightsModal && (
        <LargeModal title="Flights" onClose={() => setShowFlightsModal(false)}>
          {!scenarioData || scenarioData.flights.length === 0 ? (
            <div style={{ color: C.textSub, fontSize: "13px" }}>No flights generated yet.</div>
          ) : (
            <div style={{ border: `1px solid ${C.border}`, borderRadius: "6px", overflow: "auto", maxHeight: "68vh", background: C.bgCard }}>
              <table style={{ width: "100%", borderCollapse: "collapse", minWidth: "760px" }}>
                <thead>
                  <tr>
                    {["Callsign", "Type", "Scheduled", "Max Delay", "Max Early", "Priority"].map((h) => (
                      <th key={h} style={thStyle}>{h}</th>
                    ))}
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
            </div>
          )}
        </LargeModal>
      )}

      {/* Weather modal */}
      {showWeatherModal && (
        <LargeModal title="Weather Intervals" onClose={() => setShowWeatherModal(false)}>
          {!scenarioData || scenarioData.weatherIntervals.length === 0 ? (
            <div style={{ color: C.textSub, fontSize: "13px" }}>No weather intervals generated yet.</div>
          ) : (
            <div style={{ border: `1px solid ${C.border}`, borderRadius: "6px", overflow: "auto", maxHeight: "68vh", background: C.bgCard }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr>
                    {["Start", "End", "Condition"].map((h) => (
                      <th key={h} style={thStyle}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {scenarioData.weatherIntervals.map((w) => (
                    <tr key={w.id}>
                      <td style={tdStyle}>{formatDate(w.startTime)}</td>
                      <td style={tdStyle}>{formatDate(w.endTime)}</td>
                      <td style={tdStyle}>{weatherConditionLabel(w.condition ?? w.weatherType)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </LargeModal>
      )}

      {/* Delete confirmation */}
      {confirmDeleteScenario && (
        <ConfirmDialog
          message={`Delete scenario "${confirmDeleteScenario.name}"?`}
          onConfirm={async () => { const id = confirmDeleteScenario.id; setConfirmDeleteScenario(null); await deleteScenario(id); }}
          onCancel={() => setConfirmDeleteScenario(null)}
        />
      )}

      {/* Create scenario modal */}
      {showCreateScenario && (
        <Modal onClose={() => setShowCreateScenario(false)} title="Create Scenario">
          <div style={{ display: "grid", gap: "10px", maxHeight: "70vh", overflowY: "auto", paddingRight: "4px" }}>
            <Field label="Scenario name"><input value={name} onChange={(e) => setName(e.target.value)} className="glass-input" /></Field>
            <Field label="Difficulty"><NumberInput value={difficulty} onChange={setDifficulty} className="glass-input" min={1} max={5} /></Field>
            <Field label="Start time"><input type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} className="glass-input" /></Field>
            <Field label="End time"><input type="datetime-local" value={endTime} onChange={(e) => setEndTime(e.target.value)} className="glass-input" /></Field>
            <Field label="Seed"><NumberInput value={seed} onChange={setSeed} className="glass-input" min={0} /></Field>
            <Field label="Aircraft count"><NumberInput value={aircraftCount} onChange={setAircraftCount} className="glass-input" min={1} /></Field>
            <Field label="Aircraft difficulty"><NumberInput value={aircraftDifficulty} onChange={setAircraftDifficulty} className="glass-input" min={1} max={5} /></Field>
            <Field label="On ground aircraft"><NumberInput value={onGroundAircraftCount} onChange={setOnGroundAircraftCount} className="glass-input" min={0} /></Field>
            <Field label="Inbound aircraft"><NumberInput value={inboundAircraftCount} onChange={setInboundAircraftCount} className="glass-input" min={0} /></Field>
            <Field label="Remaining on ground"><NumberInput value={remainingOnGroundAircraftCount} onChange={setRemainingOnGroundAircraftCount} className="glass-input" min={0} /></Field>
            <Field label="Base separation (s)"><NumberInput value={baseSeparationSeconds} onChange={setBaseSeparationSeconds} className="glass-input" min={0} /></Field>
            <Field label="Wake %"><NumberInput value={wakePercent} onChange={setWakePercent} className="glass-input" min={0} max={200} /></Field>
            <Field label="Weather %"><NumberInput value={weatherPercent} onChange={setWeatherPercent} className="glass-input" min={0} max={200} /></Field>
            <Field label="Weather interval count"><NumberInput value={weatherIntervalCount} onChange={setWeatherIntervalCount} className="glass-input" min={0} /></Field>
            <Field label="Min interval (min)"><NumberInput value={minWeatherIntervalMinutes} onChange={setMinWeatherIntervalMinutes} className="glass-input" min={1} /></Field>
            <Field label="Weather difficulty"><NumberInput value={weatherDifficulty} onChange={setWeatherDifficulty} className="glass-input" min={1} max={5} /></Field>
            <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "4px" }}>
              <button onClick={() => setShowCreateScenario(false)} className="glass-btn-ghost">Cancel</button>
              <button onClick={createScenario} className="glass-btn-primary" style={{ opacity: creatingScenario || !airportId || name.trim().length === 0 ? 0.5 : 1 }} disabled={creatingScenario || !airportId || name.trim().length === 0}>
                {creatingScenario ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}
