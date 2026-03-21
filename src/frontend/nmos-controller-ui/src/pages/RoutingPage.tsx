import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { RouteInspectorPanel } from "../components/routing/RouteInspectorPanel";
import { RouterTab } from "../components/routing/RouterTab";
import { RoutingTabs, type RoutingTabId } from "../components/routing/RoutingTabs";
import { TopologyTab } from "../components/routing/TopologyTab";
import { XYTab } from "../components/routing/XYTab";
import { useRouteInspectorState } from "../hooks/useRouteInspectorState";
import { useRoutingState } from "../hooks/useRoutingState";
import { useState } from "react";

export function RoutingPage() {
  const routing = useRoutingState();
  const [activeTab, setActiveTab] = useState<RoutingTabId>("router");
  const inspector = useRouteInspectorState(routing.selectedDestination);

  if (routing.isLoading || !routing.matrix || !routing.topology) {
    return <LoadingPanel />;
  }

  if (routing.error) {
    return <ErrorPanel message={routing.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader
        title="Broadcast Routing"
        subtitle="Split operational routing, engineering topology, and XY switching into focused workspaces with shared synchronized state."
        actions={<RoutingTabs activeTab={activeTab} onChange={setActiveTab} />}
      />

      {activeTab === "router" ? (
        <RouterTab
          sources={routing.sources}
          destinations={routing.destinations}
          filteredSources={routing.filteredSources}
          crosspoints={routing.matrix.crosspoints}
          selectedDestinationId={routing.selectedDestinationId}
          selectedSourceId={routing.selectedSourceId}
          previewDestinationId={routing.preview.destinationId}
          previewLayers={routing.preview.layers}
          enabledLayers={routing.enabledLayers}
          autoTake={routing.autoTake}
          hasPreview={routing.hasPreview}
          isBusy={routing.isMutating}
          sourceSearch={routing.sourceSearch}
          destinationSearch={routing.destinationSearch}
          onSourceSearchChange={routing.setSourceSearch}
          onDestinationSearchChange={routing.setDestinationSearch}
          onToggleLayer={routing.toggleLayer}
          onSourceSelect={routing.selectSource}
          onDestinationSelect={routing.selectDestination}
          onCrosspointSelect={routing.selectMatrixCrosspoint}
          onToggleAutoTake={routing.setAutoTake}
          onTake={() => void routing.takePreview()}
          onClear={routing.clearPreview}
          onDisconnect={() => void routing.disconnectSelected()}
        />
      ) : null}

      {activeTab === "topology" ? (
        <TopologyTab
          topology={routing.topology}
          previewEdges={routing.previewEdges}
          selectedDestinationId={routing.selectedDestinationId}
          selectedSourceId={routing.selectedSourceId}
          selectedDestinationLabel={routing.selectedDestination?.label ?? null}
          inspectorRoutes={routing.inspectorRoutes}
          visibleLayers={routing.visibleTopologyLayers}
          showInfrastructure={routing.showInfrastructure}
          showOnlySelectedRoute={routing.showOnlySelectedRoute}
          inspectorExpanded={inspector.isExpanded}
          onToggleLayer={routing.toggleTopologyLayer}
          onToggleInfrastructure={routing.setShowInfrastructure}
          onToggleOnlySelectedRoute={routing.setShowOnlySelectedRoute}
          onToggleInspector={() => inspector.setIsExpanded(!inspector.isExpanded)}
          onRouteSelect={routing.syncFromGraphEdge}
        />
      ) : null}

      {activeTab === "xy" ? (
        <XYTab
          destinations={routing.destinations}
          sources={routing.filteredSources}
          selectedDestinationId={routing.selectedDestinationId}
          selectedSourceId={routing.selectedSourceId}
          enabledLayers={routing.enabledLayers}
          autoTake={routing.autoTake}
          hasPreview={routing.hasPreview}
          isBusy={routing.isMutating}
          onDestinationSelect={routing.selectDestination}
          onSourceSelect={routing.selectSource}
          onToggleLayer={routing.toggleLayer}
          onToggleAutoTake={routing.setAutoTake}
          onTake={() => void routing.takePreview()}
          onClear={routing.clearPreview}
          onDisconnect={() => void routing.disconnectSelected()}
        />
      ) : null}

      {activeTab === "inspector" ? (
        <div className="routing-tab-layout is-inspector">
          <RouteInspectorPanel
            selectedDestinationLabel={routing.selectedDestination?.label ?? null}
            inspectorRoutes={routing.inspectorRoutes}
          />
        </div>
      ) : null}
    </div>
  );
}
