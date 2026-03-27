export async function apiFetch(input: string, init: RequestInit = {}) {
  const token = localStorage.getItem("token");

  const headers = new Headers(init.headers || {});
  headers.set("Accept", "application/json");

  if (!(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(input, {
    ...init,
    headers,
  });

  if (response.status === 401 && token) {
    localStorage.removeItem("token");
    window.location.href = "/";
    throw new Error("Invalid Token");
  }

  return response;
}
