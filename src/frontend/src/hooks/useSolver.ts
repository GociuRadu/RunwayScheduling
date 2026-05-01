import { useState, useCallback } from 'react';
import { apiFetchJson } from '../lib/api';
import type { SolverResultDto } from '../lib/api';

export function useSolveGreedy() {
  const [data, setData] = useState<SolverResultDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (scenarioConfigId: string): Promise<SolverResultDto> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await apiFetchJson<SolverResultDto>(`/api/solver/greedy/${scenarioConfigId}`, { method: 'POST' });
      setData(result);
      return result;
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Solver failed';
      setError(msg);
      throw e;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { data, mutate, isLoading, error };
}

export function useSolverResult(_scenarioConfigId: string | null) {
  const [data] = useState<SolverResultDto | null>(null);
  const [isLoading] = useState(false);
  const [error] = useState<string | null>(null);

  const refetch = useCallback(() => {}, []);
  return { data, isLoading, error, refetch };
}
