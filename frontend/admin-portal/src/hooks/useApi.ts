import { useState, useEffect, useCallback, useRef } from 'react';
import { handleApiError } from '../services/api';

// Generic API hook for data fetching
export function useApi<T>(
  apiCall: () => Promise<T>,
  dependencies: unknown[] = []
) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const apiCallRef = useRef(apiCall);
  
  // Update the ref when apiCall changes
  useEffect(() => {
    apiCallRef.current = apiCall;
  }, [apiCall]);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCallRef.current();
      setData(result);
    } catch (err) {
      setError(handleApiError(err));
    } finally {
      setLoading(false);
    }
  }, [...dependencies]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return {
    data,
    loading,
    error,
    refetch: fetchData,
  };
}

// API hook for mutations (create, update, delete)
export function useMutation<TData, TVariables = void>(
  mutationFn: (variables: TVariables) => Promise<TData>
) {
  const [data, setData] = useState<TData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (variables: TVariables) => {
    try {
      setLoading(true);
      setError(null);
      const result = await mutationFn(variables);
      setData(result);
      return result;
    } catch (err) {
      const errorMessage = handleApiError(err);
      setError(errorMessage);
      throw new Error(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [mutationFn]);

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setLoading(false);
  }, []);

  return {
    data,
    loading,
    error,
    mutate,
    reset,
  };
}

// Paginated API hook
export function usePaginatedApi<T>(
  apiCall: (page: number, pageSize: number) => Promise<{ items: T[]; totalCount: number }>,
  initialPageSize: number = 10
) {
  const [data, setData] = useState<T[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [totalCount, setTotalCount] = useState(0);
  const apiCallRef = useRef(apiCall);

  // Update the ref when apiCall changes
  useEffect(() => {
    apiCallRef.current = apiCall;
  }, [apiCall]);

  const fetchData = useCallback(async (pageNum: number, size: number) => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCallRef.current(pageNum, size);
      setData(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(handleApiError(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData(page, pageSize);
  }, [fetchData, page, pageSize]);

  const nextPage = useCallback(() => {
    if (page * pageSize < totalCount) {
      setPage(p => p + 1);
    }
  }, [page, pageSize, totalCount]);

  const prevPage = useCallback(() => {
    if (page > 1) {
      setPage(p => p - 1);
    }
  }, [page]);

  const goToPage = useCallback((pageNum: number) => {
    setPage(pageNum);
  }, []);

  const changePageSize = useCallback((size: number) => {
    setPageSize(size);
    setPage(1); // Reset to first page
  }, []);

  const totalPages = Math.ceil(totalCount / pageSize);

  return {
    data,
    loading,
    error,
    page,
    pageSize,
    totalCount,
    totalPages,
    hasNextPage: page * pageSize < totalCount,
    hasPrevPage: page > 1,
    nextPage,
    prevPage,
    goToPage,
    changePageSize,
    refetch: () => fetchData(page, pageSize),
  };
} 