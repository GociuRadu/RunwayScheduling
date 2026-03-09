import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

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

export default function AirportsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const [airports, setAirports] = useState<AirportDto[]>([]);
  const [runways, setRunways] = useState<RunwayDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [airportsLoaded, setAirportsLoaded] = useState(false);
  const [runwaysLoaded, setRunwaysLoaded] = useState(false);
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

  const selectedAirport = useMemo(() => {
    return airports.find((a) => a.id === selectedAirportId) ?? null;
  }, [airports, selectedAirportId]);

  useEffect(() => {
    if (!selectedAirportId) return;

    localStorage.setItem(STORAGE_AIRPORT_ID, selectedAirportId);

    const params = new URLSearchParams(searchParams);
    params.set("airportId", selectedAirportId);
    setSearchParams(params, { replace: true });
  }, [selectedAirportId, searchParams, setSearchParams]);

  useEffect(() => {
    const airportNameFromList = selectedAirport?.name;
    if (!airportNameFromList) return;

    setSelectedAirportName(airportNameFromList);
    localStorage.setItem(STORAGE_AIRPORT_NAME, airportNameFromList);
  }, [selectedAirport]);

  function clearSelectedAirport() {
    setSelectedAirportId(null);
    setSelectedAirportName("");
    setRunways([]);
    setRunwaysLoaded(false);
    setEditingRunwayId(null);

    localStorage.removeItem(STORAGE_AIRPORT_ID);
    localStorage.removeItem(STORAGE_AIRPORT_NAME);

    const params = new URLSearchParams(searchParams);
    params.delete("airportId");
    setSearchParams(params, { replace: true });
  }

  async function fetchAirports() {
    try {
      setError(null);
      setLoadingAirports(true);

      const res = await fetch("/api/airports");
      if (!res.ok) throw new Error(`Failed to load airports: ${res.status}`);

      const data = (await res.json()) as AirportDto[];
      setAirports(data);
      setAirportsLoaded(true);

      if (selectedAirportId) {
        const matched = data.find((a) => a.id === selectedAirportId);
        if (matched) {
          setSelectedAirportName(matched.name);
          localStorage.setItem(STORAGE_AIRPORT_NAME, matched.name);
        }
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setLoadingAirports(false);
    }
  }

  async function fetchRunways(airportId: string) {
    try {
      setError(null);
      setLoadingRunways(true);
      setEditingRunwayId(null);

      const res = await fetch(`/api/airports/${airportId}/runways`);
      if (!res.ok) throw new Error(`Failed to load runways: ${res.status}`);

      const data = (await res.json()) as RunwayDto[];
      setRunways(data);
      setRunwaysLoaded(true);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
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
    setRunwaysLoaded(false);
    setEditingRunwayId(null);
  }

  async function createAirport() {
    try {
      setError(null);
      setCreatingAirport(true);

      const res = await fetch("/api/airport", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: newAirportName.trim(),
          standCapacity: newAirportStandCapacity,
          latitude: newAirportLatitude,
          longitude: newAirportLongitude,
        }),
      });

      if (!res.ok) throw new Error(`Failed to create airport: ${res.status}`);

      const created = (await res.json()) as AirportDto;

      if (airportsLoaded) {
        setAirports((prev) => [created, ...prev]);
      }

      selectAirport(created);
      setShowNewAirport(false);

      setNewAirportName("");
      setNewAirportStandCapacity(20);
      setNewAirportLatitude(0);
      setNewAirportLongitude(0);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setCreatingAirport(false);
    }
  }

  async function createRunway() {
    if (!selectedAirportId) return;

    try {
      setError(null);
      setCreatingRunway(true);

      const res = await fetch(`/api/airports/${selectedAirportId}/runways`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          airportId: selectedAirportId,
          name: newRunwayName.trim(),
          isActive: newRunwayIsActive,
          runwayType: newRunwayType,
        }),
      });

      if (!res.ok) throw new Error(`Failed to create runway: ${res.status}`);

      setShowNewRunway(false);
      setNewRunwayName("");
      setNewRunwayIsActive(true);
      setNewRunwayType(2);

      await fetchRunways(selectedAirportId);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setCreatingRunway(false);
    }
  }

  async function deleteAirport(airportId: string) {
    try {
      setError(null);

      const res = await fetch(`/api/airports/${airportId}`, {
        method: "DELETE",
      });

      if (!res.ok && res.status !== 204) {
        throw new Error(`Failed to delete airport: ${res.status}`);
      }

      setAirports((prev) => prev.filter((a) => a.id !== airportId));

      if (selectedAirportId === airportId) {
        clearSelectedAirport();
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    }
  }

  async function deleteRunway(runwayId: string) {
    try {
      setError(null);

      const res = await fetch(`/api/runways/${runwayId}`, {
        method: "DELETE",
      });

      if (!res.ok && res.status !== 204) {
        throw new Error(`Failed to delete runway: ${res.status}`);
      }

      setRunways((prev) => prev.filter((r) => r.id !== runwayId));

      if (editingRunwayId === runwayId) {
        setEditingRunwayId(null);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
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
      setError(null);

      const res = await fetch(`/api/runways/${runwayId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          airportId: selectedAirportId,
          name: editName.trim(),
          isActive: editIsActive,
          runwayType: editRunwayType,
        }),
      });

      if (!res.ok && res.status !== 204) {
        throw new Error(`Failed to update runway: ${res.status}`);
      }

      setRunways((prev) =>
        prev.map((r) =>
          r.id === runwayId
            ? {
                ...r,
                name: editName.trim(),
                isActive: editIsActive,
                runwayType: editRunwayType,
              }
            : r,
        ),
      );

      setEditingRunwayId(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    }
  }

  function runwayTypeLabel(type: number) {
    switch (type) {
      case 0:
        return "Landing";
      case 1:
        return "Takeoff";
      case 2:
        return "Both";
      default:
        return `Type ${type}`;
    }
  }

  const pageStyle: React.CSSProperties = {
    maxWidth: "1200px",
    margin: "0 auto",
    padding: "8px 8px 40px",
  };

  const cardStyle: React.CSSProperties = {
    border: "1px solid rgba(255,255,255,0.12)",
    borderRadius: "16px",
    background: "rgba(255,255,255,0.04)",
    padding: "18px",
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

  const inputStyle: React.CSSProperties = {
    width: "100%",
    padding: "10px 12px",
    borderRadius: "10px",
    border: "1px solid rgba(255,255,255,0.16)",
    background: "rgba(0,0,0,0.28)",
    color: "white",
    outline: "none",
  };

  return (
    <div style={pageStyle}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "16px",
          marginBottom: "20px",
          flexWrap: "wrap",
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Airports</h1>
          <div style={{ opacity: 0.75, marginTop: "6px" }}>
          </div>
        </div>

        <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
          <button onClick={fetchAirports} style={secondaryBtn}>
            {loadingAirports ? "Loading..." : "Show airports"}
          </button>

          <button onClick={() => setShowNewAirport(true)} style={primaryBtn}>
            Create airport
          </button>

          {selectedAirportId && (
            <button
              onClick={() =>
                navigate(`/scenario-config?airportId=${selectedAirportId}`)
              }
              style={secondaryBtn}
            >
              Go to Scenario Config
            </button>
          )}
        </div>
      </div>

      {error && (
        <div
          style={{
            ...cardStyle,
            borderColor: "rgba(255,80,80,0.45)",
            color: "#ff9d9d",
            marginBottom: "18px",
          }}
        >
          {error}
        </div>
      )}

      <div
        style={{
          ...cardStyle,
          marginBottom: "20px",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "18px",
          flexWrap: "wrap",
        }}
      >
        <div>
          <div style={{ fontSize: "13px", opacity: 0.7 }}>Selected airport</div>
          <div style={{ fontSize: "22px", fontWeight: 800, marginTop: "4px" }}>
            {selectedAirport?.name || selectedAirportName || "No airport selected"}
          </div>

          {(selectedAirport || selectedAirportName) && (
            <div style={{ marginTop: "10px", opacity: 0.86, fontSize: "14px" }}>
              {selectedAirport && (
                <>
                  <div>Stand capacity: {selectedAirport.standCapacity}</div>
                  <div>
                    Coordinates: {selectedAirport.latitude}, {selectedAirport.longitude}
                  </div>
                </>
              )}
            </div>
          )}
        </div>

        <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
          <button
            onClick={() => {
              if (selectedAirportId) fetchRunways(selectedAirportId);
            }}
            style={secondaryBtn}
            disabled={!selectedAirportId || loadingRunways}
          >
            {loadingRunways ? "Loading..." : "Show runways"}
          </button>

          <button
            onClick={() => setShowNewRunway(true)}
            style={primaryBtn}
            disabled={!selectedAirportId}
          >
            Create runway
          </button>

          <button
            onClick={clearSelectedAirport}
            style={dangerBtn}
            disabled={!selectedAirportId}
          >
            Clear selection
          </button>
        </div>
      </div>

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1.1fr 1fr",
          gap: "22px",
          alignItems: "start",
        }}
      >
        <div>
          <div style={{ fontSize: "20px", fontWeight: 800, marginBottom: "14px" }}>
            Airports
          </div>

          {!airportsLoaded ? (
            <div style={cardStyle}>
              Airports are hidden by default. Click <b>Show airports</b>.
            </div>
          ) : airports.length === 0 ? (
            <div style={cardStyle}>No airports found.</div>
          ) : (
            <div style={{ display: "grid", gap: "14px" }}>
              {airports.map((airport) => {
                const isSelected = airport.id === selectedAirportId;

                return (
                  <div
                    key={airport.id}
                    style={{
                      ...cardStyle,
                      borderColor: isSelected
                        ? "rgba(15,118,110,0.95)"
                        : "rgba(255,255,255,0.12)",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        gap: "14px",
                        flexWrap: "wrap",
                      }}
                    >
                      <div>
                        <div style={{ fontSize: "18px", fontWeight: 800 }}>
                          {airport.name}
                        </div>
                        <div style={{ marginTop: "10px", opacity: 0.86, fontSize: "14px" }}>
                          <div>Stand capacity: {airport.standCapacity}</div>
                          <div>
                            Coordinates: {airport.latitude}, {airport.longitude}
                          </div>
                        </div>
                      </div>

                      <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
                        <button
                          onClick={() => selectAirport(airport)}
                          style={isSelected ? primaryBtn : secondaryBtn}
                        >
                          {isSelected ? "Selected" : "Select"}
                        </button>

                        <button
                          onClick={() => {
                            selectAirport(airport);
                            fetchRunways(airport.id);
                          }}
                          style={secondaryBtn}
                        >
                          Show runways
                        </button>

                        <button
                          onClick={() => deleteAirport(airport.id)}
                          style={dangerBtn}
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

        <div>
          <div style={{ fontSize: "20px", fontWeight: 800, marginBottom: "14px" }}>
            Runways
          </div>

          {!selectedAirportId ? (
            <div style={cardStyle}>Select an airport first.</div>
          ) : !runwaysLoaded ? (
            <div style={cardStyle}>
              Runways are hidden by default. Click <b>Show runways</b>.
            </div>
          ) : runways.length === 0 ? (
            <div style={cardStyle}>No runways for this airport.</div>
          ) : (
            <div style={{ display: "grid", gap: "14px" }}>
              {runways.map((runway) => {
                const isEditing = editingRunwayId === runway.id;

                return (
                  <div key={runway.id} style={cardStyle}>
                    {!isEditing ? (
                      <>
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            gap: "14px",
                            flexWrap: "wrap",
                          }}
                        >
                          <div>
                            <div style={{ fontSize: "18px", fontWeight: 800 }}>
                              {runway.name}
                            </div>

                            <div style={{ marginTop: "10px", opacity: 0.86, fontSize: "14px" }}>
                              <div>Status: {runway.isActive ? "Active" : "Inactive"}</div>
                              <div>Type: {runwayTypeLabel(runway.runwayType)}</div>
                            </div>
                          </div>

                          <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
                            <button
                              onClick={() => startEditRunway(runway)}
                              style={secondaryBtn}
                            >
                              Edit
                            </button>

                            <button
                              onClick={() => deleteRunway(runway.id)}
                              style={dangerBtn}
                            >
                              Delete
                            </button>
                          </div>
                        </div>
                      </>
                    ) : (
                      <div style={{ display: "grid", gap: "10px" }}>
                        <input
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          placeholder="Runway name"
                          style={inputStyle}
                        />

                        <select
                          value={editIsActive ? "true" : "false"}
                          onChange={(e) => setEditIsActive(e.target.value === "true")}
                          style={inputStyle}
                        >
                          <option value="true">Active</option>
                          <option value="false">Inactive</option>
                        </select>

                        <select
                          value={editRunwayType}
                          onChange={(e) => setEditRunwayType(Number(e.target.value))}
                          style={inputStyle}
                        >
                          <option value={0}>Landing</option>
                          <option value={1}>Takeoff</option>
                          <option value={2}>Both</option>
                        </select>

                        <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
                          <button
                            onClick={() => saveRunway(runway.id)}
                            style={primaryBtn}
                          >
                            Save
                          </button>

                          <button
                            onClick={() => setEditingRunwayId(null)}
                            style={secondaryBtn}
                          >
                            Cancel
                          </button>
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

      {showNewAirport && (
        <Modal onClose={() => setShowNewAirport(false)} title="Create airport">
          <div style={{ display: "grid", gap: "10px" }}>
            <input
              value={newAirportName}
              onChange={(e) => setNewAirportName(e.target.value)}
              placeholder="Airport name"
              style={inputStyle}
            />

            <input
              type="number"
              value={newAirportStandCapacity}
              onChange={(e) => setNewAirportStandCapacity(Number(e.target.value))}
              placeholder="Stand capacity"
              style={inputStyle}
            />

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "10px" }}>
              <input
                type="number"
                value={newAirportLatitude}
                onChange={(e) => setNewAirportLatitude(Number(e.target.value))}
                placeholder="Latitude"
                style={inputStyle}
              />

              <input
                type="number"
                value={newAirportLongitude}
                onChange={(e) => setNewAirportLongitude(Number(e.target.value))}
                placeholder="Longitude"
                style={inputStyle}
              />
            </div>

            <div style={{ display: "flex", justifyContent: "flex-end", gap: "10px" }}>
              <button onClick={() => setShowNewAirport(false)} style={secondaryBtn}>
                Cancel
              </button>

              <button
                onClick={createAirport}
                style={primaryBtn}
                disabled={creatingAirport || newAirportName.trim().length === 0}
              >
                {creatingAirport ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {showNewRunway && (
        <Modal onClose={() => setShowNewRunway(false)} title="Create runway">
          <div style={{ display: "grid", gap: "10px" }}>
            <input
              value={newRunwayName}
              onChange={(e) => setNewRunwayName(e.target.value)}
              placeholder="Runway name"
              style={inputStyle}
            />

            <select
              value={newRunwayIsActive ? "true" : "false"}
              onChange={(e) => setNewRunwayIsActive(e.target.value === "true")}
              style={inputStyle}
            >
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>

            <select
              value={newRunwayType}
              onChange={(e) => setNewRunwayType(Number(e.target.value))}
              style={inputStyle}
            >
              <option value={0}>Landing</option>
              <option value={1}>Takeoff</option>
              <option value={2}>Both</option>
            </select>

            <div style={{ display: "flex", justifyContent: "flex-end", gap: "10px" }}>
              <button onClick={() => setShowNewRunway(false)} style={secondaryBtn}>
                Cancel
              </button>

              <button
                onClick={createRunway}
                style={primaryBtn}
                disabled={creatingRunway || newRunwayName.trim().length === 0}
              >
                {creatingRunway ? "Creating..." : "Create"}
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
        zIndex: 1000,
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "520px",
          maxWidth: "calc(100vw - 24px)",
          borderRadius: "16px",
          padding: "18px",
          background: "#111",
          border: "1px solid rgba(255,255,255,0.12)",
        }}
      >
        <div style={{ fontSize: "18px", fontWeight: 900, marginBottom: "14px" }}>
          {title}
        </div>
        {children}
      </div>
    </div>
  );
}