import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { apiFetch } from "../lib/api";
import { C, S } from "../styles/tokens";
import { Modal } from "../components/Modal";
import { SkeletonCard } from "../components/Skeleton";
import { useToast } from "../components/Toast";

type AirportDto = {
  id: string;
  name: string;
  standCapacity: number;
  latitude: number;
  longitude: number;
};

type RunwayDto = {
  id: string;
  airportId: string;
  name: string;
  isActive: boolean;
  runwayType: number;
};

const STORAGE_AIRPORT_ID = "selectedAirportId";
const STORAGE_AIRPORT_NAME = "selectedAirportName";

function runwayTypeLabel(type: number) {
  switch (type) {
    case 0: return "Landing";
    case 1: return "Takeoff";
    case 2: return "Both";
    default: return `Type ${type}`;
  }
}

export default function AirportsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { showToast } = useToast();

  const [airports, setAirports] = useState<AirportDto[]>([]);
  const [runways, setRunways] = useState<RunwayDto[]>([]);
  const [loadingAirports, setLoadingAirports] = useState(false);
  const [loadingRunways, setLoadingRunways] = useState(false);

  const [selectedAirportId, setSelectedAirportId] = useState<string | null>(
    searchParams.get("airportId") || localStorage.getItem(STORAGE_AIRPORT_ID),
  );
  const [selectedAirportName, setSelectedAirportName] = useState<string>(
    localStorage.getItem(STORAGE_AIRPORT_NAME) || "",
  );

  const [showNewAirport, setShowNewAirport] = useState(false);
  const [creatingAirport, setCreatingAirport] = useState(false);
  const [newAirportName, setNewAirportName] = useState("");
  const [newAirportStandCapacity, setNewAirportStandCapacity] = useState(20);
  const [newAirportLatitude, setNewAirportLatitude] = useState(0);
  const [newAirportLongitude, setNewAirportLongitude] = useState(0);

  const [showNewRunway, setShowNewRunway] = useState(false);
  const [creatingRunway, setCreatingRunway] = useState(false);
  const [newRunwayName, setNewRunwayName] = useState("");
  const [newRunwayIsActive, setNewRunwayIsActive] = useState(true);
  const [newRunwayType, setNewRunwayType] = useState<number>(2);

  const [editingRunwayId, setEditingRunwayId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editIsActive, setEditIsActive] = useState(true);
  const [editRunwayType, setEditRunwayType] = useState<number>(2);

  const selectedAirport = useMemo(
    () => airports.find((a) => a.id === selectedAirportId) ?? null,
    [airports, selectedAirportId],
  );

  // Sync selectedAirportId to URL and localStorage
  useEffect(() => {
    if (!selectedAirportId) return;
    localStorage.setItem(STORAGE_AIRPORT_ID, selectedAirportId);
    const params = new URLSearchParams(searchParams);
    params.set("airportId", selectedAirportId);
    setSearchParams(params, { replace: true });
  }, [selectedAirportId, searchParams, setSearchParams]);

  // Sync selectedAirport name to state and localStorage
  useEffect(() => {
    const name = selectedAirport?.name;
    if (!name) return;
    setSelectedAirportName(name);
    localStorage.setItem(STORAGE_AIRPORT_NAME, name);
  }, [selectedAirport]);

  // Auto-load airports on mount
  useEffect(() => {
    fetchAirports();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Auto-load runways when airport selected
  useEffect(() => {
    if (selectedAirportId) {
      fetchRunways(selectedAirportId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedAirportId]);

  function clearSelectedAirport() {
    setSelectedAirportId(null);
    setSelectedAirportName("");
    setRunways([]);
    setEditingRunwayId(null);
    localStorage.removeItem(STORAGE_AIRPORT_ID);
    localStorage.removeItem(STORAGE_AIRPORT_NAME);
    const params = new URLSearchParams(searchParams);
    params.delete("airportId");
    setSearchParams(params, { replace: true });
  }

  async function fetchAirports() {
    try {
      setLoadingAirports(true);
      const res = await apiFetch("/api/airports");
      if (!res.ok) throw new Error(`Failed to load airports (${res.status})`);
      const data = (await res.json()) as AirportDto[];
      setAirports(data);
      if (selectedAirportId) {
        const matched = data.find((a) => a.id === selectedAirportId);
        if (matched) {
          setSelectedAirportName(matched.name);
          localStorage.setItem(STORAGE_AIRPORT_NAME, matched.name);
        }
      }
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load airports", "error");
    } finally {
      setLoadingAirports(false);
    }
  }

  async function fetchRunways(airportId: string) {
    try {
      setLoadingRunways(true);
      setEditingRunwayId(null);
      const res = await apiFetch(`/api/airports/${airportId}/runways`);
      if (!res.ok) throw new Error(`Failed to load runways (${res.status})`);
      const data = (await res.json()) as RunwayDto[];
      setRunways(data);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to load runways", "error");
    } finally {
      setLoadingRunways(false);
    }
  }

  function selectAirport(airport: AirportDto) {
    setSelectedAirportId(airport.id);
    setSelectedAirportName(airport.name);
    localStorage.setItem(STORAGE_AIRPORT_ID, airport.id);
    localStorage.setItem(STORAGE_AIRPORT_NAME, airport.name);
    setRunways([]);
    setEditingRunwayId(null);
  }

  async function createAirport() {
    try {
      setCreatingAirport(true);
      const res = await apiFetch("/api/airport", {
        method: "POST",
        body: JSON.stringify({
          name: newAirportName.trim(),
          standCapacity: newAirportStandCapacity,
          latitude: newAirportLatitude,
          longitude: newAirportLongitude,
        }),
      });
      if (!res.ok) throw new Error(`Failed to create airport (${res.status})`);
      const created = (await res.json()) as AirportDto;
      setAirports((prev) => [created, ...prev]);
      selectAirport(created);
      setShowNewAirport(false);
      setNewAirportName("");
      setNewAirportStandCapacity(20);
      setNewAirportLatitude(0);
      setNewAirportLongitude(0);
      showToast("Airport created", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to create airport", "error");
    } finally {
      setCreatingAirport(false);
    }
  }

  async function createRunway() {
    if (!selectedAirportId) return;
    try {
      setCreatingRunway(true);
      const res = await apiFetch(`/api/airports/${selectedAirportId}/runways`, {
        method: "POST",
        body: JSON.stringify({
          airportId: selectedAirportId,
          name: newRunwayName.trim(),
          isActive: newRunwayIsActive,
          runwayType: newRunwayType,
        }),
      });
      if (!res.ok) throw new Error(`Failed to create runway (${res.status})`);
      setShowNewRunway(false);
      setNewRunwayName("");
      setNewRunwayIsActive(true);
      setNewRunwayType(2);
      await fetchRunways(selectedAirportId);
      showToast("Runway created", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to create runway", "error");
    } finally {
      setCreatingRunway(false);
    }
  }

  async function deleteAirport(airportId: string) {
    try {
      const res = await apiFetch(`/api/airports/${airportId}`, { method: "DELETE" });
      if (!res.ok && res.status !== 204) throw new Error(`Failed to delete airport (${res.status})`);
      setAirports((prev) => prev.filter((a) => a.id !== airportId));
      if (selectedAirportId === airportId) clearSelectedAirport();
      showToast("Airport deleted", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to delete airport", "error");
    }
  }

  async function deleteRunway(runwayId: string) {
    try {
      const res = await apiFetch(`/api/runways/${runwayId}`, { method: "DELETE" });
      if (!res.ok && res.status !== 204) throw new Error(`Failed to delete runway (${res.status})`);
      setRunways((prev) => prev.filter((r) => r.id !== runwayId));
      if (editingRunwayId === runwayId) setEditingRunwayId(null);
      showToast("Runway deleted", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to delete runway", "error");
    }
  }

  function startEditRunway(r: RunwayDto) {
    setEditingRunwayId(r.id);
    setEditName(r.name);
    setEditIsActive(r.isActive);
    setEditRunwayType(r.runwayType);
  }

  async function saveRunway(runwayId: string) {
    if (!selectedAirportId) return;
    try {
      const res = await apiFetch(`/api/runways/${runwayId}`, {
        method: "PUT",
        body: JSON.stringify({
          airportId: selectedAirportId,
          name: editName.trim(),
          isActive: editIsActive,
          runwayType: editRunwayType,
        }),
      });
      if (!res.ok && res.status !== 204) throw new Error(`Failed to update runway (${res.status})`);
      setRunways((prev) =>
        prev.map((r) =>
          r.id === runwayId ? { ...r, name: editName.trim(), isActive: editIsActive, runwayType: editRunwayType } : r,
        ),
      );
      setEditingRunwayId(null);
      showToast("Runway updated", "success");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Failed to update runway", "error");
    }
  }

  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto" }}>
      {/* Page header */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "24px", flexWrap: "wrap", gap: "12px" }}>
        <div>
          <div style={S.label}>AIRPORT MANAGEMENT</div>
          <h1 style={{ margin: "6px 0 0", fontSize: "22px", fontWeight: 800 }}>Airports</h1>
        </div>
        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
          <button onClick={() => setShowNewAirport(true)} style={S.primaryBtn}>
            + Create airport
          </button>
          {selectedAirportId && (
            <button onClick={() => navigate(`/scenario-config?airportId=${selectedAirportId}`)} style={S.secondaryBtn}>
              Scenarios →
            </button>
          )}
        </div>
      </div>

      {/* Selected airport stat bar */}
      {(selectedAirport || selectedAirportName) && (
        <div style={{ ...S.cardAccent, marginBottom: "20px" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "16px" }}>
            <div>
              <div style={S.label}>Selected airport</div>
              <div style={{ fontSize: "18px", fontWeight: 800, marginTop: "4px" }}>
                {selectedAirport?.name || selectedAirportName}
              </div>
            </div>
            {selectedAirport && (
              <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: "8px" }}>
                {[
                  { label: "Stands", value: selectedAirport.standCapacity },
                  { label: "Runways", value: runways.length || "—" },
                  { label: "Lat/Lng", value: `${selectedAirport.latitude}, ${selectedAirport.longitude}` },
                ].map((stat) => (
                  <div key={stat.label} style={{ background: C.bg, border: `1px solid ${C.border}`, borderRadius: "6px", padding: "8px 12px", textAlign: "center" }}>
                    <div style={S.label}>{stat.label}</div>
                    <div style={{ color: C.primary, fontSize: "16px", fontWeight: 800, marginTop: "2px" }}>{stat.value}</div>
                  </div>
                ))}
              </div>
            )}
            <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
              <button onClick={() => setShowNewRunway(true)} style={S.secondaryBtn} disabled={!selectedAirportId}>
                + Runway
              </button>
              <button onClick={clearSelectedAirport} style={S.dangerBtn}>
                Clear
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Two-column grid */}
      <div style={{ display: "grid", gridTemplateColumns: "1.1fr 1fr", gap: "20px", alignItems: "start" }}>
        {/* Airports column */}
        <div>
          <div style={S.sectionTitle}>Airports</div>

          {loadingAirports ? (
            <div style={{ display: "grid", gap: "12px" }}>
              {[0, 1, 2].map((i) => <SkeletonCard key={i} />)}
            </div>
          ) : airports.length === 0 ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>No airports found.</div>
          ) : (
            <div style={{ display: "grid", gap: "10px" }}>
              {airports.map((airport) => {
                const isSelected = airport.id === selectedAirportId;
                return (
                  <div
                    key={airport.id}
                    style={{
                      ...S.card,
                      borderColor: isSelected ? C.borderAccentRed : C.border,
                    }}
                  >
                    <div style={{ display: "flex", justifyContent: "space-between", gap: "12px", flexWrap: "wrap" }}>
                      <div>
                        <div style={{ fontSize: "15px", fontWeight: 700 }}>{airport.name}</div>
                        <div style={{ marginTop: "6px", color: C.textSub, fontSize: "12px" }}>
                          <div>Stands: {airport.standCapacity}</div>
                          <div>Coords: {airport.latitude}, {airport.longitude}</div>
                        </div>
                      </div>
                      <div style={{ display: "flex", gap: "6px", flexWrap: "wrap", alignItems: "flex-start" }}>
                        <button onClick={() => selectAirport(airport)} style={isSelected ? S.primaryBtn : S.secondaryBtn}>
                          {isSelected ? "✓ Selected" : "Select"}
                        </button>
                        <button onClick={() => deleteAirport(airport.id)} style={S.dangerBtn}>
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

        {/* Runways column */}
        <div>
          <div style={S.sectionTitle}>Runways</div>

          {!selectedAirportId ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>Select an airport first.</div>
          ) : loadingRunways ? (
            <div style={{ display: "grid", gap: "12px" }}>
              {[0, 1].map((i) => <SkeletonCard key={i} />)}
            </div>
          ) : runways.length === 0 ? (
            <div style={{ ...S.card, color: C.textSub, fontSize: "13px" }}>No runways for this airport.</div>
          ) : (
            <div style={{ display: "grid", gap: "10px" }}>
              {runways.map((runway) => {
                const isEditing = editingRunwayId === runway.id;
                return (
                  <div key={runway.id} style={S.card}>
                    {!isEditing ? (
                      <div style={{ display: "flex", justifyContent: "space-between", gap: "12px", flexWrap: "wrap" }}>
                        <div>
                          <div style={{ fontSize: "15px", fontWeight: 700 }}>{runway.name}</div>
                          <div style={{ marginTop: "6px", fontSize: "12px" }}>
                            <span style={{ color: runway.isActive ? C.activeGreen : C.textMuted, fontWeight: 600 }}>
                              {runway.isActive ? "● Active" : "○ Inactive"}
                            </span>
                            <span style={{ color: C.textSub, marginLeft: "8px" }}>{runwayTypeLabel(runway.runwayType)}</span>
                          </div>
                        </div>
                        <div style={{ display: "flex", gap: "6px", alignItems: "flex-start" }}>
                          <button onClick={() => startEditRunway(runway)} style={S.secondaryBtn}>Edit</button>
                          <button onClick={() => deleteRunway(runway.id)} style={S.dangerBtn}>Delete</button>
                        </div>
                      </div>
                    ) : (
                      <div style={{ display: "grid", gap: "8px" }}>
                        <input value={editName} onChange={(e) => setEditName(e.target.value)} placeholder="Runway name" style={S.input} />
                        <select value={editIsActive ? "true" : "false"} onChange={(e) => setEditIsActive(e.target.value === "true")} style={S.input}>
                          <option value="true">Active</option>
                          <option value="false">Inactive</option>
                        </select>
                        <select value={editRunwayType} onChange={(e) => setEditRunwayType(Number(e.target.value))} style={S.input}>
                          <option value={0}>Landing</option>
                          <option value={1}>Takeoff</option>
                          <option value={2}>Both</option>
                        </select>
                        <div style={{ display: "flex", gap: "8px" }}>
                          <button onClick={() => saveRunway(runway.id)} style={S.primaryBtn}>Save</button>
                          <button onClick={() => setEditingRunwayId(null)} style={S.secondaryBtn}>Cancel</button>
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Create airport modal */}
      {showNewAirport && (
        <Modal onClose={() => setShowNewAirport(false)} title="Create airport">
          <div style={{ display: "grid", gap: "10px" }}>
            <input value={newAirportName} onChange={(e) => setNewAirportName(e.target.value)} placeholder="Airport name" style={S.input} />
            <input type="number" value={newAirportStandCapacity} onChange={(e) => setNewAirportStandCapacity(Number(e.target.value))} placeholder="Stand capacity" style={S.input} />
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "8px" }}>
              <input type="number" value={newAirportLatitude} onChange={(e) => setNewAirportLatitude(Number(e.target.value))} placeholder="Latitude" style={S.input} />
              <input type="number" value={newAirportLongitude} onChange={(e) => setNewAirportLongitude(Number(e.target.value))} placeholder="Longitude" style={S.input} />
            </div>
            <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "4px" }}>
              <button onClick={() => setShowNewAirport(false)} style={S.secondaryBtn}>Cancel</button>
              <button onClick={createAirport} style={S.primaryBtn} disabled={creatingAirport || newAirportName.trim().length === 0}>
                {creatingAirport ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Create runway modal */}
      {showNewRunway && (
        <Modal onClose={() => setShowNewRunway(false)} title="Create runway">
          <div style={{ display: "grid", gap: "10px" }}>
            <input value={newRunwayName} onChange={(e) => setNewRunwayName(e.target.value)} placeholder="Runway name" style={S.input} />
            <select value={newRunwayIsActive ? "true" : "false"} onChange={(e) => setNewRunwayIsActive(e.target.value === "true")} style={S.input}>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
            <select value={newRunwayType} onChange={(e) => setNewRunwayType(Number(e.target.value))} style={S.input}>
              <option value={0}>Landing</option>
              <option value={1}>Takeoff</option>
              <option value={2}>Both</option>
            </select>
            <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "4px" }}>
              <button onClick={() => setShowNewRunway(false)} style={S.secondaryBtn}>Cancel</button>
              <button onClick={createRunway} style={S.primaryBtn} disabled={creatingRunway || newRunwayName.trim().length === 0}>
                {creatingRunway ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}
