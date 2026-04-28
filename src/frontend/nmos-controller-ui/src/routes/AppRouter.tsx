import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useRegistry } from "../api/hooks";
import { AppLayout } from "../components/AppLayout";
import { AuditLogPage } from "../pages/AuditLogPage";
import { DashboardPage } from "../pages/DashboardPage";
import { InventoryPage } from "../pages/InventoryPage";
import { RegistryStatusPage } from "../pages/RegistryStatusPage";
import { ResourceDetailPage } from "../pages/ResourceDetailPage";
import { RoutingPage } from "../pages/RoutingPage";
import { SendersReceiversPage } from "../pages/SendersReceiversPage";
import { SettingsPage } from "../pages/SettingsPage";
import { SetupWizardPage } from "../pages/SetupWizardPage";

export function AppRouter() {
  const location = useLocation();
  const registryQuery = useRegistry();
  const isWizardRoute = location.pathname === "/setup-wizard";
  const wizardBypassEnabled = window.localStorage.getItem("nmos_controller_wizard_bypass") === "true";

  if (!isWizardRoute && !wizardBypassEnabled && registryQuery.data && !registryQuery.data.initialSetupCompleted) {
    return <Navigate to="/setup-wizard" replace />;
  }

  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/registry" element={<RegistryStatusPage />} />
        <Route path="/inventory" element={<InventoryPage />} />
        <Route path="/senders-receivers" element={<SendersReceiversPage />} />
        <Route path="/senders" element={<Navigate to="/senders-receivers?tab=senders" replace />} />
        <Route path="/receivers" element={<Navigate to="/senders-receivers" replace />} />
        <Route path="/routing" element={<RoutingPage />} />
        <Route path="/resources/:resourceId" element={<ResourceDetailPage />} />
        <Route path="/audit" element={<AuditLogPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/setup-wizard" element={<SetupWizardPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
