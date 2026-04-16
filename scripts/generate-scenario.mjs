// Generates a hard 500-flight scenario JSON for the solver import feature.
// Run with: node scripts/generate-scenario.mjs

import { writeFileSync } from "fs";

const START = new Date("2026-06-15T05:00:00Z");
const END   = new Date("2026-06-15T21:00:00Z");
const WINDOW_MS = END - START; // 16 hours

const AIRLINES = [
  { prefix: "ROT", name: "TAROM" },
  { prefix: "WZZ", name: "Wizz Air" },
  { prefix: "DLH", name: "Lufthansa" },
  { prefix: "BAW", name: "British Airways" },
  { prefix: "AFR", name: "Air France" },
  { prefix: "KLM", name: "KLM" },
  { prefix: "EZY", name: "EasyJet" },
  { prefix: "TUI", name: "TUI" },
  { prefix: "TRA", name: "Transavia" },
  { prefix: "IBE", name: "Iberia" },
  { prefix: "VLG", name: "Vueling" },
  { prefix: "RYR", name: "Ryanair" },
  { prefix: "SWR", name: "Swiss" },
  { prefix: "AUA", name: "Austrian" },
  { prefix: "CSA", name: "Czech Airlines" },
  { prefix: "LOT", name: "LOT Polish" },
  { prefix: "FIN", name: "Finnair" },
  { prefix: "SAS", name: "Scandinavian" },
  { prefix: "TAP", name: "TAP Portugal" },
  { prefix: "UAE", name: "Emirates" },
];

function rng(seed) {
  // Simple seeded LCG PRNG
  let s = seed;
  return () => {
    s = (s * 1664525 + 1013904223) & 0xffffffff;
    return (s >>> 0) / 0xffffffff;
  };
}

const rand = rng(42);
const ri = (min, max) => Math.floor(rand() * (max - min + 1)) + min;
const pick = (arr) => arr[ri(0, arr.length - 1)];

// --- Runways: 2 runways ---
const runways = [
  { name: "08L", runwayType: 0, isActive: true },  // Landing only
  { name: "08R", runwayType: 1, isActive: true },  // Takeoff only
];

// --- Flights ---
const flights = [];
const usedCallsigns = new Set();

// 480 arrivals, 480 departures, 40 on-ground parked = 1000... too many
// Let's do 300 arrivals + 185 departures + 15 on-ground = 500
const ARRIVALS   = 300;
const DEPARTURES = 185;
const ON_GROUND  = 15;

for (let i = 0; i < ARRIVALS + DEPARTURES + ON_GROUND; i++) {
  const airline = pick(AIRLINES);
  let callsign;
  let attempts = 0;
  do {
    callsign = `${airline.prefix}${ri(100, 9999)}`;
    attempts++;
  } while (usedCallsigns.has(callsign) && attempts < 100);
  usedCallsigns.add(callsign);

  let type;
  if (i < ARRIVALS)              type = 0; // Arrival
  else if (i < ARRIVALS + DEPARTURES) type = 1; // Departure
  else                            type = 2; // OnGround

  // Spread flights across window, slightly clustered around morning/evening rush
  let offsetMs;
  if (type === 2) {
    offsetMs = ri(0, 30) * 60 * 1000; // OnGround at start
  } else {
    // Create two rush peaks: 06:00-09:00 and 15:00-18:00
    const peak = rand() < 0.6
      ? ri(0, 3 * 60)          // morning rush (first 3h)
      : ri(3 * 60, WINDOW_MS / 60000); // rest of day
    offsetMs = peak * 60 * 1000;
  }

  const scheduledTime = new Date(START.getTime() + Math.min(offsetMs, WINDOW_MS - 60000));
  const maxDelayMinutes = type === 2 ? 0 : pick([15, 20, 25, 30, 45, 60]);
  const maxEarlyMinutes = type === 2 ? 0 : pick([0, 5, 10, 15, 20]);
  const priority        = type === 2 ? 1 : pick([1, 1, 2, 2, 2, 3, 3]);

  flights.push({
    callsign,
    type,
    scheduledTime: scheduledTime.toISOString(),
    maxDelayMinutes,
    maxEarlyMinutes,
    priority,
  });
}

// --- Weather intervals: 4 intervals across the day ---
const weatherIntervals = [
  { startTime: "2026-06-15T06:30:00Z", endTime: "2026-06-15T08:00:00Z", weatherType: 2 }, // Rain
  { startTime: "2026-06-15T09:30:00Z", endTime: "2026-06-15T10:30:00Z", weatherType: 1 }, // Cloud
  { startTime: "2026-06-15T12:00:00Z", endTime: "2026-06-15T13:30:00Z", weatherType: 4 }, // Fog
  { startTime: "2026-06-15T16:00:00Z", endTime: "2026-06-15T17:00:00Z", weatherType: 5 }, // Storm
];

// --- Random events: 3 disruptions ---
const randomEvents = [
  {
    name: "VIP Diplomatic Arrival",
    description: "Priority corridor — all runways blocked for state aircraft",
    startTime: "2026-06-15T07:45:00Z",
    endTime:   "2026-06-15T08:05:00Z",
    impactPercent: 100,
  },
  {
    name: "Bird Strike Inspection",
    description: "08L inspection after bird strike report",
    startTime: "2026-06-15T11:00:00Z",
    endTime:   "2026-06-15T11:30:00Z",
    impactPercent: 60,
  },
  {
    name: "Emergency Landing",
    description: "Medical emergency — runway 26L reserved",
    startTime: "2026-06-15T14:30:00Z",
    endTime:   "2026-06-15T14:50:00Z",
    impactPercent: 100,
  },
];

const scenario = {
  algorithm: "greedy",
  scenarioConfig: {
    name: "LROP Hard — 500 Flights",
    startTime: START.toISOString(),
    endTime:   END.toISOString(),
    baseSeparationSeconds: 45,
  },
  runways,
  flights,
  weatherIntervals,
  randomEvents,
};

const outPath = "scripts/scenario-500.json";
writeFileSync(outPath, JSON.stringify(scenario, null, 2));
console.log(`Generated ${flights.length} flights → ${outPath}`);
console.log(`  Arrivals:   ${flights.filter(f => f.type === 0).length}`);
console.log(`  Departures: ${flights.filter(f => f.type === 1).length}`);
console.log(`  OnGround:   ${flights.filter(f => f.type === 2).length}`);
