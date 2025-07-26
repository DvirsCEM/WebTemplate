/**
 * Sends a POST request to the specified path with the provided parameters.
 * @param path - The endpoint path to which the request is sent.
 * @param params - The parameters to be sent in the request body. Defaults to an empty array.
 * @returns A promise that resolves to the data returned from the server, or null if an error occurs.
 */
export async function send<T = any>(
  path: string,
  ...params: any[]
): Promise<T> {
  var response = await fetch(
    `/${path}`,
    {
      method: "POST",
      body: JSON.stringify(params.length > 1 ? params : params[0] ?? []),
      headers: { "X-Custom-Request": "true" },
    },
  );

  try {
    var obj = await response.json();
    var data = obj.data ?? null;
    return data;
  } catch {
    return null!;
  }
}
