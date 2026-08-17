using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Non-interactive connector layer drawn behind mutation nodes. Same-category
    /// prerequisites are solid; cross-category grafts are dashed as a non-color cue.
    /// </summary>
    public sealed class MutationDependencyGraphGraphic : MaskableGraphic
    {
        private const float DefaultThickness = 1.5f;
        private const float HighlightThickness = 3f;
        private const float DashLength = 9f;
        private const float DashGap = 6f;
        private const float UnlockGrowthDuration = 0.55f;

        private readonly List<Edge> edges = new();
        private readonly HashSet<int> highlightedPrerequisiteIds = new();
        private readonly HashSet<int> highlightedDependentIds = new();
        private readonly HashSet<int> growingDependentIds = new();
        private float unlockGrowthProgress = 1f;

        public void Configure(IReadOnlyList<MutationNodeUI> nodes, IReadOnlyList<Mutation> mutations)
        {
            edges.Clear();
            var nodesById = nodes.ToDictionary(node => node.MutationId);
            var mutationsById = mutations.ToDictionary(mutation => mutation.Id);

            foreach (Mutation dependent in mutations)
            {
                if (!nodesById.TryGetValue(dependent.Id, out MutationNodeUI dependentNode))
                {
                    continue;
                }

                foreach (MutationPrerequisite prerequisite in dependent.Prerequisites)
                {
                    if (!nodesById.TryGetValue(prerequisite.MutationId, out MutationNodeUI prerequisiteNode)
                        || !mutationsById.TryGetValue(prerequisite.MutationId, out Mutation prerequisiteMutation))
                    {
                        continue;
                    }

                    edges.Add(new Edge(
                        prerequisiteMutation.Id,
                        dependent.Id,
                        prerequisiteNode.transform as RectTransform,
                        dependentNode.transform as RectTransform,
                        prerequisiteMutation.Category != dependent.Category));
                }
            }

            SetVerticesDirty();
        }

        public void SetInspection(IEnumerable<int> prerequisitePathIds, IEnumerable<int> directDependentIds)
        {
            highlightedPrerequisiteIds.Clear();
            highlightedDependentIds.Clear();
            highlightedPrerequisiteIds.UnionWith(prerequisitePathIds);
            highlightedDependentIds.UnionWith(directDependentIds);
            SetVerticesDirty();
        }

        public void ClearInspection()
        {
            highlightedPrerequisiteIds.Clear();
            highlightedDependentIds.Clear();
            SetVerticesDirty();
        }

        public void RefreshGeometry() => SetVerticesDirty();

        public void GrowNewlyUnlockedPaths(IEnumerable<int> dependentIds)
        {
            growingDependentIds.Clear();
            growingDependentIds.UnionWith(dependentIds);
            if (growingDependentIds.Count == 0)
            {
                return;
            }

            unlockGrowthProgress = 0f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (unlockGrowthProgress >= 1f)
            {
                return;
            }

            unlockGrowthProgress = Mathf.Min(1f, unlockGrowthProgress + (Time.unscaledDeltaTime / UnlockGrowthDuration));
            SetVerticesDirty();
            if (unlockGrowthProgress >= 1f)
            {
                growingDependentIds.Clear();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            foreach (Edge edge in edges)
            {
                if (edge.PrerequisiteRect == null || edge.DependentRect == null)
                {
                    continue;
                }

                bool highlighted = (highlightedPrerequisiteIds.Contains(edge.PrerequisiteId)
                        && highlightedPrerequisiteIds.Contains(edge.DependentId))
                    || (highlightedDependentIds.Contains(edge.DependentId)
                        && highlightedPrerequisiteIds.Contains(edge.PrerequisiteId));
                Color edgeColor = highlighted
                    ? UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Spore, 0.92f)
                    : UIStyleTokens.WithAlpha(UIStyleTokens.Text.Muted, 0.28f);
                float thickness = highlighted ? HighlightThickness : DefaultThickness;

                Vector2 start = ToLocalPoint(edge.PrerequisiteRect, new Vector2(0.5f, 0f));
                Vector2 end = ToLocalPoint(edge.DependentRect, new Vector2(0.5f, 1f));
                float middleY = (start.y + end.y) * 0.5f;

                DrawSegment(vertexHelper, start, new Vector2(start.x, middleY), thickness, edgeColor, edge.IsCrossCategory);
                DrawSegment(vertexHelper, new Vector2(start.x, middleY), new Vector2(end.x, middleY), thickness, edgeColor, edge.IsCrossCategory);
                DrawSegment(vertexHelper, new Vector2(end.x, middleY), end, thickness, edgeColor, edge.IsCrossCategory);

                if (growingDependentIds.Contains(edge.DependentId))
                {
                    DrawGrowingPath(
                        vertexHelper,
                        start,
                        new Vector2(start.x, middleY),
                        new Vector2(end.x, middleY),
                        end,
                        unlockGrowthProgress,
                        UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, 0.98f));
                }
            }
        }

        private static void DrawGrowingPath(
            VertexHelper helper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Vector2 fourth,
            float progress,
            Color color)
        {
            Vector2[] points = { first, second, third, fourth };
            float totalLength = Vector2.Distance(first, second)
                + Vector2.Distance(second, third)
                + Vector2.Distance(third, fourth);
            float remaining = totalLength * Mathf.Clamp01(progress);

            for (int index = 0; index < points.Length - 1 && remaining > 0f; index++)
            {
                float segmentLength = Vector2.Distance(points[index], points[index + 1]);
                if (segmentLength < 0.01f)
                {
                    continue;
                }

                float visibleLength = Mathf.Min(remaining, segmentLength);
                Vector2 visibleEnd = Vector2.Lerp(points[index], points[index + 1], visibleLength / segmentLength);
                AddQuad(helper, points[index], visibleEnd, HighlightThickness, color);
                remaining -= visibleLength;
            }
        }

        private Vector2 ToLocalPoint(RectTransform target, Vector2 normalizedPoint)
        {
            Vector2 targetPoint = new(
                Mathf.Lerp(target.rect.xMin, target.rect.xMax, normalizedPoint.x),
                Mathf.Lerp(target.rect.yMin, target.rect.yMax, normalizedPoint.y));
            return rectTransform.InverseTransformPoint(target.TransformPoint(targetPoint));
        }

        private static void DrawSegment(VertexHelper helper, Vector2 start, Vector2 end, float thickness, Color color, bool dashed)
        {
            float length = Vector2.Distance(start, end);
            if (length < 0.01f)
            {
                return;
            }

            if (!dashed)
            {
                AddQuad(helper, start, end, thickness, color);
                return;
            }

            Vector2 direction = (end - start) / length;
            for (float distance = 0f; distance < length; distance += DashLength + DashGap)
            {
                float dashEnd = Mathf.Min(distance + DashLength, length);
                AddQuad(helper, start + (direction * distance), start + (direction * dashEnd), thickness, color);
            }
        }

        private static void AddQuad(VertexHelper helper, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new(-direction.y, direction.x);
            Vector2 offset = normal * (thickness * 0.5f);
            int index = helper.currentVertCount;
            helper.AddVert(start - offset, color, Vector2.zero);
            helper.AddVert(start + offset, color, Vector2.zero);
            helper.AddVert(end + offset, color, Vector2.zero);
            helper.AddVert(end - offset, color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
        }

        private readonly struct Edge
        {
            public Edge(int prerequisiteId, int dependentId, RectTransform prerequisiteRect, RectTransform dependentRect, bool isCrossCategory)
            {
                PrerequisiteId = prerequisiteId;
                DependentId = dependentId;
                PrerequisiteRect = prerequisiteRect;
                DependentRect = dependentRect;
                IsCrossCategory = isCrossCategory;
            }

            public int PrerequisiteId { get; }
            public int DependentId { get; }
            public RectTransform PrerequisiteRect { get; }
            public RectTransform DependentRect { get; }
            public bool IsCrossCategory { get; }
        }
    }
}
