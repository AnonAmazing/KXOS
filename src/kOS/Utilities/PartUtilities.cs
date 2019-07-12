using kOS.Safe.Encapsulation;
using UnityEngine;
using System;

namespace kOS.Utilities
{
    public static class PartUtilities
    {
        public static float CalculateCurrentMass(this Part part)
        {
            // rb mass is one physics tick behind.  Use part.GetPhysicslessChildMass() if the
            // delay becomes a significant problem, but this should be good 99% of the time.
            // Default to zero if the rigid body is not yet updated, or the part is physics-less
            return part.HasPhysics() && part.rb != null ? part.rb.mass : 0;
        }

        public static bool HasPhysics(this Part part)
        {
            switch (part.physicalSignificance)
            {
                case Part.PhysicalSignificance.FULL:
                    return true;
                case Part.PhysicalSignificance.NONE:
                    return false;
                default:
                    throw new NotImplementedException("Unknown Part physics type: " + part.physicalSignificance);
            }
        }

        public static float GetDryMass(this Part part)
        {
            // this will technically have an oportunity to return a negative wet mass
            // if the part is physics-less, but that option is intended for small part
            // to help with the physics calculation, not tanks of fuel.
            return part.CalculateCurrentMass() - part.resourceMass;
        }

        public static float GetWetMass(this Part part)
        {
            // See the note above regarding negative dry mass, the wet mass may net to
            // zero in the same case.  Again, highly unlikely.
            float mass = part.GetDryMass();

            for (int index = 0; index < part.Resources.Count; ++index)
            {
                PartResource partResource = part.Resources[index];
                mass += (float)partResource.maxAmount * partResource.info.density;
            }

            return mass;
        }

        /// <summary>
        /// Returns the part's bounds in a bounding box that is oriented
        /// to align with the part's PART:FACING orientation.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static Bounds KosGetPartBounds(this Part part)
        {
            // Our normal facings use Z for forward, but parts use Y for forward:
            Quaternion rotateZToY = Quaternion.FromToRotation(Vector3.forward, Vector3.up);

            Bounds unionBounds = new Bounds();

            MeshFilter[] meshes = part.GetComponentsInChildren<MeshFilter>();
            for (int meshIndex = 0; meshIndex < meshes.Length; ++meshIndex)
            {
                MeshFilter mesh = meshes[meshIndex];
                Bounds bounds = mesh.mesh.bounds;

                // Part meshes could be scaled as well as rotated (the mesh might describe a
                // part that's 1 meter wide while the real part is 2 meters wide, and has a scale of 2x
                // encoded into its transform to do this).  Because of this, the only really
                // reliable way to get the real shape is to let the transform do its work on all 6 corners
                // of the bounding box, transforming them with the mesh's transform, then back-calculating
                // from that world-space result back into the part's own reference frame to get the bounds
                // relative to the part.
                Console.WriteLine("eraseme: starting a mesh work.");
                Vector3 center = bounds.center;

                // This triple-nested loop visits all 8 corners of the box:
                for (int signX = -1; signX <= 1; signX += 2)
                    for (int signY = -1; signY <= 1; signY += 2)
                        for (int signZ = -1; signZ <= 1; signZ += 2)
                        {
                            Vector3 corner = center + new Vector3(signX * bounds.extents.x, signY * bounds.extents.y, signZ * bounds.extents.z);
                            Console.WriteLine("eraseme:     corner = " + corner);
                            Vector3 worldCorner = mesh.transform.TransformPoint(corner);
                            Console.WriteLine("eraseme:worldCorner = " + worldCorner);
                            Vector3 partCorner = rotateZToY * part.transform.InverseTransformPoint(worldCorner);
                            Console.WriteLine("eraseme: partCorner = " + partCorner);

                            // Stretches the bounds we're making (which started at size zero in all axes),
                            // just big enough to include this corner:
                            unionBounds.Encapsulate(partCorner);
                        }
            }
            Console.WriteLine("eraseme: unionBounds.min x=" + unionBounds.min.x + " y=" + unionBounds.min.y + " z=" + unionBounds.min.z);
            Console.WriteLine("eraseme: unionBounds.max x=" + unionBounds.max.x + " y=" + unionBounds.max.y + " z=" + unionBounds.max.z);
            return unionBounds;
        }

    }
}