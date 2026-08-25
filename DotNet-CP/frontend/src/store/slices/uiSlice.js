import { createSlice } from "@reduxjs/toolkit";

// Small global slice for cross-cutting UI concerns — e.g. a toast/banner
// after "Enquiry confirmation email sent" or "Receipt generated" that
// isn't naturally owned by any single page.
const uiSlice = createSlice({
  name: "ui",
  initialState: {
    notification: null, // { type: 'success' | 'error', message: string }
  },
  reducers: {
    showNotification(state, action) {
      state.notification = action.payload;
    },
    clearNotification(state) {
      state.notification = null;
    },
  },
});

export const { showNotification, clearNotification } = uiSlice.actions;
export default uiSlice.reducer;
