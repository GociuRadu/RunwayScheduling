import { useState, useEffect, useCallback } from 'react';
import { apiFetch, apiFetchJson } from '../lib/api';
import type { AirportDto, RunwayDto } from '../lib/api';

export function useAirports() {
  const [data, setData] = useState<AirportDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fetchCount, setFetchCount] = useState(0);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const d = await apiFetchJson<AirportDto[]>('/api/airports');
        if (!cancelled) setData(d);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load airports');
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };
    load();
    return () => { cancelled = true; };
  }, [fetchCount]);

  const refetch = useCallback(() => setFetchCount(c => c + 1), []);
  return { data, isLoading, error, refetch };
}

export function useCreateAirport() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (payload: Omit<AirportDto, 'id'>): Promise<AirportDto> => {
    setIsLoading(true);
    setError(null);
    try {
      return await apiFetchJson<AirportDto>('/api/airport', {
        method: 'POST',
        body: JSON.stringify(payload),
      });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to create airport';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useDeleteAirport() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (airportId: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/airports/${airportId}`, { method: 'DELETE' });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to delete airport';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useRunways(airportId: string | null) {
  const [data, setData] = useState<RunwayDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fetchCount, setFetchCount] = useState(0);

  useEffect(() => {
    if (!airportId) return;
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const d = await apiFetchJson<RunwayDto[]>(`/api/airports/${airportId}/runways`);
        if (!cancelled) setData(d);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load runways');
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };
    load();
    return () => { cancelled = true; };
  }, [airportId, fetchCount]);

  const refetch = useCallback(() => setFetchCount(c => c + 1), []);
  return { data: airportId ? data : [], isLoading, error, refetch };
}

export function useCreateRunway() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (airportId: string, payload: { name: string; isActive: boolean; runwayType: number }): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/airports/${airportId}/runways`, {
        method: 'POST',
        body: JSON.stringify({ airportId, ...payload }),
      });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to create runway';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useDeleteRunway() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (runwayId: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/runways/${runwayId}`, { method: 'DELETE' });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to delete runway';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}

export function useUpdateRunway() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (runwayId: string, payload: { airportId: string; name: string; isActive: boolean; runwayType: number }): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await apiFetch(`/api/runways/${runwayId}`, {
        method: 'PUT',
        body: JSON.stringify(payload),
      });
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to update runway';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { mutate, isLoading, error };
}
