'use client';

import { usePermissions } from '@/hooks/use-auth';

/**
 * Hides UI the user cannot act on. This is convenience only - the API rejects
 * unauthorized calls regardless of what the UI shows.
 */
export function PermissionGate({
  permission, children, fallback = null,
}: { permission: string; children: React.ReactNode; fallback?: React.ReactNode }) {
  const { can } = usePermissions();
  return <>{can(permission) ? children : fallback}</>;
}
