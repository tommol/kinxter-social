"use client";

import { useState } from "react";

type ApiStatus = "idle" | "checking" | "online" | "offline";

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:8080";

export default function Home() {
  const [status, setStatus] = useState<ApiStatus>("idle");
  const [message, setMessage] = useState("API status has not been checked yet.");

  async function checkApi() {
    setStatus("checking");
    setMessage("Checking API health endpoint...");

    try {
      const response = await fetch(`${apiBaseUrl}/health`, {
        cache: "no-store",
      });

      if (!response.ok) {
        throw new Error(`Health check failed with HTTP ${response.status}.`);
      }

      const payload = (await response.json()) as {
        status?: string;
        service?: string;
      };

      setStatus("online");
      setMessage(`${payload.service ?? "API"} is ${payload.status ?? "online"}.`);
    } catch (error) {
      setStatus("offline");
      setMessage(error instanceof Error ? error.message : "API is unavailable.");
    }
  }

  return (
    <main className="shell">
      <section className="workspace">
        <div className="intro">
          <p className="eyebrow">Kinxter Social</p>
          <h1>Web client workspace</h1>
          <p className="summary">
            Minimal Next.js client prepared for the monorepo container setup.
          </p>
        </div>

        <div className="statusPanel">
          <div>
            <span className={`statusDot ${status}`} aria-hidden="true" />
            <span className="statusLabel">{status}</span>
          </div>
          <p>{message}</p>
          <button type="button" onClick={checkApi} disabled={status === "checking"}>
            {status === "checking" ? "Checking" : "Check API"}
          </button>
          <code>{apiBaseUrl}/health</code>
        </div>
      </section>
    </main>
  );
}
