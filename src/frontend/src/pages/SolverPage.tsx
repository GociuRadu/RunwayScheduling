import { useEffect, useState } from "react";
import scenarioMinimal from "../scenarios/scenario-minimal.json";
import scenario500 from "../scenarios/scenario-500.json";
import { apiFetch } from "../lib/api";
import { formatDate, toLocalDatetime, toUtcString } from "../lib/utils";
import { C, S } from "../styles/tokens";
import { Modal } from "../components/Modal";
import { useToast } from "../hooks/useToast";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { NumberInput } from "../components/NumberInput";

type RandomEventDto = {
  id: string;
  scenarioConfigId: string;
  name: string;
  description: string;
  startTime: string;
  endTime: string;
  impactPercent: number;
};

type ScenarioConfigDto = {
  id: string;
  name: string;
  startTime: string;
  endTime: string;
};

type SolvedFlightDto = {
  flightId: string;
  callsign: string;
  type: number;
  priority: number;
  processingOrder: number;
  scheduledTime: string;
  maxDelayMinutes: number;
  maxEarlyMinutes: number;
  status: number;
  cancellationReason: number;
  assignedRunway: string | null;
  assignedTime: string | null;
  delayMinutes: number;
  earlyMinutes: number;
  separationAppliedSeconds: number;
  weatherAtAssignment: number | null;
  affectedByRandomEvent: boolean;
};

type SolverResultDto = {
  algorithmName: string;
  flights: SolvedFlightDto[];
  totalFlights: number;
  totalScheduledFlights: number;
  totalOnTimeFlights: number;
  totalEarlyFlights: number;
  totalDelayedFlights: number;
  totalCanceledFlights: number;
  totalRescheduledFlights: number;
  canceledNoCompatibleRunway: number;
  canceledOutsideWindow: number;
  canceledExceedsMaxDelay: number;
  totalDelayMinutes: number;
  averageDelayMinutes: number;
  maxDelayMinutes: number;
  totalEarlyMinutes: number;
  fitness: number;
  solveTimeMs: number;
  throughputFlightsPerHour: number;
};

type ComparisonResultDto = {
  greedy: SolverResultDto;
  genetic: SolverResultDto;
};

type BenchmarkEntryDto = {
  id: string;
  scenarioConfigId: string;
  algorithmType: string;
  configIndex: number;
  runTimestampUtc: string;
  fitness: number;
  solveTimeMs: number;
  populationSize: number | null;
  maxGenerations: number | null;
  crossoverRate: number | null;
  mutationRateLocal: number | null;
  mutationRateMemetic: number | null;
  tournamentSize: number | null;
  eliteCount: number | null;
  noImprovementGenerations: number | null;
  randomSeed: number | null;
};

type GaBenchmarkConfigDto = {
  populationSize: number;
  maxGenerations: number;
  crossoverRate: number;
  mutationRateLocal: number;
  mutationRateMemetic: number;
  tournamentSize: number;
  eliteCount: number;
  noImprovementGenerations: number;
  randomSeed: number;
};

type GaBenchmarkEntryDto = {
  scenarioConfigId: string;
  configIndex: number;
  config: GaBenchmarkConfigDto;
  fitness: number;
  solveTimeMs: number;
};

type GaBenchmarkResultDto = {
  entries: GaBenchmarkEntryDto[];
};

function getDefaultBenchmarkJson(sid: string) {
  return JSON.stringify(
    {
      scenarioConfigIds: [sid || "00000000-0000-0000-0000-000000000000"],
      configs: [
        {
          populationSize: 30,
          maxGenerations: 50,
          crossoverRate: 0.85,
          mutationRateLocal: 0.05,
          mutationRateMemetic: 0.0,
          tournamentSize: 4,
          eliteCount: 2,
          noImprovementGenerations: 20,
          cpSatTimeLimitMsMicro: 0,
          cpSatTimeLimitMsMacro: 0,
          cpSatNeighborhoodSize: 3,
        },
      ],
    },
    null,
    2
  );
}

function clampImpactPercent(value: number) {
  return Math.min(100, Math.max(0, value));
}

function flightTypeLabel(t: number) {
  if (t === 0) return "Arrival";
  if (t === 1) return "Departure";
  if (t === 2) return "On Ground";
  return String(t);
}

function statusLabel(s: number) {
  switch (s) {
    case 0: return "Pending";
    case 1: return "Scheduled";
    case 2: return "Delayed";
    case 3: return "Canceled";
    case 4: return "Early";
    case 5: return "Rescheduled";
    default: return String(s);
  }
}

function statusColor(s: number) {
  switch (s) {
    case 1: return C.activeGreen;
    case 2: return C.primary;
    case 3: return C.danger;
    case 4: return "#38bdf8";
    case 5: return "#34d399";
    default: return C.textSub;
  }
}

function cancellationLabel(r: number) {
  switch (r) {
    case 0: return "–";
    case 1: return "No Compatible Runway";
    case 2: return "Outside Scenario Window";
    case 3: return "Exceeds Max Delay";
    default: return String(r);
  }
}

function weatherLabel(v: number | null | undefined) {
  switch (v) {
    case 0: return "Clear";
    case 1: return "Cloud";
    case 2: return "Rain";
    case 3: return "Snow";
    case 4: return "Fog";
    case 5: return "Storm";
    default: return "–";
  }
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

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div style={{ ...S.label, marginBottom: "6px" }}>{label}</div>
      {children}
    </div>
  );
}

