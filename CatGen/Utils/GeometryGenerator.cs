using System;
using System.Collections.Generic;
using System.Linq;

using SharpDX;

namespace CatGen.Utils;

public static class GeometryGenerator
{

    public static MeshData CreateGrid(float width, float depth, int m, int n)
    {
        var meshData = new MeshData();

        //
        // Create the vertices.
        //

        float halfWidth = 0.5f * width;
        float halfDepth = 0.5f * depth;

        float dx = width / (n - 1);
        float dz = depth / (m - 1);

        float du = 1f / (n - 1);
        float dv = 1f / (m - 1);

        for (int i = 0; i < m; i++)
        {
            float z = halfDepth - i * dz;
            for (int j = 0; j < n; j++)
            {
                float x = -halfWidth + j * dx;

                meshData.Vertices.Add(new BiggaVertex(
                    new Vector3(x, 0, z),
                    new Vector3(0, 1, 0),
                    new Vector3(1, 0, 0),
                    new Vector2(j * du, i * dv))); // Stretch texture over grid.
            }
        }

        //
        // Create the indices.
        //

        // Iterate over each quad and compute indices.
        for (int i = 0; i < m - 1; i++)
        {
            for (int j = 0; j < n - 1; j++)
            {
                meshData.Indices.Add(i * n + j);
                meshData.Indices.Add(i * n + j + 1);
                meshData.Indices.Add((i + 1) * n + j);

                meshData.Indices.Add((i + 1) * n + j);
                meshData.Indices.Add(i * n + j + 1);
                meshData.Indices.Add((i + 1) * n + j + 1);
            }
        }

        return meshData;
    }

