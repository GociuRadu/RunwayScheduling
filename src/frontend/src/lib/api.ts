import { clearAuthSession, getValidAccessToken } from "./auth";

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  problemDetails?: ProblemDetails;

  constructor(status: number, message: string, problemDetails?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problemDetails = problemDetails;
  }
}

export async function apiFetch(input: string, init: RequestInit = {}) {
  const token = getValidAccessToken();

  const headers = new Headers(init.headers || {});
  headers.set("Accept", "application/json");

  if (init.body && !(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(input, {
    ...init,
    headers,
  });

  if (!response.ok) {
    const problemDetails = await readProblemDetails(response);

    if (response.status === 401) {
      clearAuthSession();
    }

    throw new ApiError(
      response.status,
      buildApiErrorMessage(response.status, problemDetails),
      problemDetails,
    );
  }

  return response;
}

export async function apiFetchJson<T>(input: string, init: RequestInit = {}) {
  const response = await apiFetch(input, init);
  return (await response.json()) as T;
}

export function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof ApiError || error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("application/json")) {
    return undefined;
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

function buildApiErrorMessage(status: number, problemDetails?: ProblemDetails): string {
  const validationMessage = flattenValidationErrors(problemDetails?.errors);
  if (validationMessage) {
    return validationMessage;
  }

  if (problemDetails?.detail) {
    return problemDetails.detail;
  }

  switch (status) {
    case 401:
      return "Session expired. Please sign in again.";
    case 403:
      return "You do not have permission to perform this action.";
    case 404:
      return "The requested resource was not found.";
    case 429:
      return "Too many requests. Please try again later.";
    default:
      return status >= 500
        ? "Server error. Please try again."
        : `Request failed (${status}).`;
  }
}

function flattenValidationErrors(errors?: Record<string, string[]>): string | null {
  if (!errors) {
    return null;
  }

  const messages = Object.values(errors)
    .flatMap((value) => value)
    .filter(Boolean);

  return messages.length > 0 ? messages[0] : null;
}
