import axiosClient from "../api/axiosClient";

/**
 * Opens a payment receipt PDF in a new tab.
 *
 * A plain <a href="/api/payments/1/receipt"> can't work: a browser
 * navigation carries cookies, not headers, and this API is stateless —
 * the JWT lives in localStorage and is only attached by axios. The
 * request would arrive anonymous and be rejected, which is exactly right
 * of the server (a receipt carries a student's name, fees and contact
 * details) and exactly wrong for the user.
 *
 * So the PDF is fetched with the header attached, wrapped in a blob URL,
 * and handed to the tab.
 */
export default async function openReceipt(paymentId) {
  // The tab is opened *before* the await. Browsers only treat
  // window.open as user-initiated during the click's own task, so
  // opening it after the network round-trip gets it blocked as a popup.
  const tab = window.open("", "_blank");

  try {
    const { data } = await axiosClient.get(`/payments/${paymentId}/receipt`, {
      responseType: "blob",
    });

    const url = URL.createObjectURL(new Blob([data], { type: "application/pdf" }));

    if (tab && !tab.closed) {
      tab.location.href = url;
    } else {
      // Popup blocked anyway — fall back to a download so the click still
      // produces the receipt rather than silently doing nothing.
      const link = document.createElement("a");
      link.href = url;
      link.download = `receipt-${paymentId}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
    }

    // Give the tab time to load before releasing the object URL; revoking
    // immediately leaves it showing a blank page.
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch (err) {
    tab?.close();
    throw err;
  }
}