    public static MeshData CreateBox(float width, float height, float depth, int numSubdivisions)
    {
        var meshData = new MeshData();

        //
        // Create the vertices.
        //

        var w2 = 0.5f * width;
        var h2 = 0.5f * height;
        var d2 = 0.5f * depth;


        // Fill in the front face vertex data.
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, -d2, 0, 0, -1, 1, 0, 0, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, -d2, 0, 0, -1, 1, 0, 0, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, -d2, 0, 0, -1, 1, 0, 0, 1, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, -d2, 0, 0, -1, 1, 0, 0, 1, 1));
        // Fill in the back face vertex data.
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, +d2, 0, 0, 1, -1, 0, 0, 1, 1));
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, +d2, 0, 0, 1, -1, 0, 0, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, +d2, 0, 0, 1, -1, 0, 0, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, +d2, 0, 0, 1, -1, 0, 0, 1, 0));
        // Fill in the top face vertex data.
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, -d2, 0, 1, 0, 1, 0, 0, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, +d2, 0, 1, 0, 1, 0, 0, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, +d2, 0, 1, 0, 1, 0, 0, 1, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, -d2, 0, 1, 0, 1, 0, 0, 1, 1));
        // Fill in the bottom face vertex data.
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, -d2, 0, -1, 0, -1, 0, 0, 1, 1));
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, -d2, 0, -1, 0, -1, 0, 0, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, +d2, 0, -1, 0, -1, 0, 0, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, +d2, 0, -1, 0, -1, 0, 0, 1, 0));
        // Fill in the left face vertex data.
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, +d2, -1, 0, 0, 0, 0, -1, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, +d2, -1, 0, 0, 0, 0, -1, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(-w2, +h2, -d2, -1, 0, 0, 0, 0, -1, 1, 0));
        meshData.Vertices.Add(new BiggaVertex(-w2, -h2, -d2, -1, 0, 0, 0, 0, -1, 1, 1));
        // Fill in the right face vertex data.
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, -d2, 1, 0, 0, 0, 0, 1, 0, 1));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, -d2, 1, 0, 0, 0, 0, 1, 0, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, +h2, +d2, 1, 0, 0, 0, 0, 1, 1, 0));
        meshData.Vertices.Add(new BiggaVertex(+w2, -h2, +d2, 1, 0, 0, 0, 0, 1, 1, 1));

        //
        // Create the indices.
        //

        meshData.Indices.AddRange(new[]
        {
            // Fill in the front face index data.
            0, 1, 2, 0, 2, 3,
            // Fill in the back face index data.
            4, 5, 6, 4, 6, 7,
            // Fill in the top face index data.
            8, 9, 10, 8, 10, 11,
            // Fill in the bottom face index data.
            12, 13, 14, 12, 14, 15,
            // Fill in the left face index data
            16, 17, 18, 16, 18, 19,
            // Fill in the right face index data
            20, 21, 22, 20, 22, 23
        });

        // Put a cap on the number of subdivisions.
        numSubdivisions = Math.Min(numSubdivisions, 6);

        for (int i = 0; i < numSubdivisions; ++i)
            Subdivide(meshData);

        return meshData;
    }

    //TODO:
    private static void Subdivide(MeshData meshData)
    {
        // Save a copy of the input geometry.
        var verticesCopy = meshData.Vertices.ToArray();
        var indicesCopy = meshData.Indices.ToArray();

        meshData.Vertices.Clear();
        meshData.Indices.Clear();

        //       v1
        //       *
        //      / \
        //     /   \
        //  m0*-----*m1
        //   / \   / \
        //  /   \ /   \
        // *-----*-----*
        // v0    m2     v2

        var numTriangles = indicesCopy.Length / 3;
        for (var i = 0; i < numTriangles; i++)
        {
            var v0 = verticesCopy[indicesCopy[i * 3 + 0]];
            var v1 = verticesCopy[indicesCopy[i * 3 + 1]];
            var v2 = verticesCopy[indicesCopy[i * 3 + 2]];

            //
            // Generate the midpoints.
            //

            var m0 = MidPoint(v0, v1);
            var m1 = MidPoint(v1, v2);
            var m2 = MidPoint(v0, v2);

            //
            // Add new geometry.
            //

            meshData.Vertices.Add(v0); // 0
            meshData.Vertices.Add(v1); // 1
            meshData.Vertices.Add(v2); // 2
            meshData.Vertices.Add(m0); // 3
            meshData.Vertices.Add(m1); // 4
            meshData.Vertices.Add(m2); // 5

            meshData.Indices.Add(i * 6 + 0);
            meshData.Indices.Add(i * 6 + 3);
            meshData.Indices.Add(i * 6 + 5);

            meshData.Indices.Add(i * 6 + 3);
            meshData.Indices.Add(i * 6 + 4);
            meshData.Indices.Add(i * 6 + 5);

            meshData.Indices.Add(i * 6 + 5);
            meshData.Indices.Add(i * 6 + 4);
            meshData.Indices.Add(i * 6 + 2);

            meshData.Indices.Add(i * 6 + 3);
            meshData.Indices.Add(i * 6 + 1);
            meshData.Indices.Add(i * 6 + 4);
        }
    }
    //TODO:
    private static BiggaVertex MidPoint(BiggaVertex v0, BiggaVertex v1)
    {
        // Compute the midpoints of all the attributes. Vectors need to be normalized
        // since linear interpolating can make them not unit length.
        var pos = 0.5f * (v0.Position + v1.Position);
        var normal = Vector3.Normalize(0.5f * (v0.Normal + v1.Normal));
        var tangent = Vector3.Normalize(0.5f * (v0.TangentU + v1.TangentU));
        var tex = 0.5f * (v0.TextureCoordinate + v1.TextureCoordinate);

        return new BiggaVertex(pos, normal, tangent, tex);
    }

    public static MeshData CreateCylinder(float bottomRadius, float topRadius,
        float height, int sliceCount, int stackCount)
    {
        var meshData = new MeshData();

        BuildCylinderSide(bottomRadius, topRadius, height, sliceCount, stackCount, meshData);
        BuildCylinderTopCap(topRadius, height, sliceCount, meshData);
        BuildCylinderBottomCap(bottomRadius, height, sliceCount, meshData);

        return meshData;
    }

    private static void BuildCylinderSide(float bottomRadius, float topRadius,
            float height, int sliceCount, int stackCount, MeshData meshData)
        {
            var stackHeight = height / stackCount;

            // Amount to increment radius as we move up each stack level from bottom to top.
            var radiusStep = (topRadius - bottomRadius) / stackCount;

            var ringCount = stackCount + 1;

            // Compute vertices for each stack ring starting at the bottom and moving up.
            for (var i = 0; i < ringCount; i++)
            {
                var y = -0.5f * height + i * stackHeight;
                var r = bottomRadius + i * radiusStep;

                // Vertices of ring.
                var dTheta = 2.0f * MathUtil.Pi / sliceCount;
                for (var j = 0; j <= sliceCount; j++)
                {
                    var c = MathHelper.Cosf(j * dTheta);
                    var s = MathHelper.Sinf(j * dTheta);

                    var pos = new Vector3(r * c, y, r * s);
                    var uv = new Vector2((float)j / sliceCount, 1f - (float)i / stackCount);
                    var tangent = new Vector3(-s, 0.0f, c);

                    var dr = bottomRadius - topRadius;
                    var bitangent = new Vector3(dr * c, -height, dr * s);

                    var normal = Vector3.Cross(tangent, bitangent);
                    normal.Normalize();
                    meshData.Vertices.Add(new BiggaVertex(pos, normal, tangent, uv));
                }
            }

            // Add one because we duplicate the first and last vertex per ring
            // since the texture coordinates are different.
            var ringVertexCount = sliceCount + 1;

            // Compute indices for each stack.
            for (var i = 0; i < stackCount; i++)
            {
                for (var j = 0; j < sliceCount; j++)
                {
                    meshData.Indices.Add(i * ringVertexCount + j);
                    meshData.Indices.Add((i + 1) * ringVertexCount + j);
                    meshData.Indices.Add((i + 1) * ringVertexCount + j + 1);

                    meshData.Indices.Add(i * ringVertexCount + j);
                    meshData.Indices.Add((i + 1) * ringVertexCount + j + 1);
                    meshData.Indices.Add(i * ringVertexCount + j + 1);
                }
            }
        }

        private static void BuildCylinderTopCap(float topRadius, float height,
            int sliceCount, MeshData meshData)
        {
            var baseIndex = meshData.Vertices.Count;

            var y = 0.5f * height;
            var dTheta = 2.0f * MathUtil.Pi / sliceCount;

            // Duplicate cap ring vertices because the texture coordinates and normals differ.
            for (var i = 0; i <= sliceCount; i++)
            {
                var x = topRadius * MathHelper.Cosf(i * dTheta);
                var z = topRadius * MathHelper.Sinf(i * dTheta);

                // Scale down by the height to try and make top cap texture coord area
                // proportional to base.
                var u = x / height + 0.5f;
                var v = z / height + 0.5f;

                meshData.Vertices.Add(new BiggaVertex(
                    new Vector3(x, y, z), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector2(u, v)));
            }

            // Cap center vertex.
            meshData.Vertices.Add(new BiggaVertex(
                new Vector3(0, y, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector2(0.5f, 0.5f)));

            // Index of center vertex.
            var centerIndex = meshData.Vertices.Count - 1;

            for (var i = 0; i < sliceCount; i++)
            {
                meshData.Indices.Add(centerIndex);
                meshData.Indices.Add(baseIndex + i + 1);
                meshData.Indices.Add(baseIndex + i);
            }
        }

        private static void BuildCylinderBottomCap(float bottomRadius, float height,
            int sliceCount, MeshData meshData)
        {
            var baseIndex = meshData.Vertices.Count;
            var y = -0.5f * height;

            // vertices of ring
            var dTheta = 2.0f * MathUtil.Pi / sliceCount;
            for (var i = 0; i <= sliceCount; i++)
            {
                var x = bottomRadius * MathHelper.Cosf(i * dTheta);
                var z = bottomRadius * MathHelper.Sinf(i * dTheta);

                // Scale down by the height to try and make top cap texture coord area
                // proportional to base.
                var u = x / height + 0.5f;
                var v = z / height + 0.5f;

                meshData.Vertices.Add(new BiggaVertex(new Vector3(x, y, z), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector2(u, v)));
            }

            // Cap center vertex.
            meshData.Vertices.Add(new BiggaVertex(new Vector3(0, y, 0), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector2(0.5f, 0.5f)));

            // Cache the index of center vertex.
            var centerIndex = meshData.Vertices.Count - 1;

            for (var i = 0; i < sliceCount; i++)
            {
                meshData.Indices.Add(baseIndex + i);
                meshData.Indices.Add(baseIndex + i + 1);
                meshData.Indices.Add(centerIndex);
            }
        }

    //TODO:
    public static SubmeshGeometry AppendMeshData(MeshData meshData, List<BiggaVertex> vertices, List<int> indices)
    {
        // Определяем SubmeshGeometry которая описывает часть буфера вершин/индексов, содержащую подгеометрию

        var submesh = new SubmeshGeometry
        {
            IndexCount = meshData.Indices.Count,
            StartIndexLocation = indices.Count,
            BaseVertexLocation = vertices.Count,
            World = meshData.NormalizedWorld,
        };

        vertices.AddRange(meshData.Vertices);
        indices.AddRange(meshData.Indices);

        return submesh;
    }

    //TODO:
    public static MeshData CreateSphere(float radius, int sliceCount, int stackCount)
    {
        var meshData = new MeshData();

        //
        // Compute the vertices stating at the top pole and moving down the stacks.
        //

        // Poles: note that there will be texture coordinate distortion as there is
        // not a unique point on the texture map to assign to the pole when mapping
        // a rectangular texture onto a sphere.

        // Top vertex.
        meshData.Vertices.Add(new BiggaVertex(new Vector3(0, radius, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), Vector2.Zero));

        var phiStep = MathUtil.Pi / stackCount;
        var thetaStep = 2f * MathUtil.Pi / sliceCount;

        for (var i = 1; i <= stackCount - 1; i++)
        {
            var phi = i * phiStep;
            for (var j = 0; j <= sliceCount; j++)
            {
                var theta = j * thetaStep;

                // Spherical to cartesian.
                var pos = new Vector3(
                    radius * MathHelper.Sinf(phi) * MathHelper.Cosf(theta),
                    radius * MathHelper.Cosf(phi),
                    radius * MathHelper.Sinf(phi) * MathHelper.Sinf(theta));

                // Partial derivative of P with respect to theta.
                var tan = new Vector3(
                    -radius * MathHelper.Sinf(phi) * MathHelper.Sinf(theta),
                    0,
                    radius * MathHelper.Sinf(phi) * MathHelper.Cosf(theta));
                tan.Normalize();

                var norm = pos;
                norm.Normalize();

                var texCoord = new Vector2(theta / (MathUtil.Pi * 2), phi / MathUtil.Pi);

                meshData.Vertices.Add(new BiggaVertex(pos, norm, tan, texCoord));
            }
        }

        // Bottom vertex.
        meshData.Vertices.Add(new BiggaVertex(0, -radius, 0, 0, -1, 0, 1, 0, 0, 0, 1));

        //
        // Compute indices for top stack.  The top stack was written first to the vertex buffer
        // and connects the top pole to the first ring.
        //

        for (var i = 1; i <= sliceCount; i++)
        {
            meshData.Indices.Add(0);
            meshData.Indices.Add(i + 1);
            meshData.Indices.Add(i);
        }

        //
        // Compute indices for inner stacks (not connected to poles).
        //

        var baseIndex = 1;
        var ringVertexCount = sliceCount + 1;
        for (var i = 0; i < stackCount - 2; i++)
        {
            for (var j = 0; j < sliceCount; j++)
            {
                meshData.Indices.Add(baseIndex + i * ringVertexCount + j);
                meshData.Indices.Add(baseIndex + i * ringVertexCount + j + 1);
                meshData.Indices.Add(baseIndex + (i + 1) * ringVertexCount + j);

                meshData.Indices.Add(baseIndex + (i + 1) * ringVertexCount + j);
                meshData.Indices.Add(baseIndex + i * ringVertexCount + j + 1);
                meshData.Indices.Add(baseIndex + (i + 1) * ringVertexCount + j + 1);
            }
        }

        //
        // Compute indices for bottom stack.  The bottom stack was written last to the vertex buffer
        // and connects the bottom pole to the bottom ring.
        //

        // South pole vertex was added last.
        var southPoleIndex = meshData.Vertices.Count - 1;

        // Offset the indices to the index of the first vertex in the last ring.
        baseIndex = southPoleIndex - ringVertexCount;

        for (var i = 0; i < sliceCount; i++)
        {
            meshData.Indices.Add(southPoleIndex);
            meshData.Indices.Add(baseIndex + i);
            meshData.Indices.Add(baseIndex + i + 1);
        }
        return meshData;
    }


}
