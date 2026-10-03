using System;
using System.Collections.Generic;

namespace HashiraChronicles
{
    /// <summary>
    /// Melts a group of overlapping character parts (ellipsoids, lathed limbs and shells, rounded boxes, cones) into
    /// one continuous, smooth surface — the "sculpted" look of a hand-modelled game character instead of a figure
    /// assembled from separate shapes. Each part becomes a signed distance field; the fields are joined with a
    /// smooth minimum (so every seam becomes a soft fillet), sampled on a grid, and turned back into a mesh with
    /// surface nets. Normals come from the field's gradient, so the result shades like a polished sculpt.
    ///
    /// Pure C# with no engine types, so it runs (and is tested) outside Unity. <see cref="CharacterVisual"/> feeds
    /// it the parts and turns the result into a skinned mesh.
    /// </summary>
    public sealed class SdfMesher
    {
        public enum Shape { Sphere, Lathe, RoundBox }

        /// <summary>Corner radius of <c>MeshFactory.RoundedCube</c> (unit cube).</summary>
        public const float RoundBoxRadius = 0.14f;

        public sealed class Prim
        {
            public Shape shape;
            /// <summary>Frame space → the part's own mesh space, row-major 3×4 (rotation, scale and offset).</summary>
            public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23;
            /// <summary>Lathe: closed (r, y) polygon; the closing edges on the axis are not surfaces.</summary>
            public float[] polyR, polyY;
            public bool[] skipEdge;
            /// <summary>Colour slot and owning joint (indices chosen by the caller).</summary>
            public int slot, bone;
            /// <summary>Frame-space bounding box.</summary>
            public float minX, minY, minZ, maxX, maxY, maxZ;

            /// <summary>Lathe polygon from a mesh profile (r, y), bottom to top, closed through the axis.</summary>
            public void SetProfile(float[] r, float[] y)
            {
                var pr = new List<float>();
                var py = new List<float>();
                if (r[0] > 1e-4f) { pr.Add(0f); py.Add(y[0]); }
                for (int i = 0; i < r.Length; i++) { pr.Add(Math.Max(0f, r[i])); py.Add(y[i]); }
                if (r[r.Length - 1] > 1e-4f) { pr.Add(0f); py.Add(y[y.Length - 1]); }
                polyR = pr.ToArray();
                polyY = py.ToArray();
                int n = polyR.Length;
                skipEdge = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    skipEdge[i] = polyR[i] < 1e-4f && polyR[j] < 1e-4f;
                }
            }
        }

        public sealed class Result
        {
            public int count;
            public float[] pos, nrm;
            public int[] tris;
            public int triCount;
        }

        // ------------------------------------------------------------------ Distance

        /// <summary>Signed distance from a frame-space point to a part (exact on the surface, first-order nearby).</summary>
        public static float Eval(Prim p, float x, float y, float z)
        {
            float qx = p.m00 * x + p.m01 * y + p.m02 * z + p.m03;
            float qy = p.m10 * x + p.m11 * y + p.m12 * z + p.m13;
            float qz = p.m20 * x + p.m21 * y + p.m22 * z + p.m23;
            float d, gx, gy, gz;
            switch (p.shape)
            {
                case Shape.Sphere:
                {
                    float l = (float)Math.Sqrt(qx * qx + qy * qy + qz * qz);
                    d = l - 0.5f;
                    if (l > 1e-7f) { gx = qx / l; gy = qy / l; gz = qz / l; } else { gx = 0f; gy = 1f; gz = 0f; }
                    break;
                }
                case Shape.RoundBox:
                {
                    const float r = RoundBoxRadius, b = 0.5f - RoundBoxRadius;
                    float ax = Math.Abs(qx) - b, ay = Math.Abs(qy) - b, az = Math.Abs(qz) - b;
                    float ox = Math.Max(ax, 0f), oy = Math.Max(ay, 0f), oz = Math.Max(az, 0f);
                    float lo = (float)Math.Sqrt(ox * ox + oy * oy + oz * oz);
                    float mx = Math.Max(ax, Math.Max(ay, az));
                    d = lo + Math.Min(mx, 0f) - r;
                    if (lo > 1e-7f) { gx = ox / lo * Math.Sign(qx); gy = oy / lo * Math.Sign(qy); gz = oz / lo * Math.Sign(qz); }
                    else if (mx == ax) { gx = qx < 0f ? -1f : 1f; gy = 0f; gz = 0f; }
                    else if (mx == ay) { gx = 0f; gy = qy < 0f ? -1f : 1f; gz = 0f; }
                    else { gx = 0f; gy = 0f; gz = qz < 0f ? -1f : 1f; }
                    break;
                }
                default:
                {
                    float rho = (float)Math.Sqrt(qx * qx + qz * qz);
                    float g2r, g2y;
                    d = Lathe2D(p, rho, qy, out g2r, out g2y);
                    if (rho > 1e-7f) { gx = g2r * qx / rho; gz = g2r * qz / rho; } else { gx = g2r; gz = 0f; }
                    gy = g2y;
                    break;
                }
            }
            // Back to frame units: divide by how fast the part-space distance grows along frame space.
            float fx = p.m00 * gx + p.m10 * gy + p.m20 * gz;
            float fy = p.m01 * gx + p.m11 * gy + p.m21 * gz;
            float fz = p.m02 * gx + p.m12 * gy + p.m22 * gz;
            float len = (float)Math.Sqrt(fx * fx + fy * fy + fz * fz);
            return d / Math.Max(len, 1e-6f);
        }

