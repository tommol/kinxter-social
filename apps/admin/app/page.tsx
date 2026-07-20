"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import styles from "./admin.module.css";

type MonitoringStatus = "ok" | "degraded" | "down";

type MonitoringDependency = {
  name: string;
  status: MonitoringStatus;
  detail?: string | null;
};

type MonitoringMetrics = {
  accountCount?: number;
  profileCount?: number;
};

type MonitoringOutbox = {
  moduleName: string;
  pendingCount: number;
  failedCount: number;
  oldestPendingCreatedAt?: string | null;
  lastFailureAt?: string | null;
};

type MonitoringOverview = {
  service: string;
  status: MonitoringStatus;
  checkedAt: string;
  dependencies: MonitoringDependency[];
  metrics: MonitoringMetrics;
  outbox: MonitoringOutbox[];
};

type AdminMonitoringSnapshot = {
  status: MonitoringStatus;
  checkedAt: string;
  latencyMs: number | null;
  apiBaseUrl: string;
  overview: MonitoringOverview | null;
  error?: string;
};

const refreshIntervalMs = 10_000;

const statusCopy: Record<
  MonitoringStatus,
  { label: string; tone: MonitoringStatus }
> = {
  ok: { label: "Operacyjny", tone: "ok" },
  degraded: { label: "Wymaga uwagi", tone: "degraded" },
  down: { label: "Niedostępny", tone: "down" },
};

