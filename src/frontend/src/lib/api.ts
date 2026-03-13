// wrapper over fetch that automatically sends the JWT token
export async function apiFetch(input: string, init: RequestInit = {}) {
  const token = localStorage.getItem("token");

  // build headers and attach JSON defaults
  const headers = new Headers(init.headers || {});
  headers.set("Accept", "application/json");

  // keep browser-managed content type for FormData
  if (!(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  // attach JWT if user is logged in
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  // send the request with merged options
  const response = await fetch(input, {
    ...init,
    headers,
  });

  // if token is rejected, clear session and redirect
  if (response.status === 401) {
    localStorage.removeItem("token");
    window.location.href = "/";
    throw new Error("Invalid Token");
  }

  return response;
}