        /// <summary>Signed distance to the lathe's (r, y) polygon and its outward unit gradient.</summary>
        static float Lathe2D(Prim p, float r, float y, out float gr, out float gy)
        {
            var pr = p.polyR;
            var py = p.polyY;
            int n = pr.Length;
            float best = float.MaxValue, cr = 0f, cy = 0f, nr = 1f, ny = 0f;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                // Crossing test (every edge, including the axis).
                if ((py[i] > y) != (py[j] > y) && r < (pr[j] - pr[i]) * (y - py[i]) / (py[j] - py[i]) + pr[i]) inside = !inside;
                if (p.skipEdge[j]) continue;
                // Edge j → i.
                float ex = pr[i] - pr[j], ey = py[i] - py[j];
                float wx = r - pr[j], wy = y - py[j];
                float ee = ex * ex + ey * ey;
                float t = ee > 1e-12f ? Math.Max(0f, Math.Min(1f, (wx * ex + wy * ey) / ee)) : 0f;
                float px = pr[j] + ex * t, pyy = py[j] + ey * t;
                float dx = r - px, dy = y - pyy;
                float dd = dx * dx + dy * dy;
                if (dd < best)
                {
                    best = dd; cr = px; cy = pyy;
                    float el = (float)Math.Sqrt(ee);
                    if (el > 1e-9f) { nr = ey / el; ny = -ex / el; }
                }
            }
            float dist = (float)Math.Sqrt(best);
            float sgn = inside ? -1f : 1f;
            // Exactly on the surface the edge's own normal stands in for the gradient.
            if (dist > 1e-7f) { gr = (r - cr) / dist * sgn; gy = (y - cy) / dist * sgn; }
            else { gr = nr; gy = ny; }
            return dist * sgn;
        }

        /// <summary>Polynomial smooth minimum: the union of two fields with a soft fillet of width k.</summary>
        static float SMin(float a, float b, float k)
        {
            if (a > 1e8f) return b;
            float h = Math.Max(k - Math.Abs(a - b), 0f) / k;
            return Math.Min(a, b) - h * h * k * 0.25f;
        }

        // ------------------------------------------------------------------ Meshing

        int nx, ny, nz;
        float ox, oy, oz, h;
        float[] F;

        /// <summary>
        /// Builds the melted surface. h = grid spacing, k = fillet width (both frame units). The grid grows coarser
        /// if the parts span more than maxCells samples, so a huge costume can't stall the game.
        /// </summary>
        public Result Build(List<Prim> prims, float cell, float k, int maxCells = 900000)
        {
            if (prims == null || prims.Count == 0) return null;
            float margin = k + cell * 3f;
            float x0 = float.MaxValue, y0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, z1 = float.MinValue;
            foreach (var p in prims)
            {
                x0 = Math.Min(x0, p.minX); y0 = Math.Min(y0, p.minY); z0 = Math.Min(z0, p.minZ);
                x1 = Math.Max(x1, p.maxX); y1 = Math.Max(y1, p.maxY); z1 = Math.Max(z1, p.maxZ);
            }
            h = cell;
            for (int guard = 0; guard < 8; guard++)
            {
                margin = k + h * 3f;
                nx = (int)Math.Ceiling((x1 - x0 + 2f * margin) / h) + 1;
                ny = (int)Math.Ceiling((y1 - y0 + 2f * margin) / h) + 1;
                nz = (int)Math.Ceiling((z1 - z0 + 2f * margin) / h) + 1;
                long total = (long)nx * ny * nz;
                if (total <= maxCells) break;
                h *= (float)Math.Pow((double)total / maxCells, 1.0 / 3.0) * 1.02f;
            }
            ox = x0 - margin; oy = y0 - margin; oz = z0 - margin;
            F = new float[nx * ny * nz];
            for (int i = 0; i < F.Length; i++) F[i] = 1e9f;

            // Each part is sampled in 4×4×4 blocks: a block far outside the part is skipped, a block deep inside is
            // filled with one value, and only blocks near its surface are sampled voxel by voxel.
            const int B = 4;
            // Exact values are needed within the fillet width of a surface plus the gradient stencil (k + 3h).
            float band = k + h * 3f;
            foreach (var p in prims)
            {
                int i0 = Math.Max(0, (int)Math.Floor((p.minX - margin - ox) / h)), i1 = Math.Min(nx - 1, (int)Math.Ceiling((p.maxX + margin - ox) / h));
                int j0 = Math.Max(0, (int)Math.Floor((p.minY - margin - oy) / h)), j1 = Math.Min(ny - 1, (int)Math.Ceiling((p.maxY + margin - oy) / h));
                int k0 = Math.Max(0, (int)Math.Floor((p.minZ - margin - oz) / h)), k1 = Math.Min(nz - 1, (int)Math.Ceiling((p.maxZ + margin - oz) / h));
                for (int bk = k0; bk <= k1; bk += B)
                    for (int bj = j0; bj <= j1; bj += B)
                        for (int bi = i0; bi <= i1; bi += B)
                        {
                            int ei = Math.Min(bi + B - 1, i1), ej = Math.Min(bj + B - 1, j1), ek = Math.Min(bk + B - 1, k1);
                            float cxp = ox + (bi + ei) * 0.5f * h, cyp = oy + (bj + ej) * 0.5f * h, czp = oz + (bk + ek) * 0.5f * h;
                            float half = 0.5f * h * (float)Math.Sqrt((ei - bi) * (ei - bi) + (ej - bj) * (ej - bj) + (ek - bk) * (ek - bk));
                            float dc = Eval(p, cxp, cyp, czp);
                            float safe = half * 1.3f + band;
                            if (dc > safe) continue;
                            if (dc < -safe)
                            {
                                for (int kk = bk; kk <= ek; kk++)
                                    for (int jj = bj; jj <= ej; jj++)
                                    {
                                        int row = (kk * ny + jj) * nx;
                                        for (int ii = bi; ii <= ei; ii++) if (dc < F[row + ii]) F[row + ii] = dc;
                                    }
                                continue;
                            }
                            for (int kk = bk; kk <= ek; kk++)
                            {
                                float z = oz + kk * h;
                                for (int jj = bj; jj <= ej; jj++)
                                {
                                    float y = oy + jj * h;
                                    int row = (kk * ny + jj) * nx;
                                    for (int ii = bi; ii <= ei; ii++)
                                    {
                                        int idx = row + ii;
                                        F[idx] = SMin(F[idx], Eval(p, ox + ii * h, y, z), k);
                                    }
                                }
                            }
                        }
            }
            return Extract();
        }

        static readonly int[] cornerX = { 0, 1, 0, 1, 0, 1, 0, 1 };
        static readonly int[] cornerY = { 0, 0, 1, 1, 0, 0, 1, 1 };
        static readonly int[] cornerZ = { 0, 0, 0, 0, 1, 1, 1, 1 };
        static readonly int[] edgeA = { 0, 2, 4, 6, 0, 1, 4, 5, 0, 1, 2, 3 };
        static readonly int[] edgeB = { 1, 3, 5, 7, 2, 3, 6, 7, 4, 5, 6, 7 };

        int G(int i, int j, int k) { return (k * ny + j) * nx + i; }

        Result Extract()
        {
            int cx = nx - 1, cy = ny - 1, cz = nz - 1;
            var cellVert = new int[cx * cy * cz];
            var pos = new List<float>();
            var cv = new float[8];
            int count = 0;
            for (int k = 0; k < cz; k++)
                for (int j = 0; j < cy; j++)
                    for (int i = 0; i < cx; i++)
                    {
                        int ci = (k * cy + j) * cx + i;
                        cellVert[ci] = -1;
                        int mask = 0;
                        bool far = false;
                        for (int c = 0; c < 8; c++)
                        {
                            float v = F[G(i + cornerX[c], j + cornerY[c], k + cornerZ[c])];
                            if (v > 1e8f) far = true;
                            cv[c] = v;
                            if (v < 0f) mask |= 1 << c;
                        }
                        if (mask == 0 || mask == 255 || far) continue;
                        float sx = 0f, sy = 0f, sz = 0f;
                        int n = 0;
                        for (int e = 0; e < 12; e++)
                        {
                            int a = edgeA[e], b = edgeB[e];
                            float va = cv[a], vb = cv[b];
                            if ((va < 0f) == (vb < 0f)) continue;
                            float t = va / (va - vb);
                            sx += cornerX[a] + (cornerX[b] - cornerX[a]) * t;
                            sy += cornerY[a] + (cornerY[b] - cornerY[a]) * t;
                            sz += cornerZ[a] + (cornerZ[b] - cornerZ[a]) * t;
                            n++;
                        }
                        cellVert[ci] = count++;
                        pos.Add(ox + (i + sx / n) * h);
                        pos.Add(oy + (j + sy / n) * h);
                        pos.Add(oz + (k + sz / n) * h);
                    }
            if (count < 4) return null;

            var tris = new List<int>();
            // Every grid edge crossing the surface gives one quad joining the four cells around it.
            for (int k = 1; k < nz - 1; k++)
                for (int j = 1; j < ny - 1; j++)
                    for (int i = 0; i < nx - 1; i++)
                    {
                        float a = F[G(i, j, k)], b = F[G(i + 1, j, k)];
                        if ((a < 0f) == (b < 0f) || a > 1e8f || b > 1e8f) continue;
                        Quad(tris, pos, cellVert, Cell(i, j - 1, k - 1), Cell(i, j, k - 1), Cell(i, j, k), Cell(i, j - 1, k), a < 0f);
                    }
            for (int k = 1; k < nz - 1; k++)
                for (int j = 0; j < ny - 1; j++)
                    for (int i = 1; i < nx - 1; i++)
                    {
                        float a = F[G(i, j, k)], b = F[G(i, j + 1, k)];
                        if ((a < 0f) == (b < 0f) || a > 1e8f || b > 1e8f) continue;
                        Quad(tris, pos, cellVert, Cell(i - 1, j, k - 1), Cell(i - 1, j, k), Cell(i, j, k), Cell(i, j, k - 1), a < 0f);
                    }
            for (int k = 0; k < nz - 1; k++)
                for (int j = 1; j < ny - 1; j++)
                    for (int i = 1; i < nx - 1; i++)
                    {
                        float a = F[G(i, j, k)], b = F[G(i, j, k + 1)];
                        if ((a < 0f) == (b < 0f) || a > 1e8f || b > 1e8f) continue;
                        Quad(tris, pos, cellVert, Cell(i - 1, j - 1, k), Cell(i, j - 1, k), Cell(i, j, k), Cell(i - 1, j, k), a < 0f);
                    }

            var res = new Result { count = count, pos = pos.ToArray(), tris = tris.ToArray(), triCount = tris.Count / 3 };
            res.nrm = new float[count * 3];
            // Settle every vertex onto the smooth surface, then take its normal from the field's gradient.
            for (int v = 0; v < count; v++)
            {
                float x = res.pos[v * 3], y = res.pos[v * 3 + 1], z = res.pos[v * 3 + 2];
                float gx, gy, gz;
                for (int it = 0; it < 2; it++)
                {
                    float f = SampleFast(x, y, z, out gx, out gy, out gz);
                    float gg = gx * gx + gy * gy + gz * gz;
                    if (gg < 1e-10f) break;
                    float step = f / gg;
                    // Never let a vertex wander more than half a cell.
                    float lim = h * 0.5f / (float)Math.Sqrt(gg);
                    if (step > lim) step = lim; else if (step < -lim) step = -lim;
                    x -= gx * step; y -= gy * step; z -= gz * step;
                }
                Sample(x, y, z, out gx, out gy, out gz);
                float gl = (float)Math.Sqrt(gx * gx + gy * gy + gz * gz);
                if (gl < 1e-9f) { gx = 0f; gy = 1f; gz = 0f; gl = 1f; }
                res.pos[v * 3] = x; res.pos[v * 3 + 1] = y; res.pos[v * 3 + 2] = z;
                res.nrm[v * 3] = gx / gl; res.nrm[v * 3 + 1] = gy / gl; res.nrm[v * 3 + 2] = gz / gl;
            }
            return res;
        }

        int Cell(int i, int j, int k) { return (k * (ny - 1) + j) * (nx - 1) + i; }

        static void Quad(List<int> tris, List<float> pos, int[] cellVert, int c0, int c1, int c2, int c3, bool insideFirst)
        {
            int v0 = cellVert[c0], v1 = cellVert[c1], v2 = cellVert[c2], v3 = cellVert[c3];
            if (v0 < 0 || v1 < 0 || v2 < 0 || v3 < 0) return;
            if (!insideFirst) { int t = v1; v1 = v3; v3 = t; }
            // Split along the shorter diagonal.
            if (Dist2(pos, v0, v2) <= Dist2(pos, v1, v3))
            {
                tris.Add(v0); tris.Add(v1); tris.Add(v2);
                tris.Add(v0); tris.Add(v2); tris.Add(v3);
            }
            else
            {
                tris.Add(v0); tris.Add(v1); tris.Add(v3);
                tris.Add(v1); tris.Add(v2); tris.Add(v3);
            }
        }

        static float Dist2(List<float> p, int a, int b)
        {
            float dx = p[a * 3] - p[b * 3], dy = p[a * 3 + 1] - p[b * 3 + 1], dz = p[a * 3 + 2] - p[b * 3 + 2];
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>Trilinear field value and gradient (central differences) at a point.</summary>
        float Sample(float x, float y, float z, out float gx, out float gy, out float gz)
        {
            float fx = (x - ox) / h, fy = (y - oy) / h, fz = (z - oz) / h;
            int i = Math.Max(1, Math.Min(nx - 3, (int)Math.Floor(fx)));
            int j = Math.Max(1, Math.Min(ny - 3, (int)Math.Floor(fy)));
            int k = Math.Max(1, Math.Min(nz - 3, (int)Math.Floor(fz)));
            float tx = Math.Max(0f, Math.Min(1f, fx - i)), ty = Math.Max(0f, Math.Min(1f, fy - j)), tz = Math.Max(0f, Math.Min(1f, fz - k));
            float f = 0f;
            gx = gy = gz = 0f;
            for (int c = 0; c < 8; c++)
            {
                int a = i + cornerX[c], b = j + cornerY[c], d = k + cornerZ[c];
                float w = (cornerX[c] == 1 ? tx : 1f - tx) * (cornerY[c] == 1 ? ty : 1f - ty) * (cornerZ[c] == 1 ? tz : 1f - tz);
                float v = Val(a, b, d);
                f += v * w;
                gx += (Val(a + 1, b, d) - Val(a - 1, b, d)) * w;
                gy += (Val(a, b + 1, d) - Val(a, b - 1, d)) * w;
                gz += (Val(a, b, d + 1) - Val(a, b, d - 1)) * w;
            }
            float inv = 1f / (2f * h);
            gx *= inv; gy *= inv; gz *= inv;
            return f;
        }

        /// <summary>Trilinear value with the interpolant's own gradient (cheap; used to settle vertices).</summary>
        float SampleFast(float x, float y, float z, out float gx, out float gy, out float gz)
        {
            float fx = (x - ox) / h, fy = (y - oy) / h, fz = (z - oz) / h;
            int i = Math.Max(0, Math.Min(nx - 2, (int)Math.Floor(fx)));
            int j = Math.Max(0, Math.Min(ny - 2, (int)Math.Floor(fy)));
            int k = Math.Max(0, Math.Min(nz - 2, (int)Math.Floor(fz)));
            float tx = Math.Max(0f, Math.Min(1f, fx - i)), ty = Math.Max(0f, Math.Min(1f, fy - j)), tz = Math.Max(0f, Math.Min(1f, fz - k));
            float c000 = Val(i, j, k), c100 = Val(i + 1, j, k), c010 = Val(i, j + 1, k), c110 = Val(i + 1, j + 1, k);
            float c001 = Val(i, j, k + 1), c101 = Val(i + 1, j, k + 1), c011 = Val(i, j + 1, k + 1), c111 = Val(i + 1, j + 1, k + 1);
            float x00 = c000 + (c100 - c000) * tx, x10 = c010 + (c110 - c010) * tx, x01 = c001 + (c101 - c001) * tx, x11 = c011 + (c111 - c011) * tx;
            float y0 = x00 + (x10 - x00) * ty, y1 = x01 + (x11 - x01) * ty;
            float dx0 = (c100 - c000) + ((c110 - c010) - (c100 - c000)) * ty, dx1 = (c101 - c001) + ((c111 - c011) - (c101 - c001)) * ty;
            gx = (dx0 + (dx1 - dx0) * tz) / h;
            gy = ((x10 - x00) + ((x11 - x01) - (x10 - x00)) * tz) / h;
            gz = (y1 - y0) / h;
            return y0 + (y1 - y0) * tz;
        }

        float Val(int i, int j, int k)
        {
            i = Math.Max(0, Math.Min(nx - 1, i));
            j = Math.Max(0, Math.Min(ny - 1, j));
            k = Math.Max(0, Math.Min(nz - 1, k));
            float v = F[G(i, j, k)];
            // Unsampled space is "far outside"; clamp so it can't blow up a gradient.
            return v > 1e8f ? h * 3f : v;
        }

        // ------------------------------------------------------------------ Attributes

        /// <summary>
        /// Per-vertex distance to the nearest part of every colour slot (the shader paints each pixel with the
        /// closest slot, so colour edges follow the parts' real outlines instead of the triangles), and up to four
        /// joint weights (parts near a vertex pull it along with their joint; blends are soft across a few cm).
        /// </summary>
        public static void Attributes(Result r, List<Prim> prims, int slots, int bones, float softness,
            out float[] slotDist, out int[] boneIdx, out float[] boneW)
        {
            int n = r.count;
            slotDist = new float[n * slots];
            boneIdx = new int[n * 4];
            boneW = new float[n * 4];
            var bd = new float[Math.Max(1, bones)];
            const float far = 0.25f;
            for (int v = 0; v < n; v++)
            {
                float x = r.pos[v * 3], y = r.pos[v * 3 + 1], z = r.pos[v * 3 + 2];
                for (int s = 0; s < slots; s++) slotDist[v * slots + s] = far;
                for (int b = 0; b < bd.Length; b++) bd[b] = float.MaxValue;
                foreach (var p in prims)
                {
                    if (x < p.minX - far || x > p.maxX + far || y < p.minY - far || y > p.maxY + far || z < p.minZ - far || z > p.maxZ + far) continue;
                    float d = Eval(p, x, y, z);
                    int si = v * slots + p.slot;
                    if (d < slotDist[si]) slotDist[si] = d;
                    if (p.bone >= 0 && p.bone < bd.Length && d < bd[p.bone]) bd[p.bone] = d;
                }
                for (int s = 0; s < slots; s++) slotDist[v * slots + s] = Math.Min(far, slotDist[v * slots + s]);
                float dmin = float.MaxValue;
                for (int b = 0; b < bd.Length; b++) dmin = Math.Min(dmin, bd[b]);
                // Top four joints by closeness.
                for (int q = 0; q < 4; q++) { boneIdx[v * 4 + q] = 0; boneW[v * 4 + q] = 0f; }
                float total = 0f;
                for (int q = 0; q < 4; q++)
                {
                    int pick = -1;
                    float pd = float.MaxValue;
                    for (int b = 0; b < bd.Length; b++)
                        if (bd[b] < pd) { pd = bd[b]; pick = b; }
                    if (pick < 0 || pd == float.MaxValue) break;
                    float w = (float)Math.Exp(-(pd - dmin) / Math.Max(1e-4f, softness));
                    bd[pick] = float.MaxValue;
                    if (w < 0.01f) break;
                    boneIdx[v * 4 + q] = pick;
                    boneW[v * 4 + q] = w;
                    total += w;
                }
                if (total <= 0f) { boneW[v * 4] = 1f; total = 1f; }
                for (int q = 0; q < 4; q++) boneW[v * 4 + q] /= total;
            }
        }
    }
}
