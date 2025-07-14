/**
 * Sends a POST request to the specified path with the provided parameters.
 * @param path - The endpoint path to which the request is sent.
 * @param params - The parameters to be sent in the request body. Defaults to an empty array.
 * @returns
 */
export async function send(path: string, params: any = []): Promise<any> {
  var response = await fetch(
    `/${path}`,
    {
      method: "POST",
      body: JSON.stringify(params),
      headers: {
        "X-Is-Custom": "true",
      },
    },
  );

  try {
    var obj = await response.json();
    var data = obj.data ?? null;
    return data;
  } catch {
    return null;
  }
}
