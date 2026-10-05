import { updateLocation } from "./profileApi";

export function requestAndShareLocation(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error("Geolocation is not supported by this browser."));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        updateLocation(position.coords.latitude, position.coords.longitude)
          .then(resolve)
          .catch(reject);
      },
      (error) => reject(new Error(error.message || "Could not get your location.")),
      { timeout: 10000 }
    );
  });
}
