import { useCallback, useState } from "react";
import axiosClient from "../api/axiosClient";

/**
 * useApiResource — the ONE hook every module's data-fetching goes through.
 *
 * Frontend requirement: "Custom Hook to Call API [Do not repeat code for
 * Get and getById]". Both list-fetching and single-record-fetching are
 * implemented as thin wrappers around the same private `request()`
 * executor below, so there is exactly one place that owns loading/error
 * state and one place that talks to Axios — not two near-duplicate hooks.
 *
 * Usage:
 *   const courses = useApiResource("/courses");
 *   useEffect(() => { courses.fetchAll(); }, []);
 *   useEffect(() => { courses.fetchById(id); }, [id]);
 *
 * Works for every master/table-maintenance table (courses, recruiters,
 * announcements, closure_reasons, banners, ...) and every CRM entity
 * (inquiries, followups, students, ...) without writing a new hook per
 * table — that reuse is the actual point of this file.
 */
export default function useApiResource(baseEndpoint) {
  const [data, setData] = useState([]); // list results
  const [item, setItem] = useState(null); // single-record result
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Every read AND write in this hook funnels through here. This is the
  // one place that manages loading/error state and unwraps the response —
  // fetchAll and fetchById below are just different `path`/`onSuccess`
  // arguments to the exact same executor, not separate implementations.
  const request = useCallback(async (method, path, { params, body, onSuccess } = {}) => {
    setLoading(true);
    setError(null);
    try {
      const response = await axiosClient.request({
        method,
        url: path,
        params,
        data: body,
      });
      onSuccess?.(response.data);
      return response.data;
    } catch (err) {
      setError(err.message || "Request failed");
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchAll = useCallback(
    (params) => request("get", baseEndpoint, { params, onSuccess: setData }),
    [request, baseEndpoint]
  );

  const fetchById = useCallback(
    (id) => request("get", `${baseEndpoint}/${id}`, { onSuccess: setItem }),
    [request, baseEndpoint]
  );

  const create = useCallback(
    (payload) => request("post", baseEndpoint, { body: payload }),
    [request, baseEndpoint]
  );

  const update = useCallback(
    (id, payload) => request("put", `${baseEndpoint}/${id}`, { body: payload }),
    [request, baseEndpoint]
  );

  const remove = useCallback(
    (id) => request("delete", `${baseEndpoint}/${id}`),
    [request, baseEndpoint]
  );

  return { data, item, loading, error, fetchAll, fetchById, create, update, remove };
}
