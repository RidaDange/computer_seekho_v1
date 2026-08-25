import { createSlice } from "@reduxjs/toolkit";
import { TOKEN_KEY, STAFF_KEY } from "../../api/axiosClient";

// Auth is genuinely global (Protected Route, admin nav, and the API
// client's JWT header all need it) so it lives in Redux rather than
// component state.
//
// The staff record is mirrored into localStorage alongside the token so a
// hard refresh doesn't drop the user's role and briefly hide nav items
// they're actually allowed to see. The token remains the only thing the
// server trusts — this copy is purely for rendering.
function readStoredStaff() {
  try {
    const raw = localStorage.getItem(STAFF_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

const storedToken = localStorage.getItem(TOKEN_KEY);

const initialState = {
  staff: readStoredStaff(), // { staffId, name, role, ... }
  token: storedToken || null,
  isAuthenticated: Boolean(storedToken),
};

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    loginSuccess(state, action) {
      const { staff, token } = action.payload;
      state.staff = staff;
      state.token = token;
      state.isAuthenticated = true;
      localStorage.setItem(TOKEN_KEY, token);
      localStorage.setItem(STAFF_KEY, JSON.stringify(staff));
    },
    logout(state) {
      state.staff = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(STAFF_KEY);
    },
  },
});

export const { loginSuccess, logout } = authSlice.actions;
export default authSlice.reducer;

// Shared role helpers — kept next to the slice so the nav, the route guard
// and any screen-level check all agree on what a role can do.
export const MASTER_DATA_ROLES = ["Admin", "Manager"];
export const CRM_ROLES = ["Admin", "Manager", "Counselor", "Receptionist"];

export const selectRole = (state) => state.auth.staff?.role ?? null;
export const hasRole = (role, allowed) => Boolean(role) && allowed.includes(role);
