import { useState, useEffect, useCallback } from 'react';
import { apiFetch, apiFetchJson } from '../lib/api';
import type { ScenarioConfigDto } from '../lib/api';

export function useScenarios(airportId: string | null) {
  const [data, setData] = useState<ScenarioConfigDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fetchCount, setFetchCount] = useState(0);

  useEffect(() => {
    if (!airportId) return;
    let cancelled = false;
    setIsLoading(true);
    setError(null);
    apiFetchJson<ScenarioConfigDto[]>(`/api/scenario-configs?airportId=${airportId}`)
      .then(d => { if (!cancelled) setData(d); })
      .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load scenarios'); })
      .finally(() => { if (!cancelled) setIsLoading(false); });
    return () => { cancelled = true; };
  }, [airportId, fetchCount]);

  const refetch = useCallback(() => setFetchCount(c => c + 1), []);
  return { data: airportId ? data : [], isLoading, error, refetch };
}

export function useCreateScenario() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (payload: Partial<ScenarioConfigDto> & { airportId: string; name: string }): Promise<ScenarioConfigDto> => {
    setIsLoading(true);
    setError(null);
    try {
      return await apiFetchJson<ScenarioConfigDto>('/api/scenario-config', {
        method: 'POST',
        body: JSON.stringify(payload),
      });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to create scenario';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useDeleteScenario() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (scenarioId: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/scenario-configs/${scenarioId}`, { method: 'DELETE' });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to delete scenario';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useGenerateFlights() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (scenarioConfigId: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/scenario-configs/${scenarioConfigId}/flights`, { method: 'POST' });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to generate flights';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}
