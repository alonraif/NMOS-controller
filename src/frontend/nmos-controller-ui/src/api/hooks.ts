import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import { queryKeys } from "./queryKeys";
import type {
  ConnectReceiverPayload,
  DisconnectReceiverPayload,
  ExecutePresetPayload,
  RouteValidationPayload,
  UpdateRegistryPayload,
  UpsertPresetPayload,
} from "./types";

export function useTopology(refresh = false) {
  return useQuery({
    queryKey: queryKeys.topology(refresh),
    queryFn: () => api.getTopology(refresh),
  });
}

export function useSenders(refresh = false) {
  return useQuery({
    queryKey: queryKeys.senders(refresh),
    queryFn: () => api.getSenders(refresh),
  });
}

export function useReceivers(refresh = false) {
  return useQuery({
    queryKey: queryKeys.receivers(refresh),
    queryFn: () => api.getReceivers(refresh),
  });
}

export function useResource(resourceId: string) {
  return useQuery({
    queryKey: queryKeys.resource(resourceId),
    queryFn: () => api.getResource(resourceId),
    enabled: Boolean(resourceId),
  });
}

export function useRegistry() {
  return useQuery({
    queryKey: queryKeys.registry(),
    queryFn: () => api.getRegistry(),
  });
}

export function usePresets() {
  return useQuery({
    queryKey: queryKeys.presets(),
    queryFn: () => api.getPresets(),
  });
}

export function useAudit(limit: number) {
  return useQuery({
    queryKey: queryKeys.audit(limit),
    queryFn: () => api.getAudit(limit),
  });
}

export function useUpdateRegistry() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateRegistryPayload) => api.updateRegistry(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.registry() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.topology() });
    },
  });
}

export function useValidateRoute() {
  return useMutation({
    mutationFn: (payload: RouteValidationPayload) => api.validateRoute(payload),
  });
}

export function useConnectReceiver() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ receiverId, payload }: { receiverId: string; payload: ConnectReceiverPayload }) =>
      api.connectReceiver(receiverId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.topology() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.senders() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.receivers() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit(100) });
    },
  });
}

export function useDisconnectReceiver() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ receiverId, payload }: { receiverId: string; payload: DisconnectReceiverPayload }) =>
      api.disconnectReceiver(receiverId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.topology() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.senders() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.receivers() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit(100) });
    },
  });
}

export function useSavePreset() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpsertPresetPayload) => api.savePreset(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.presets() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit(100) });
    },
  });
}

export function useDeletePreset() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (presetId: string) => api.deletePreset(presetId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.presets() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit(100) });
    },
  });
}

export function useExecutePreset() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ presetId, payload }: { presetId: string; payload: ExecutePresetPayload }) =>
      api.executePreset(presetId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.presets() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.topology() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.senders() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.receivers() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit(100) });
    },
  });
}