export default function AdminMonitoringPage() {
  const [snapshot, setSnapshot] = useState<AdminMonitoringSnapshot | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const refresh = useCallback(async () => {
    setIsRefreshing(true);

    try {
      const response = await fetch("/api/monitoring/health", {
        cache: "no-store",
      });
      const payload = (await response.json()) as AdminMonitoringSnapshot;

      setSnapshot({
        ...payload,
        status: normalizeStatus(payload.status),
      });
    } catch (error) {
      setSnapshot({
        status: "down",
        checkedAt: new Date().toISOString(),
        latencyMs: null,
        apiBaseUrl: "-",
        overview: null,
        error: error instanceof Error ? error.message : "Nie udało się odczytać monitoringu.",
      });
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void refresh();

    const interval = window.setInterval(() => {
      void refresh();
    }, refreshIntervalMs);

    return () => window.clearInterval(interval);
  }, [refresh]);

  const overview = snapshot?.overview;
  const status = normalizeStatus(overview?.status ?? snapshot?.status);
  const outboxTotals = useMemo(() => {
    return (overview?.outbox ?? []).reduce(
      (totals, module) => ({
        pending: totals.pending + module.pendingCount,
        failed: totals.failed + module.failedCount,
      }),
      { pending: 0, failed: 0 },
    );
  }, [overview?.outbox]);
  const dependencyCounts = useMemo(() => {
    return (overview?.dependencies ?? []).reduce(
      (totals, dependency) => ({
        online: totals.online + (dependency.status === "ok" ? 1 : 0),
        total: totals.total + 1,
      }),
      { online: 0, total: 0 },
    );
  }, [overview?.dependencies]);
  const statusDetails = statusCopy[status];

  const metricCards = [
    {
      label: "API",
      value: overview?.service ?? "Kinxter.Api",
      meta: snapshot?.apiBaseUrl ?? "-",
    },
    {
      label: "Latencja",
      value: formatLatency(snapshot?.latencyMs),
      meta: "admin -> api",
    },
    {
      label: "Konta",
      value: formatNumber(overview?.metrics.accountCount),
      meta: "accounts",
    },
    {
      label: "Profile",
      value: formatNumber(overview?.metrics.profileCount),
      meta: "profiles",
    },
    {
      label: "Outbox pending",
      value: formatNumber(outboxTotals.pending),
      meta: "nieprzetworzone",
    },
    {
      label: "Outbox failed",
      value: formatNumber(outboxTotals.failed),
      meta: "z błędem",
    },
  ];

  return (
    <main className={styles.adminShell}>
      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <span aria-hidden="true">K</span>
          <div>
            <strong>Kinxter</strong>
            <small>Admin</small>
          </div>
        </div>

        <nav className={styles.nav} aria-label="Admin">
          <a className={`${styles.navItem} ${styles.active}`} href="/">
            Monitoring
          </a>
          <span className={`${styles.navItem} ${styles.disabled}`}>Moderacja</span>
        </nav>
      </aside>

      <section className={styles.content}>
        <header className={styles.topbar}>
          <div>
            <p className={styles.eyebrow}>Kinxter Admin</p>
            <h1 className={styles.heading}>Monitoring</h1>
          </div>
          <div className={styles.topbarActions}>
            <span>Ostatni odczyt: {formatDateTime(snapshot?.checkedAt)}</span>
            <a href="/api/auth/login">Zaloguj</a>
            <button type="button" onClick={refresh} disabled={isRefreshing}>
              {isRefreshing ? "Odświeżanie" : "Odśwież"}
            </button>
          </div>
        </header>

        <section
          className={`${styles.summaryBand} ${styles[statusDetails.tone]}`}
          aria-live="polite"
        >
          <div>
            <span className={styles.statusPill}>{statusDetails.label}</span>
            <h2>{overview?.service ?? "Kinxter.Api"}</h2>
            <p>{snapshot?.error ?? `Backend zgłosił status ${status}.`}</p>
          </div>
          <dl className={styles.summaryStats}>
            <div>
              <dt>Zależności</dt>
              <dd>
                {dependencyCounts.total === 0
                  ? "-"
                  : `${dependencyCounts.online}/${dependencyCounts.total}`}
              </dd>
            </div>
            <div>
              <dt>Odświeżanie</dt>
              <dd>{refreshIntervalMs / 1000}s</dd>
            </div>
          </dl>
        </section>

        <section className={styles.metricGrid} aria-label="Metryki">
          {metricCards.map((card) => (
            <article className={styles.metricCard} key={card.label}>
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.meta}</small>
            </article>
          ))}
        </section>

        <section className={styles.detailGrid}>
          <section className={styles.panel}>
            <div className={styles.panelHeader}>
              <h2>Zależności</h2>
              <span>{dependencyCounts.total} usług</span>
            </div>
            <div className={styles.table} role="table" aria-label="Zależności systemu">
              <div className={`${styles.tableRow} ${styles.tableHead}`} role="row">
                <span>Nazwa</span>
                <span>Status</span>
                <span>Szczegóły</span>
              </div>
              {(overview?.dependencies ?? []).map((dependency) => (
                <div className={styles.tableRow} role="row" key={dependency.name}>
                  <span>{dependency.name}</span>
                  <StatusBadge status={dependency.status} />
                  <span>{dependency.detail ?? "OK"}</span>
                </div>
              ))}
              {!overview?.dependencies?.length ? (
                <div className={styles.emptyState}>Brak odczytu zależności.</div>
              ) : null}
            </div>
          </section>

          <section className={styles.panel}>
            <div className={styles.panelHeader}>
              <h2>Outbox</h2>
              <span>{outboxTotals.pending} pending</span>
            </div>
            <div className={styles.table} role="table" aria-label="Outbox modułów">
              <div
                className={`${styles.tableRow} ${styles.tableHead} ${styles.outboxRow}`}
                role="row"
              >
                <span>Moduł</span>
                <span>Pending</span>
                <span>Failed</span>
                <span>Najstarszy</span>
              </div>
              {(overview?.outbox ?? []).map((module) => (
                <div
                  className={`${styles.tableRow} ${styles.outboxRow}`}
                  role="row"
                  key={module.moduleName}
                >
                  <span>{module.moduleName}</span>
                  <span>{formatNumber(module.pendingCount)}</span>
                  <span>{formatNumber(module.failedCount)}</span>
                  <span>{formatDateTime(module.oldestPendingCreatedAt)}</span>
                </div>
              ))}
              {!overview?.outbox?.length ? (
                <div className={styles.emptyState}>Brak odczytu outbox.</div>
              ) : null}
            </div>
          </section>
        </section>
      </section>
    </main>
  );
}

function StatusBadge({ status }: { status: MonitoringStatus }) {
  const details = statusCopy[normalizeStatus(status)];

  return (
    <span className={`${styles.badge} ${styles[details.tone]}`}>
      {details.label}
    </span>
  );
}

function normalizeStatus(status: unknown): MonitoringStatus {
  return status === "ok" || status === "degraded" || status === "down"
    ? status
    : "down";
}

function formatNumber(value: number | null | undefined) {
  return typeof value === "number" ? new Intl.NumberFormat("pl-PL").format(value) : "-";
}

function formatLatency(value: number | null | undefined) {
  return typeof value === "number" ? `${value} ms` : "-";
}

function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("pl-PL", {
    dateStyle: "short",
    timeStyle: "medium",
  }).format(date);
}
