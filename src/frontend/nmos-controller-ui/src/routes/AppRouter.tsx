import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "../components/AppLayout";
import { AuditLogPage } from "../pages/AuditLogPage";
import { DashboardPage } from "../pages/DashboardPage";
import { InventoryPage } from "../pages/InventoryPage";
import { RegistryStatusPage } from "../pages/RegistryStatusPage";
import { ResourceDetailPage } from "../pages/ResourceDetailPage";
import { RoutingPage } from "../pages/RoutingPage";
import { SendersReceiversPage } from "../pages/SendersReceiversPage";
import { SettingsPage } from "../pages/SettingsPage";

export function AppRouter() {
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
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
