"use client";

import { useEffect } from "react";

const panelIds = new Set(["start", "join", "safety"]);

export function PanelHashNavigation() {
  useEffect(() => {
    let scrollFrame: number | null = null;

    function revealPanel() {
      const hash = window.location.hash.slice(1);
      const panelId = panelIds.has(hash) ? hash : "start";
      const panel = document.getElementById(panelId);
      const viewport = document.querySelector<HTMLElement>(".horizontalViewport");

      if (!panel || !viewport) {
        return;
      }

      const behavior = window.matchMedia("(prefers-reduced-motion: reduce)")
        .matches
        ? "auto"
        : "smooth";

      if (window.matchMedia("(max-width: 760px)").matches) {
        if (hash) {
          panel.scrollIntoView({ behavior, block: "start" });
        }

        return;
      }

      viewport.scrollTo({
        behavior,
        left: panel.offsetLeft,
      });
    }

    function syncHeaderState() {
      const header = document.querySelector<HTMLElement>(".siteHeader");
      header?.classList.toggle("siteHeaderScrolled", window.scrollY > 32);
      scrollFrame = null;
    }

    function handleScroll() {
      if (scrollFrame === null) {
        scrollFrame = window.requestAnimationFrame(syncHeaderState);
      }
    }

    const frame = window.requestAnimationFrame(revealPanel);
    syncHeaderState();
    window.addEventListener("hashchange", revealPanel);
    window.addEventListener("scroll", handleScroll, { passive: true });

    return () => {
      window.cancelAnimationFrame(frame);
      if (scrollFrame !== null) {
        window.cancelAnimationFrame(scrollFrame);
      }
      window.removeEventListener("hashchange", revealPanel);
      window.removeEventListener("scroll", handleScroll);
    };
  }, []);

  return null;
}
