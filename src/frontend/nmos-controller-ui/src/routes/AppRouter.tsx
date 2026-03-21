import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "../components/AppLayout";
import { AuditLogPage } from "../pages/AuditLogPage";
import { DashboardPage } from "../pages/DashboardPage";
import { InventoryPage } from "../pages/InventoryPage";
import { PresetsPage } from "../pages/PresetsPage";
import { ReceiversPage } from "../pages/ReceiversPage";
import { RegistryStatusPage } from "../pages/RegistryStatusPage";
import { ResourceDetailPage } from "../pages/ResourceDetailPage";
import { RoutingPage } from "../pages/RoutingPage";
import { SendersPage } from "../pages/SendersPage";
import { SettingsPage } from "../pages/SettingsPage";

export function AppRouter() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/registry" element={<RegistryStatusPage />} />
        <Route path="/inventory" element={<InventoryPage />} />
        <Route path="/senders" element={<SendersPage />} />
        <Route path="/receivers" element={<ReceiversPage />} />
        <Route path="/routing" element={<RoutingPage />} />
        <Route path="/resources/:resourceId" element={<ResourceDetailPage />} />
        <Route path="/presets" element={<PresetsPage />} />
        <Route path="/audit" element={<AuditLogPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
