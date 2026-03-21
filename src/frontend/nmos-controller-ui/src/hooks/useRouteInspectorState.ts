import { useMemo, useState } from "react";
import type { RoutingDestination } from "../api/types";

export function useRouteInspectorState(selectedDestination: RoutingDestination | null) {
  const [isExpanded, setIsExpanded] = useState(true);

  const summary = useMemo(() => {
    if (!selectedDestination) {
      return {
        activeLayers: 0,
        breakawayLayers: 0,
        destinationLabel: null,
      };
    }

    return {
      activeLayers: selectedDestination.routes.filter((route) => route.activeSourceId).length,
      breakawayLayers: selectedDestination.routes.filter((route) => route.isBreakaway).length,
      destinationLabel: selectedDestination.label,
    };
  }, [selectedDestination]);

  return {
    isExpanded,
    setIsExpanded,
    summary,
  };
}