function LargeModal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="glass-modal-backdrop" style={{ padding: "20px" }} onClick={onClose}>
      <div
        onClick={(e) => e.stopPropagation()}
        className="glass-modal-panel"
        style={{ width: "1300px", maxWidth: "95vw", maxHeight: "88vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
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

const STORAGE_SCENARIO_ID = "selectedScenarioId";
const STORAGE_SCENARIO_NAME = "selectedScenarioName";
const STORAGE_AIRPORT_NAME = "selectedAirportName";

const EXAMPLE_SCENARIO = JSON.stringify(scenarioMinimal, null, 2);
const SCENARIO_500 = JSON.stringify(scenario500, null, 2);

export default function SolverPage() {
  const { showToast } = useToast();

  const scenarioId = localStorage.getItem(STORAGE_SCENARIO_ID) ?? "";
  const scenarioName = localStorage.getItem(STORAGE_SCENARIO_NAME) ?? "";
  const airportName = localStorage.getItem(STORAGE_AIRPORT_NAME) ?? "";

  const hasScenario = !!scenarioId;

  const [scenarioConfig, setScenarioConfig] = useState<ScenarioConfigDto | null>(null);

  const [events, setEvents] = useState<RandomEventDto[]>([]);
  const [loadingEvents, setLoadingEvents] = useState(false);

  const [showEventModal, setShowEventModal] = useState(false);
  const [editingEvent, setEditingEvent] = useState<RandomEventDto | null>(null);
  const [evName, setEvName] = useState("");
  const [evDescription, setEvDescription] = useState("");
  const [evStartTime, setEvStartTime] = useState("");
  const [evEndTime, setEvEndTime] = useState("");
  const [evImpact, setEvImpact] = useState(100);
  const [savingEvent, setSavingEvent] = useState(false);

  const [confirmDelete, setConfirmDelete] = useState<{ id: string; name: string } | null>(null);

  const [solving, setSolving] = useState(false);
  const [solverResult, setSolverResult] = useState<SolverResultDto | null>(null);
  const [showFlightsModal, setShowFlightsModal] = useState(false);

  const [comparing, setComparing] = useState(false);
  const [comparisonResult, setComparisonResult] = useState<ComparisonResultDto | null>(null);
  const [showComparePanel, setShowComparePanel] = useState(false);

  const [showImportModal, setShowImportModal] = useState(false);
  const [importJson, setImportJson] = useState(EXAMPLE_SCENARIO);
  const [importError, setImportError] = useState<string | null>(null);
  const [solvingImport, setSolvingImport] = useState(false);

  const [showBenchmarkModal, setShowBenchmarkModal] = useState(false);
  const [benchmarkJson, setBenchmarkJson] = useState("");
  const [benchmarkError, setBenchmarkError] = useState<string | null>(null);
  const [benchmarkRunning, setBenchmarkRunning] = useState(false);
  const [benchmarkResult, setBenchmarkResult] = useState<GaBenchmarkResultDto | null>(null);

  const [showBenchmarksModal, setShowBenchmarksModal] = useState(false);
  const [benchmarksLoading, setBenchmarksLoading] = useState(false);
  const [benchmarksData, setBenchmarksData] = useState<BenchmarkEntryDto[] | null>(null);

  useEffect(() => {
    if (hasScenario) {
      loadScenarioConfig();
      loadEvents();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadScenarioConfig() {
    try {
      const res = await apiFetch(`/api/scenarios/configs/${scenarioId}`);
      const data = (await res.json()) as ScenarioConfigDto;
      setScenarioConfig(data);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load scenario", "error");
    }
  }

  async function loadEvents() {
    try {
      setLoadingEvents(true);
      const res = await apiFetch(`/api/random-events/${scenarioId}`);
      if (!res.ok) throw new Error(`Failed to load events (${res.status})`);
      const data = (await res.json()) as RandomEventDto[];
      setEvents(data.map((event) => ({ ...event, impactPercent: clampImpactPercent(event.impactPercent) })));
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load events", "error");
    } finally {
      setLoadingEvents(false);
    }
  }

  function openCreateEvent() {
    setEditingEvent(null);
    setEvName(""); setEvDescription(""); setEvStartTime(""); setEvEndTime(""); setEvImpact(100);
    setShowEventModal(true);
  }

  function openEditEvent(ev: RandomEventDto) {
    setEditingEvent(ev);
    setEvName(ev.name);
    setEvDescription(ev.description);
    setEvStartTime(toLocalDatetime(ev.startTime));
    setEvEndTime(toLocalDatetime(ev.endTime));
    setEvImpact(clampImpactPercent(ev.impactPercent));
    setShowEventModal(true);
  }

  async function saveEvent() {
    try {
      setSavingEvent(true);

      if (scenarioConfig) {
        const scenStart = new Date(scenarioConfig.startTime).getTime();
        const scenEnd = new Date(scenarioConfig.endTime).getTime();
        const evStart = new Date(evStartTime).getTime();
        const evEnd = new Date(evEndTime).getTime();

        if (evStart < scenStart || evEnd > scenEnd) {
          throw new Error(
            `Event must be within the scenario window: ${formatDate(scenarioConfig.startTime)} → ${formatDate(scenarioConfig.endTime)}`
          );
        }
        if (evEnd <= evStart) {
          throw new Error("End time must be after start time.");
        }
      }

      const body = {
        scenarioConfigId: scenarioId,
        name: evName.trim(),
        description: evDescription.trim(),
        startTime: toUtcString(evStartTime),
        endTime: toUtcString(evEndTime),
        impactPercent: clampImpactPercent(evImpact),
      };
      if (editingEvent) {
        const res = await apiFetch(`/api/random-events/${editingEvent.id}`, { method: "PUT", body: JSON.stringify(body) });
        if (!res.ok) throw new Error(`Failed to update event (${res.status})`);
        const updated = (await res.json()) as RandomEventDto;
        setEvents((prev) => prev.map((e) => (e.id === updated.id ? { ...updated, impactPercent: clampImpactPercent(updated.impactPercent) } : e)));
        showToast("Event updated", "success");
      } else {
        const res = await apiFetch(`/api/scenarios/${scenarioId}/random-events`, { method: "POST", body: JSON.stringify(body) });
        if (!res.ok) throw new Error(`Failed to create event (${res.status})`);
        const created = (await res.json()) as RandomEventDto;
        setEvents((prev) => [...prev, { ...created, impactPercent: clampImpactPercent(created.impactPercent) }]);
        showToast("Event created", "success");
      }
      setShowEventModal(false);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to save event", "error");
    } finally {
      setSavingEvent(false);
    }
  }

  async function deleteEvent(id: string) {
    try {
      const res = await apiFetch(`/api/random-events/${id}`, { method: "DELETE" });
      if (!res.ok && res.status !== 204) throw new Error(`Failed to delete event (${res.status})`);
      setEvents((prev) => prev.filter((e) => e.id !== id));
      showToast("Event deleted", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to delete event", "error");
    }
  }

  async function runCompare() {
    try {
      if (!hasScenario) throw new Error("No scenario selected");
      setComparing(true);
      setComparisonResult(null);
      const res = await apiFetch(`/api/compare/${scenarioId}`);
      if (!res.ok) throw new Error(`Compare failed (${res.status})`);
      const data = (await res.json()) as ComparisonResultDto;
      setComparisonResult(data);
      setShowComparePanel(true);
      showToast("Comparison complete", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Comparison failed", "error");
    } finally {
      setComparing(false);
    }
  }

  async function runSolver(algorithm: "greedy" | "genetic") {
    try {
      if (!hasScenario) throw new Error("No scenario selected");
      setSolving(true);
      setSolverResult(null);
      setShowFlightsModal(false);
      const res = await apiFetch(`/api/${algorithm}/${scenarioId}`);
      if (!res.ok) throw new Error(`Solver failed (${res.status})`);
      const data = (await res.json()) as SolverResultDto;
      setSolverResult(data);
      showToast(`Solved with ${data.algorithmName}`, "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Solver failed", "error");
    } finally {
      setSolving(false);
    }
  }

  async function runSolverFromPayload(algorithm: "greedy" | "genetic") {
    setImportError(null);
    let parsed: unknown;
    try {
      parsed = JSON.parse(importJson);
    } catch (e) {
      setImportError(e instanceof Error ? e.message : "Invalid JSON");
      return;
    }
    try {
      setSolvingImport(true);
      setSolverResult(null);
      setShowFlightsModal(false);
      const payload = { ...(parsed as object), algorithm };
      const res = await apiFetch("/api/solver/solve-from-payload", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Solver failed (${res.status}): ${text}`);
      }
      const data = (await res.json()) as SolverResultDto;
      setSolverResult(data);
      setShowImportModal(false);
      showToast(`Solved with ${data.algorithmName}`, "success");
    } catch (e) {
      setImportError(e instanceof Error ? e.message : "Solver failed");
    } finally {
      setSolvingImport(false);
    }
  }

  function openBenchmarkModal() {
    setBenchmarkResult(null);
    setBenchmarkError(null);
    setBenchmarkJson(getDefaultBenchmarkJson(scenarioId));
    setShowBenchmarkModal(true);
  }

  async function runBenchmark() {
    setBenchmarkError(null);
    let parsed: unknown;
    try {
      parsed = JSON.parse(benchmarkJson);
    } catch (e) {
      setBenchmarkError(e instanceof Error ? e.message : "Invalid JSON");
      return;
    }
    try {
      setBenchmarkRunning(true);
      const res = await apiFetch("/api/solver/benchmark", {
        method: "POST",
        body: JSON.stringify(parsed),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Benchmark failed (${res.status}): ${text}`);
      }
      const data = (await res.json()) as GaBenchmarkResultDto;
      setBenchmarkResult(data);
      showToast("Benchmark complete", "success");
    } catch (e) {
      setBenchmarkError(e instanceof Error ? e.message : "Benchmark failed");
    } finally {
      setBenchmarkRunning(false);
    }
  }

  async function loadBenchmarks() {
    try {
      setBenchmarksLoading(true);
      setBenchmarksData(null);
      const res = await apiFetch("/api/solver/benchmarks");
      if (!res.ok) throw new Error(`Failed to load benchmarks (${res.status})`);
      const data = (await res.json()) as BenchmarkEntryDto[];
      setBenchmarksData(data);
      setShowBenchmarksModal(true);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load benchmarks", "error");
    } finally {
      setBenchmarksLoading(false);
    }
  }

  const cancellationBreakdown: { label: string; count: number }[] = solverResult
    ? [
        { label: "No Compatible Runway", count: solverResult.canceledNoCompatibleRunway },
        { label: "Outside Scenario Window", count: solverResult.canceledOutsideWindow },
        { label: "Exceeds Max Delay", count: solverResult.canceledExceedsMaxDelay },
      ].filter((x) => x.count > 0)
    : [];

  const canSave = !savingEvent && evName.trim().length > 0 && !!evStartTime && !!evEndTime;

  return (
    <div style={{ maxWidth: "1500px", margin: "0 auto" }}>

      <div style={{ marginBottom: "24px" }}>
        <div style={S.label}>SOLVER</div>
        <h1 style={{ margin: "6px 0 0", fontSize: "22px", fontWeight: 800 }}>Scenario Solver</h1>
      </div>

      <div className="glass-card--selected" style={{ marginBottom: "24px" }}>
        <div style={{ display: "flex", gap: "32px", alignItems: "center", flexWrap: "wrap" }}>
          <div>
            <div style={S.label}>Airport</div>
            <div style={{ fontSize: "16px", fontWeight: 700, marginTop: "4px", color: airportName ? C.text : C.textMuted }}>
              {airportName || "None"}
            </div>
          </div>
          <div>
            <div style={S.label}>Scenario</div>
            <div style={{ fontSize: "16px", fontWeight: 700, marginTop: "4px", color: scenarioName ? C.text : C.textMuted }}>
              {scenarioName || "None selected"}
            </div>
          </div>
          {scenarioConfig && (
            <div>
              <div style={S.label}>Timeline</div>
              <div style={{ fontSize: "13px", color: C.textSub, marginTop: "4px" }}>
                {formatDate(scenarioConfig.startTime)} <span style={{ color: C.textMuted }}>→</span> {formatDate(scenarioConfig.endTime)}
              </div>
            </div>
          )}
          {!hasScenario && (
            <div style={{ color: C.textSub, fontSize: "13px" }}>
              Go to <a href="/scenario-config" style={{ color: C.primary }}>Scenarios</a> and select one first.
            </div>
          )}
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "24px", alignItems: "start" }}>

        <div>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "12px" }}>
            <div style={S.sectionTitle}>Random Events</div>
            <button
              onClick={openCreateEvent}
              className="glass-btn-primary"
              disabled={!hasScenario}
              style={{ opacity: hasScenario ? 1 : 0.5 }}
            >
              + Add event
            </button>
          </div>

          {!hasScenario ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>No scenario selected.</div>
          ) : loadingEvents ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>Loading eventsâ€¦</div>
          ) : events.length === 0 ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>
              No random events yet. Add one to affect the simulation.
            </div>
          ) : (
            <div style={{ display: "grid", gap: "10px" }}>
              {events.map((ev, i) => (
                <div
                  key={ev.id}
                  className="glass-card"
                  style={{ animation: `fadeInUp 0.3s ease ${i * 50}ms both` }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between", gap: "8px" }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontSize: "14px", fontWeight: 700 }}>{ev.name}</div>
                      {ev.description && (
                        <div style={{ color: C.textSub, fontSize: "12px", marginTop: "3px" }}>{ev.description}</div>
                      )}
                      <div style={{ display: "flex", gap: "16px", marginTop: "8px", flexWrap: "wrap" }}>
                        <div>
                          <div style={S.label}>Start</div>
                          <div style={{ fontSize: "12px", marginTop: "2px" }}>{formatDate(ev.startTime)}</div>
                        </div>
                        <div>
                          <div style={S.label}>End</div>
                          <div style={{ fontSize: "12px", marginTop: "2px" }}>{formatDate(ev.endTime)}</div>
                        </div>
                        <div>
                          <div style={S.label}>Impact</div>
                          <div style={{ fontSize: "15px", color: C.primary, fontWeight: 800, marginTop: "2px" }}>
                            {clampImpactPercent(ev.impactPercent)}%
                          </div>
                        </div>
                      </div>
                    </div>
                    <div style={{ display: "flex", gap: "6px", flexShrink: 0 }}>
                      <button onClick={() => openEditEvent(ev)} className="glass-btn-ghost">Edit</button>
                      <button onClick={() => setConfirmDelete({ id: ev.id, name: ev.name })} className="glass-btn-danger">Delete</button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div>
          <div style={{ ...S.sectionTitle, marginBottom: "12px" }}>Run Solver</div>

          <div className="glass-card" style={{ marginBottom: "16px" }}>
            <div style={{ ...S.label, marginBottom: "12px" }}>Select Algorithm</div>
            <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
              <button
                onClick={() => runSolver("greedy")}
                className="glass-btn-primary"
                disabled={!hasScenario || solving || comparing}
                style={{ opacity: !hasScenario || solving || comparing ? 0.5 : 1, minWidth: "130px" }}
              >
                {solving ? "Solving..." : "Run Greedy"}
              </button>
              <button
                onClick={() => runSolver("genetic")}
                className="glass-btn-primary"
                disabled={!hasScenario || solving || comparing}
                style={{ opacity: !hasScenario || solving || comparing ? 0.5 : 1, minWidth: "130px" }}
              >
                {solving ? "Solving..." : "Run Genetic"}
              </button>
              <button
                onClick={() => { setShowImportModal(true); setImportError(null); }}
                className="glass-btn-ghost"
                style={{ minWidth: "130px" }}
              >
                Import JSON
              </button>
            </div>
          </div>

          <div className="glass-card" style={{ marginBottom: "16px", border: `1px solid ${C.border}` }}>
            <div style={{ ...S.label, marginBottom: "8px" }}>GA Benchmark</div>
            <div style={{ fontSize: "12px", color: C.textSub, marginBottom: "12px" }}>
              Run multiple GA configurations on one or more scenarios and compare results.
            </div>
            <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
              <button onClick={openBenchmarkModal} className="glass-btn-primary" style={{ minWidth: "160px" }}>
                Open Benchmark
              </button>
              <button
                onClick={loadBenchmarks}
                className="glass-btn-ghost"
                disabled={benchmarksLoading}
                style={{ opacity: benchmarksLoading ? 0.5 : 1, minWidth: "160px" }}
              >
                {benchmarksLoading ? "Loading…" : "Show Benchmarks"}
              </button>
            </div>
          </div>

          <div className="glass-card" style={{ marginBottom: "16px", border: `1px solid ${C.border}` }}>
            <div style={{ ...S.label, marginBottom: "8px" }}>Compare Algorithms</div>
            <div style={{ fontSize: "12px", color: C.textSub, marginBottom: "12px" }}>
              Runs both Greedy and Genetic on the same scenario and shows a side-by-side comparison.
            </div>
            <div style={{ display: "flex", gap: "10px", alignItems: "center", flexWrap: "wrap" }}>
              <button
                onClick={runCompare}
                className="glass-btn-primary"
                disabled={!hasScenario || comparing || solving}
                style={{ opacity: !hasScenario || comparing || solving ? 0.5 : 1, minWidth: "160px" }}
              >
                {comparing ? "Comparing..." : "Compare Greedy vs Genetic"}
              </button>
              {comparisonResult && (
                <button
                  onClick={() => setShowComparePanel((v) => !v)}
                  className="glass-btn-ghost"
                >
                  {showComparePanel ? "Hide comparison" : "Show comparison"}
                </button>
              )}
            </div>
          </div>

          {solverResult && (
            <div style={{ animation: "fadeInUp 0.3s ease both" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "12px" }}>
                <div style={{ fontSize: "15px", fontWeight: 700 }}>
                  Results – <span style={{ color: C.primary }}>{solverResult.algorithmName}</span>
                </div>
                <button onClick={() => setShowFlightsModal(true)} className="glass-btn-ghost">
                  View all flights
                </button>
              </div>

              <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: "8px", marginBottom: "12px" }}>
                {([
                  { label: "Total Flights", value: solverResult.totalFlights, color: C.text },
                  { label: "Scheduled", value: solverResult.totalScheduledFlights, color: C.activeGreen },
                  { label: "On Time", value: solverResult.totalOnTimeFlights, color: C.activeGreen },
                  { label: "Early", value: solverResult.totalEarlyFlights, color: "#38bdf8" },
                  { label: "Delayed", value: solverResult.totalDelayedFlights, color: C.primary },
                  { label: "Cancelled", value: solverResult.totalCanceledFlights, color: C.danger },
                  { label: "Rescheduled", value: solverResult.totalRescheduledFlights, color: "#34d399" },
                  { label: "Avg Delay", value: `${solverResult.averageDelayMinutes.toFixed(1)} min`, color: C.primary },
                  { label: "Max Delay", value: `${solverResult.maxDelayMinutes} min`, color: C.primary },
                  { label: "Total Delay", value: `${solverResult.totalDelayMinutes} min`, color: C.primary },
                  { label: "Total Early", value: `${solverResult.totalEarlyMinutes} min`, color: "#38bdf8" },
                  { label: "Throughput", value: `${solverResult.throughputFlightsPerHour.toFixed(1)}/h`, color: C.text },
                  { label: "Fitness", value: solverResult.fitness.toFixed(1), color: "#a78bfa" },
                  { label: "Solve Time", value: `${solverResult.solveTimeMs.toFixed(1)} ms`, color: C.textSub },
                ] as const).map((stat) => (
                  <div key={stat.label} className="glass-card" style={{ padding: "10px 12px", textAlign: "center" }}>
                    <div style={S.label}>{stat.label}</div>
                    <div style={{ fontSize: "17px", fontWeight: 800, color: stat.color, marginTop: "4px" }}>
                      {stat.value}
                    </div>
                  </div>
                ))}
              </div>

              {solverResult.totalCanceledFlights > 0 && (
                <div className="glass-card" style={{ border: `1px solid ${C.borderAccentRed}` }}>
                  <div style={{ ...S.label, marginBottom: "8px", color: C.danger }}>Cancellation Breakdown</div>
                  {cancellationBreakdown.map(({ label, count }) => (
                    <div
                      key={label}
                      style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", padding: "4px 0", borderBottom: `1px solid ${C.border}` }}
                    >
                      <span style={{ color: C.textSub }}>{label}</span>
                      <span style={{ color: C.danger, fontWeight: 700 }}>{count}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {showComparePanel && comparisonResult && (
        <div style={{ marginTop: "32px", animation: "fadeInUp 0.3s ease both" }}>
          <div style={{ ...S.sectionTitle, marginBottom: "16px" }}>
            Comparison – <span style={{ color: C.primary }}>Greedy</span> vs <span style={{ color: "#a78bfa" }}>Genetic</span>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "16px" }}>
            {([comparisonResult.greedy, comparisonResult.genetic] as SolverResultDto[]).map((r) => {
              const isGenetic = r.algorithmName === "Genetic Algorithm";
              const accentColor = isGenetic ? "#a78bfa" : C.primary;
              return (
                <div key={r.algorithmName} className="glass-card" style={{ border: `1px solid ${accentColor}33` }}>
                  <div style={{ fontSize: "15px", fontWeight: 800, color: accentColor, marginBottom: "14px" }}>
                    {r.algorithmName}
                  </div>
                  <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: "8px" }}>
                    {([
                      { label: "Total", value: r.totalFlights, color: C.text },
                      { label: "Assigned", value: r.totalScheduledFlights + r.totalRescheduledFlights, color: C.activeGreen },
                      { label: "Scheduled", value: r.totalScheduledFlights, color: C.activeGreen },
                      { label: "Rescheduled", value: r.totalRescheduledFlights, color: "#34d399" },
                      { label: "On Time", value: r.totalOnTimeFlights, color: C.activeGreen },
                      { label: "Early", value: r.totalEarlyFlights, color: "#38bdf8" },
                      { label: "Delayed", value: r.totalDelayedFlights, color: accentColor },
                      { label: "Cancelled", value: r.totalCanceledFlights, color: C.danger },
                      { label: "Avg Delay", value: `${r.averageDelayMinutes.toFixed(1)} min`, color: accentColor },
                      { label: "Max Delay", value: `${r.maxDelayMinutes} min`, color: accentColor },
                      { label: "Total Delay", value: `${r.totalDelayMinutes} min`, color: accentColor },
                      { label: "Total Early", value: `${r.totalEarlyMinutes} min`, color: "#38bdf8" },
                      { label: "Fitness", value: r.fitness.toFixed(1), color: "#a78bfa" },
                      { label: "Throughput", value: `${r.throughputFlightsPerHour.toFixed(1)}/h`, color: C.text },
                      { label: "Solve Time", value: `${r.solveTimeMs.toFixed(1)} ms`, color: C.textSub },
                    ] as const).map((stat) => (
                      <div key={stat.label} className="glass-card" style={{ padding: "8px 10px", textAlign: "center" }}>
                        <div style={S.label}>{stat.label}</div>
                        <div style={{ fontSize: "15px", fontWeight: 800, color: stat.color, marginTop: "3px" }}>
                          {stat.value}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>

          <div className="glass-card" style={{ marginTop: "16px", border: `1px solid ${C.border}` }}>
            <div style={{ ...S.label, marginBottom: "4px" }}>Pure Algorithm Comparison (excl. Rescheduled)</div>
            <div style={{ fontSize: "11px", color: C.textMuted, marginBottom: "12px" }}>
              Treats rescheduled flights as cancelled – compares raw algorithm output without post-processing.
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: "8px", marginBottom: "12px" }}>
              {([comparisonResult.greedy, comparisonResult.genetic] as SolverResultDto[]).map((r) => {
                const isGenetic = r.algorithmName === "Genetic Algorithm";
                const accentColor = isGenetic ? "#a78bfa" : C.primary;
                const pureScheduled = r.flights.filter(f => f.status !== 3 && f.status !== 5).length;
                const pureCancelled = r.flights.filter(f => f.status === 3 || f.status === 5).length;
                const pureDelayed   = r.flights.filter(f => f.status === 2).length;
                const pureEarly     = r.flights.filter(f => f.status === 4).length;
                const pureOnTime    = r.flights.filter(f => f.status === 1).length;
                return (
                  <div key={r.algorithmName} className="glass-card" style={{ border: `1px solid ${accentColor}33` }}>
                    <div style={{ fontSize: "13px", fontWeight: 800, color: accentColor, marginBottom: "10px" }}>{r.algorithmName}</div>
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "6px" }}>
                      {([
                        { label: "Assigned",   value: pureScheduled, color: C.activeGreen },
                        { label: "Cancelled",  value: pureCancelled, color: C.danger },
                        { label: "On Time",    value: pureOnTime,    color: C.activeGreen },
                        { label: "Early",      value: pureEarly,     color: "#38bdf8" },
                        { label: "Delayed",    value: pureDelayed,   color: accentColor },
                      ]).map(stat => (
                        <div key={stat.label} className="glass-card" style={{ padding: "7px 9px", textAlign: "center" }}>
                          <div style={S.label}>{stat.label}</div>
                          <div style={{ fontSize: "14px", fontWeight: 800, color: stat.color, marginTop: "3px" }}>{stat.value}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
              <div className="glass-card" style={{ border: `1px solid ${C.border}` }}>
                <div style={{ fontSize: "13px", fontWeight: 800, color: C.textSub, marginBottom: "10px" }}>Delta (G − Gr)</div>
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "6px" }}>
                  {(() => {
                    const g  = comparisonResult.genetic;
                    const gr = comparisonResult.greedy;
                    const gf  = g.flights as SolvedFlightDto[];
                    const grf = gr.flights as SolvedFlightDto[];
                    const pureScheduled = (f: SolvedFlightDto[]) => f.filter(x => x.status !== 3 && x.status !== 5).length;
                    const pureCancelled = (f: SolvedFlightDto[]) => f.filter(x => x.status === 3 || x.status === 5).length;
                    const pureDelayed   = (f: SolvedFlightDto[]) => f.filter(x => x.status === 2).length;
                    const pureEarly     = (f: SolvedFlightDto[]) => f.filter(x => x.status === 4).length;
                    return ([
                      { label: "Assigned",   delta: pureScheduled(gf) - pureScheduled(grf), higherIsBetter: true  },
                      { label: "Cancelled",  delta: pureCancelled(gf) - pureCancelled(grf), higherIsBetter: false },
                      { label: "Delayed",    delta: pureDelayed(gf)   - pureDelayed(grf),   higherIsBetter: false },
                      { label: "Early",      delta: pureEarly(gf)     - pureEarly(grf),     higherIsBetter: true  },
                      { label: "Avg Delay",   delta: g.averageDelayMinutes - gr.averageDelayMinutes, higherIsBetter: false, unit: " min", decimals: 1 },
                      { label: "Total Delay", delta: g.totalDelayMinutes - gr.totalDelayMinutes, higherIsBetter: false, unit: " min" },
                      { label: "Total Early", delta: g.totalEarlyMinutes - gr.totalEarlyMinutes, higherIsBetter: false, unit: " min" },
                      { label: "Throughput",  delta: g.throughputFlightsPerHour - gr.throughputFlightsPerHour, higherIsBetter: true, unit: "/h", decimals: 2 },
                    ] as { label: string; delta: number; higherIsBetter: boolean; unit?: string; decimals?: number }[]).map(({ label, delta, higherIsBetter, unit = "", decimals = 0 }) => {
                      const better = higherIsBetter ? delta > 0 : delta < 0;
                      const worse  = higherIsBetter ? delta < 0 : delta > 0;
                      const color  = better ? C.activeGreen : worse ? C.danger : C.textSub;
                      const prefix = delta > 0 ? "+" : "";
                      const formatted = decimals > 0 ? delta.toFixed(decimals) : String(delta);
                      return (
                        <div key={label} className="glass-card" style={{ padding: "7px 9px", textAlign: "center" }}>
                          <div style={S.label}>{label}</div>
                          <div style={{ fontSize: "14px", fontWeight: 800, color, marginTop: "3px" }}>
                            {prefix}{formatted}{unit}
                          </div>
                        </div>
                      );
                    });
                  })()}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {showFlightsModal && solverResult && (
        <LargeModal
          title={`Solved Flights – ${solverResult.algorithmName}`}
          onClose={() => setShowFlightsModal(false)}
        >
          <div style={{ border: `1px solid ${C.border}`, borderRadius: "6px", overflow: "auto", maxHeight: "68vh", background: C.bgCard }}>
            <table style={{ width: "100%", borderCollapse: "collapse", minWidth: "1200px" }}>
              <thead>
                <tr>
                  {["#", "Callsign", "Type", "Pri", "Status", "Runway", "Scheduled", "Assigned", "Delay", "Cancel Reason", "Weather", "Event"].map((h) => (
                    <th key={h} style={thStyle}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {solverResult.flights.map((f) => (
                  <tr key={f.flightId}>
                    <td style={{ ...tdStyle, color: C.textSub }}>{f.processingOrder}</td>
                    <td style={{ ...tdStyle, fontWeight: 700 }}>{f.callsign}</td>
                    <td style={tdStyle}>{flightTypeLabel(f.type)}</td>
                    <td style={{ ...tdStyle, color: C.textSub }}>{f.priority}</td>
                    <td style={{ ...tdStyle, color: statusColor(f.status), fontWeight: 700 }}>
                      {statusLabel(f.status)}
                    </td>
                    <td style={tdStyle}>{f.assignedRunway ?? "–"}</td>
                    <td style={tdStyle}>{formatDate(f.scheduledTime)}</td>
                    <td style={tdStyle}>{f.assignedTime ? formatDate(f.assignedTime) : "–"}</td>
                    <td style={{ ...tdStyle, color: f.delayMinutes > 0 ? C.primary : f.earlyMinutes > 0 ? C.activeGreen : C.textSub }}>
                      {f.delayMinutes > 0
                        ? `+${f.delayMinutes}m`
                        : f.earlyMinutes > 0
                        ? `-${f.earlyMinutes}m`
                        : "0"}
                    </td>
                    <td style={{ ...tdStyle, color: f.cancellationReason !== 0 ? C.danger : C.textSub }}>
                      {cancellationLabel(f.cancellationReason)}
                    </td>
                    <td style={tdStyle}>{weatherLabel(f.weatherAtAssignment)}</td>
                    <td style={{ ...tdStyle, color: f.affectedByRandomEvent ? C.primary : C.textSub }}>
                      {f.affectedByRandomEvent ? "Yes" : "–"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </LargeModal>
      )}

      {showEventModal && (
        <Modal
          onClose={() => setShowEventModal(false)}
          title={editingEvent ? "Edit Random Event" : "Add Random Event"}
        >
          <div style={{ display: "grid", gap: "10px" }}>
            <Field label="Name">
              <input
                value={evName}
                onChange={(e) => setEvName(e.target.value)}
                className="glass-input"
                placeholder="e.g. VIP Arrival"
              />
            </Field>
            <Field label="Description">
              <input
                value={evDescription}
                onChange={(e) => setEvDescription(e.target.value)}
                className="glass-input"
                placeholder="Optional"
              />
            </Field>
            <Field label="Start time">
              <input
                type="datetime-local"
                value={evStartTime}
                onChange={(e) => setEvStartTime(e.target.value)}
                className="glass-input"
                min={scenarioConfig ? toLocalDatetime(scenarioConfig.startTime) : undefined}
                max={scenarioConfig ? toLocalDatetime(scenarioConfig.endTime) : undefined}
              />
            </Field>
            <Field label="End time">
              <input
                type="datetime-local"
                value={evEndTime}
                onChange={(e) => setEvEndTime(e.target.value)}
                className="glass-input"
                min={scenarioConfig ? toLocalDatetime(scenarioConfig.startTime) : undefined}
                max={scenarioConfig ? toLocalDatetime(scenarioConfig.endTime) : undefined}
              />
            </Field>
            {scenarioConfig && (
              <div style={{ fontSize: "11px", color: C.textSub, marginTop: "-4px" }}>
                Scenario window: {formatDate(scenarioConfig.startTime)} → {formatDate(scenarioConfig.endTime)}
              </div>
            )}
            <Field label="Impact %">
              <NumberInput
                value={clampImpactPercent(evImpact)}
                onChange={(value) => setEvImpact(clampImpactPercent(value))}
                className="glass-input"
                min={0}
                max={100}
              />
            </Field>
            <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "4px" }}>
              <button onClick={() => setShowEventModal(false)} className="glass-btn-ghost">Cancel</button>
              <button
                onClick={saveEvent}
                className="glass-btn-primary"
                disabled={!canSave}
                style={{ opacity: canSave ? 1 : 0.5 }}
              >
                {savingEvent ? "Savingâ€¦" : editingEvent ? "Update" : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {showImportModal && (
        <div className="glass-modal-backdrop" style={{ padding: "20px" }} onClick={() => setShowImportModal(false)}>
          <div
            onClick={(e) => e.stopPropagation()}
            className="glass-modal-panel"
            style={{ width: "860px", maxWidth: "95vw", maxHeight: "88vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ fontWeight: 800, fontSize: "16px" }}>Import Scenario JSON</div>
              <button onClick={() => setShowImportModal(false)} className="glass-btn-ghost">Close</button>
            </div>

            <div style={{ fontSize: "12px", color: C.textSub }}>
              Paste a scenario payload below or use the examples. Edit directly in the textarea.
              <br />
              <span style={{ color: C.textMuted }}>runwayType: 0=Landing, 1=Takeoff, 2=Both &nbsp;|&nbsp; flight type: 0=Arrival, 1=Departure, 2=OnGround &nbsp;|&nbsp; weather: 0=Clear...5=Storm</span>
            </div>
            <div style={{ display: "flex", gap: "8px" }}>
              <button onClick={() => setImportJson(EXAMPLE_SCENARIO)} className="glass-btn-ghost" style={{ fontSize: "12px" }}>
                Load minimal example (5 flights)
              </button>
              <button onClick={() => setImportJson(SCENARIO_500)} className="glass-btn-ghost" style={{ fontSize: "12px" }}>
                Load scenario-500 (500 flights)
              </button>
            </div>

            <textarea
              value={importJson}
              onChange={(e) => { setImportJson(e.target.value); setImportError(null); }}
              className="glass-input"
              spellCheck={false}
              style={{
                flex: 1,
                minHeight: "380px",
                fontFamily: "monospace",
                fontSize: "12px",
                resize: "vertical",
                lineHeight: 1.5,
                whiteSpace: "pre",
                overflowWrap: "normal",
                overflowX: "auto",
              }}
            />

            {importError && (
              <div style={{ color: C.danger, fontSize: "12px", background: `${C.danger}11`, border: `1px solid ${C.borderAccentRed}`, borderRadius: "6px", padding: "8px 12px" }}>
                {importError}
              </div>
            )}

            <div style={{ display: "flex", gap: "10px", justifyContent: "flex-end" }}>
              <button onClick={() => setShowImportModal(false)} className="glass-btn-ghost">Cancel</button>
              <button
                onClick={() => runSolverFromPayload("greedy")}
                className="glass-btn-primary"
                disabled={solvingImport}
                style={{ opacity: solvingImport ? 0.5 : 1, minWidth: "130px" }}
              >
                {solvingImport ? "Solving..." : "Solve Greedy"}
              </button>
              <button
                onClick={() => runSolverFromPayload("genetic")}
                className="glass-btn-primary"
                disabled={solvingImport}
                style={{ opacity: solvingImport ? 0.5 : 1, minWidth: "130px" }}
              >
                {solvingImport ? "Solving..." : "Solve Genetic"}
              </button>
            </div>
          </div>
        </div>
      )}

      {showBenchmarksModal && benchmarksData && (
        <div className="glass-modal-backdrop" style={{ padding: "20px" }} onClick={() => setShowBenchmarksModal(false)}>
          <div
            onClick={(e) => e.stopPropagation()}
            className="glass-modal-panel"
            style={{ width: "1300px", maxWidth: "95vw", maxHeight: "90vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ fontWeight: 800, fontSize: "16px" }}>
                Benchmark History — {benchmarksData.length} entries
              </div>
              <button onClick={() => setShowBenchmarksModal(false)} className="glass-btn-ghost">Close</button>
            </div>

            {benchmarksData.length === 0 ? (
              <div style={{ color: C.textSub, fontSize: "13px" }}>No benchmark entries yet.</div>
            ) : (
              <div style={{ minHeight: 0, flex: 1, overflowY: "auto", display: "flex", flexDirection: "column", gap: "20px" }}>
                {Object.entries(
                  benchmarksData.reduce<Record<string, BenchmarkEntryDto[]>>((acc, e) => {
                    (acc[e.scenarioConfigId] ??= []).push(e);
                    return acc;
                  }, {})
                ).map(([sid, entries]) => {
                  const sorted = [...entries].sort((a, b) => a.fitness - b.fitness);
                  return (
                    <div key={sid}>
                      <div style={{ ...S.label, marginBottom: "8px" }}>
                        Scenario: <span style={{ color: C.text, fontFamily: "monospace" }}>{sid}</span>
                        <span style={{ color: C.textMuted, marginLeft: "10px" }}>({sorted.length} runs)</span>
                      </div>
                      <div style={{ border: `1px solid ${C.border}`, borderRadius: "6px", overflow: "auto", background: C.bgCard }}>
                        <table style={{ width: "100%", borderCollapse: "collapse", minWidth: "900px" }}>
                          <thead>
                            <tr>
                              {["Rank", "#", "Fitness", "Time ms", "Timestamp", "Pop", "Max Gen", "Crossover", "Mut Local", "Tournament", "Elite", "No Imp", "Seed"].map((h) => (
                                <th key={h} style={thStyle}>{h}</th>
                              ))}
                            </tr>
                          </thead>
                          <tbody>
                            {sorted.map((e, rank) => (
                              <tr key={e.id}>
                                <td style={{ ...tdStyle, fontWeight: 700, color: rank === 0 ? "#fbbf24" : C.textSub }}>{rank + 1}</td>
                                <td style={{ ...tdStyle, color: C.textSub }}>{e.configIndex}</td>
                                <td style={{ ...tdStyle, fontWeight: 700, color: "#a78bfa" }}>{e.fitness.toFixed(2)}</td>
                                <td style={{ ...tdStyle, color: C.textSub }}>{e.solveTimeMs.toFixed(0)}</td>
                                <td style={{ ...tdStyle, fontSize: "11px", color: C.textSub }}>{formatDate(e.runTimestampUtc)}</td>
                                <td style={tdStyle}>{e.populationSize ?? "–"}</td>
                                <td style={tdStyle}>{e.maxGenerations ?? "–"}</td>
                                <td style={tdStyle}>{e.crossoverRate != null ? e.crossoverRate.toFixed(2) : "–"}</td>
                                <td style={tdStyle}>{e.mutationRateLocal != null ? e.mutationRateLocal.toFixed(3) : "–"}</td>
                                <td style={tdStyle}>{e.tournamentSize ?? "–"}</td>
                                <td style={tdStyle}>{e.eliteCount ?? "–"}</td>
                                <td style={tdStyle}>{e.noImprovementGenerations ?? "–"}</td>
                                <td style={{ ...tdStyle, color: C.textSub }}>{e.randomSeed ?? "–"}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      )}

      {showBenchmarkModal && (
        <div className="glass-modal-backdrop" style={{ padding: "20px" }} onClick={() => setShowBenchmarkModal(false)}>
          <div
            onClick={(e) => e.stopPropagation()}
            className="glass-modal-panel"
            style={{ width: "1300px", maxWidth: "95vw", maxHeight: "90vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ fontWeight: 800, fontSize: "16px" }}>GA Benchmark</div>
              <button onClick={() => setShowBenchmarkModal(false)} className="glass-btn-ghost">Close</button>
            </div>

            <div style={{ fontSize: "12px", color: C.textSub }}>
              Payload JSON with <code>scenarioConfigIds</code> and <code>configs</code> arrays. Results are saved to the database.
            </div>

            <textarea
              value={benchmarkJson}
              onChange={(e) => { setBenchmarkJson(e.target.value); setBenchmarkError(null); }}
              className="glass-input"
              spellCheck={false}
              style={{
                minHeight: "200px",
                fontFamily: "monospace",
                fontSize: "12px",
                resize: "vertical",
                lineHeight: 1.5,
                whiteSpace: "pre",
                overflowWrap: "normal",
                overflowX: "auto",
              }}
            />

            {benchmarkError && (
              <div style={{ color: C.danger, fontSize: "12px", background: `${C.danger}11`, border: `1px solid ${C.borderAccentRed}`, borderRadius: "6px", padding: "8px 12px" }}>
                {benchmarkError}
              </div>
            )}

            <div style={{ display: "flex", gap: "10px", justifyContent: "flex-end" }}>
              <button onClick={() => setShowBenchmarkModal(false)} className="glass-btn-ghost">Cancel</button>
              <button
                onClick={runBenchmark}
                className="glass-btn-primary"
                disabled={benchmarkRunning}
                style={{ opacity: benchmarkRunning ? 0.5 : 1, minWidth: "140px" }}
              >
                {benchmarkRunning ? "Running…" : "Run Benchmark"}
              </button>
            </div>

            {benchmarkResult && benchmarkResult.entries.length > 0 && (
              <div style={{ minHeight: 0, flex: 1, overflow: "hidden", display: "flex", flexDirection: "column", gap: "8px" }}>
                <div style={{ ...S.label }}>Results — {benchmarkResult.entries.length} entries</div>
                <div style={{ flex: 1, overflow: "auto", border: `1px solid ${C.border}`, borderRadius: "6px", background: C.bgCard }}>
                  <table style={{ width: "100%", borderCollapse: "collapse", minWidth: "900px" }}>
                    <thead>
                      <tr>
                        {["#", "Scenario", "Fitness", "Time ms", "Pop", "Max Gen", "Crossover", "Mut Local", "Mut Mem", "Tournament", "Elite", "No Imp"].map((h) => (
                          <th key={h} style={thStyle}>{h}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {benchmarkResult.entries.map((entry, i) => (
                        <tr key={i}>
                          <td style={{ ...tdStyle, color: C.textSub }}>{entry.configIndex}</td>
                          <td style={{ ...tdStyle, fontFamily: "monospace", fontSize: "11px", color: C.textSub }}>{entry.scenarioConfigId.slice(0, 8)}…</td>
                          <td style={{ ...tdStyle, fontWeight: 700, color: "#a78bfa" }}>{entry.fitness.toFixed(2)}</td>
                          <td style={{ ...tdStyle, color: C.textSub }}>{entry.solveTimeMs.toFixed(0)}</td>
                          <td style={tdStyle}>{entry.config.populationSize}</td>
                          <td style={tdStyle}>{entry.config.maxGenerations}</td>
                          <td style={tdStyle}>{entry.config.crossoverRate.toFixed(2)}</td>
                          <td style={tdStyle}>{entry.config.mutationRateLocal.toFixed(3)}</td>
                          <td style={tdStyle}>{entry.config.mutationRateMemetic.toFixed(3)}</td>
                          <td style={tdStyle}>{entry.config.tournamentSize}</td>
                          <td style={tdStyle}>{entry.config.eliteCount}</td>
                          <td style={tdStyle}>{entry.config.noImprovementGenerations}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {confirmDelete && (
        <ConfirmDialog
          message={`Delete event "${confirmDelete.name}"?`}
          onConfirm={async () => {
            const id = confirmDelete.id;
            setConfirmDelete(null);
            await deleteEvent(id);
          }}
          onCancel={() => setConfirmDelete(null)}
        />
      )}
    </div>
  );
}
