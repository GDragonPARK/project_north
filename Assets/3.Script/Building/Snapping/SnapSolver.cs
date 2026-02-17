using UnityEngine;
using System.Collections.Generic;

public static class SnapSolver
{
    private static Collider[] s_colliderCache = new Collider[64];

    public struct SnapResult
    {
        public Vector3 position;
        public Quaternion rotation;
        public SnapPoint worldSnap;
        public SnapPoint previewSnap;
    }

    public static bool TrySolveSnap(
        Vector3 desiredPos,
        Quaternion desiredRot,
        BuildingPiece previewPiece,
        float searchRadius,
        LayerMask snapPointMask,
        out Pose solvedPose,
        out SnapPoint matchedWorldPoint,
        out SnapPoint matchedPreviewPoint)
    {
        solvedPose = new Pose(desiredPos, desiredRot);
        matchedWorldPoint = null;
        matchedPreviewPoint = null;

        if (!previewPiece) return false;

        // 1. Gather Candidates
        int count = Physics.OverlapSphereNonAlloc(desiredPos, searchRadius, s_colliderCache, snapPointMask, QueryTriggerInteraction.Collide);
        if (count == 0) return false;

        SnapResult bestResult = default;
        float bestScore = float.MaxValue; 
        bool foundMatch = false;

        // Cache preview transform data to avoid repeated native calls
        Vector3 previewScale = previewPiece.transform.lossyScale; 
        // Note: scale is usually (1,1,1). If not, math gets complex. Assuming (1,1,1).

        // 2. Iterate World SnapPoints
        for (int i = 0; i < count; i++)
        {
            SnapPoint worldSp = s_colliderCache[i].GetComponent<SnapPoint>();
            if (worldSp == null || worldSp.isOccupied) continue;

            // 3. Iterate Preview Sockets
            foreach (var previewSp in previewPiece.SnapPoints)
            {
                // Compatibility Check
                 if (!worldSp.CanConnectTo(previewSp)) continue;

                // 4. Calculate Alignment
                // Target: We want the PREVIEW socket's forward to be OPPOSITE to WORLD socket's forward.
                // previewSocket.forward = -worldSocket.forward
                // previewSocket.up = worldSocket.up (or maintain relative up? Usually Wall-Wall needs alignment)
                
                // Construct target rotation for the connection point in world space
                Quaternion targetSocketWorldRot = Quaternion.LookRotation(-worldSp.transform.forward, worldSp.transform.up);
                
                // We need to find the Root Rotation (R_root) such that:
                // R_root * R_localSocket = targetSocketWorldRot
                // => R_root = targetSocketWorldRot * Inverse(R_localSocket)
                
                // Calculate local rotation of the socket relative to the piece root
                Quaternion socketLocalRot = Quaternion.Inverse(previewPiece.transform.rotation) * previewSp.transform.rotation;
                
                Quaternion solvedRootRot = targetSocketWorldRot * Quaternion.Inverse(socketLocalRot);
                
                // Now Position:
                // P_root + R_root * P_localSocket = P_targetSocket
                // => P_root = P_targetSocket - R_root * P_localSocket
                
                Vector3 socketLocalPos = previewPiece.transform.InverseTransformPoint(previewSp.transform.position);
                
                // IMPORTANT: If parent has scale, InverseTransformPoint includes it. 
                // When we rotate (R_root * socketLocalPos), we must ensure socketLocalPos is the "scaled" offset if we apply it to a scaled root.
                // But generally ghosts are scale (1,1,1). 
                Vector3 solvedRootPos = worldSp.transform.position - (solvedRootRot * socketLocalPos);

                // 5. Scoring
                // Score based on how far the snapped root is from the desired root position
                float distSq = (solvedRootPos - desiredPos).sqrMagnitude;
                
                // Distance Threshold (0.4m radius = 0.16 sqr)
                if (distSq > (0.4f * 0.4f)) continue; 

                // Additional Score: Angle difference between desired rotation and solved rotation?
                // If user rotated "near" the snap, prefer that?
                // float angleDiff = Quaternion.Angle(desiredRot, solvedRootRot);
                // float finalScore = distSq + (angleDiff * 0.0001f); // Weight distance heavily
                
                if (distSq < bestScore)
                {
                    bestScore = distSq;
                    bestResult.position = solvedRootPos;
                    bestResult.rotation = solvedRootRot;
                    bestResult.worldSnap = worldSp;
                    bestResult.previewSnap = previewSp;
                    foundMatch = true;
                }
            }
        }

        if (foundMatch)
        {
            solvedPose = new Pose(bestResult.position, bestResult.rotation);
            matchedWorldPoint = bestResult.worldSnap;
            matchedPreviewPoint = bestResult.previewSnap;
            return true;
        }

        return false;
    }
}
