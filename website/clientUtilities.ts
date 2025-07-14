export async function send(path: string, params: any = []): Promise<any> {
  var response = await fetch(
    `/${path}`,
    {
      method: "POST",
      body: JSON.stringify(params),
      headers: {
        "X-Is-Custom": "true"
      }
    }
  );

  try {
    var obj = await response.json();
    var data = obj.data ?? null;
    return data;
  }
  catch {
    return null;
  }
};