import { useParams } from "react-router-dom";
import { useResource } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function ResourceDetailPage() {
  const { resourceId = "" } = useParams();
  const resourceQuery = useResource(resourceId);

  if (resourceQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (resourceQuery.isError) {
    return <ErrorPanel message={resourceQuery.error.message} />;
  }

  if (!resourceQuery.data) {
    return <ErrorPanel message="Resource details are unavailable." />;
  }

  const resource = resourceQuery.data;

  return (
    <div className="stack-xl">
      <PageHeader title="Resource Detail" subtitle="Raw normalized payload from the controller API." />
      <Card title={resource.id} subtitle={`Type: ${resource.kind}`}>
        <pre className="json-view">{JSON.stringify(resource.payload, null, 2)}</pre>
      </Card>
    </div>
  );
}
