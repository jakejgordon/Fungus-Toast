using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Non-interactive contextual connector layer drawn behind mutation nodes.
    /// Every edge is derived from a registered named Core prerequisite; aggregate
    /// requirements deliberately have no node-to-node representation here.
    /// </summary>
    public sealed class MutationDependencyGraphGraphic : MaskableGraphic
    {
        private const float DirectThickness = 3.4f;
        private const float MinimumThickness = 1.45f;
        private const float DashLength = 9f;
        private const float DashGap = 6f;
        private const float ArrowLength = 15f;
        private const float ArrowHalfWidth = 8f;
        private const float CrossCategoryKnotSize = 3.5f;
        private const float UnlockGrowthDuration = 0.22f;
        private const float RouteEmphasisDuration = 0.7f;

        private readonly List<Edge> edges = new();
        private readonly Dictionary<int, List<int>> incomingEdgeIndexesByMutationId = new();
        private readonly Dictionary<int, List<int>> outgoingEdgeIndexesByMutationId = new();
        private readonly Dictionary<int, int> upstreamDepthByMutationId = new();
        private readonly List<int> traversalMutationIds = new();
        private readonly HashSet<int> growingDependentIds = new();
        private readonly Vector3[] cardWorldCorners = new Vector3[4];

        private ScrollRect boundScrollRect;
        private Player inspectionPlayer;
        private float unlockGrowthProgress = 1f;
        private int emphasizedEdgeIndex = -1;
        private float routeEmphasisElapsed = RouteEmphasisDuration;

        public void Configure(IReadOnlyList<MutationNodeUI> nodes, IReadOnlyList<Mutation> mutations)
        {
            edges.Clear();
            incomingEdgeIndexesByMutationId.Clear();
            outgoingEdgeIndexesByMutationId.Clear();
            var nodesById = nodes.ToDictionary(node => node.MutationId);
            var mutationsById = mutations.ToDictionary(mutation => mutation.Id);

            foreach (Mutation dependent in mutations)
            {
                if (!nodesById.TryGetValue(dependent.Id, out MutationNodeUI dependentNode))
                {
                    continue;
                }

                foreach (MutationPrerequisite prerequisite in dependent.Prerequisites.Concat(dependent.AnyPrerequisiteGroups.SelectMany(group => group.Alternatives)))
                {
                    if (!nodesById.TryGetValue(prerequisite.MutationId, out MutationNodeUI prerequisiteNode)
                        || !mutationsById.TryGetValue(prerequisite.MutationId, out Mutation prerequisiteMutation))
                    {
                        continue;
                    }

                    int edgeIndex = edges.Count;
                    edges.Add(new Edge(
                        prerequisiteMutation.Id,
                        dependent.Id,
                        prerequisite.RequiredLevel,
                        prerequisiteNode.DependencyAnchorRect,
                        dependentNode.DependencyAnchorRect,
                        prerequisiteMutation.Category != dependent.Category));
                    AddEdgeIndex(incomingEdgeIndexesByMutationId, dependent.Id, edgeIndex);
                    AddEdgeIndex(outgoingEdgeIndexesByMutationId, prerequisiteMutation.Id, edgeIndex);
                }
            }

            inspectionPlayer = null;
            emphasizedEdgeIndex = -1;
            routeEmphasisElapsed = RouteEmphasisDuration;
            SetVerticesDirty();
        }

        public void SetInspection(int focusedMutationId, Player player)
        {
            inspectionPlayer = player;
            foreach (Edge edge in edges)
            {
                edge.Relationship = EdgeRelationship.None;
                edge.Depth = 0;
            }

            upstreamDepthByMutationId.Clear();
            traversalMutationIds.Clear();
            upstreamDepthByMutationId[focusedMutationId] = 0;
            traversalMutationIds.Add(focusedMutationId);

            for (int traversalIndex = 0; traversalIndex < traversalMutationIds.Count; traversalIndex++)
            {
                int dependentId = traversalMutationIds[traversalIndex];
                int dependentDepth = upstreamDepthByMutationId[dependentId];
                if (!incomingEdgeIndexesByMutationId.TryGetValue(dependentId, out List<int> incomingIndexes))
                {
                    continue;
                }

                foreach (int edgeIndex in incomingIndexes)
                {
                    Edge edge = edges[edgeIndex];
                    edge.Relationship = EdgeRelationship.Upstream;
                    edge.Depth = dependentDepth;
                    int prerequisiteDepth = dependentDepth + 1;
                    if (upstreamDepthByMutationId.TryGetValue(edge.PrerequisiteId, out int knownDepth)
                        && knownDepth <= prerequisiteDepth)
                    {
                        continue;
                    }

                    upstreamDepthByMutationId[edge.PrerequisiteId] = prerequisiteDepth;
                    traversalMutationIds.Add(edge.PrerequisiteId);
                }
            }

            if (outgoingEdgeIndexesByMutationId.TryGetValue(focusedMutationId, out List<int> outgoingIndexes))
            {
                foreach (int edgeIndex in outgoingIndexes)
                {
                    Edge edge = edges[edgeIndex];
                    edge.Relationship = EdgeRelationship.Downstream;
                    edge.Depth = 0;
                }
            }

            SetVerticesDirty();
        }

        public void ClearInspection()
        {
            inspectionPlayer = null;
            foreach (Edge edge in edges)
            {
                edge.Relationship = EdgeRelationship.None;
                edge.Depth = 0;
            }

            SetVerticesDirty();
        }

        public void BindScrollRect(ScrollRect scrollRect)
        {
            if (boundScrollRect == scrollRect)
            {
                return;
            }

            if (boundScrollRect != null)
            {
                boundScrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
            }

            boundScrollRect = scrollRect;
            if (boundScrollRect != null)
            {
                boundScrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
            }
        }

        public void EmphasizeDirectRoute(int firstMutationId, int secondMutationId)
        {
            emphasizedEdgeIndex = -1;
            for (int index = 0; index < edges.Count; index++)
            {
                Edge edge = edges[index];
                if ((edge.PrerequisiteId == firstMutationId && edge.DependentId == secondMutationId)
                    || (edge.PrerequisiteId == secondMutationId && edge.DependentId == firstMutationId))
                {
                    emphasizedEdgeIndex = index;
                    routeEmphasisElapsed = 0f;
                    SetVerticesDirty();
                    return;
                }
            }
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
            bool requiresRedraw = false;
            if (unlockGrowthProgress < 1f)
            {
                float duration = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode
                    ? 0.08f
                    : UnlockGrowthDuration;
                unlockGrowthProgress = Mathf.Min(1f, unlockGrowthProgress + (Time.unscaledDeltaTime / duration));
                requiresRedraw = true;
                if (unlockGrowthProgress >= 1f)
                {
                    growingDependentIds.Clear();
                }
            }

            if (routeEmphasisElapsed < RouteEmphasisDuration)
            {
                routeEmphasisElapsed = Mathf.Min(RouteEmphasisDuration, routeEmphasisElapsed + Time.unscaledDeltaTime);
                requiresRedraw = true;
                if (routeEmphasisElapsed >= RouteEmphasisDuration)
                {
                    emphasizedEdgeIndex = -1;
                }
            }

            if (requiresRedraw)
            {
                SetVerticesDirty();
            }
        }

        protected override void OnDestroy()
        {
            if (boundScrollRect != null)
            {
                boundScrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
            }

            base.OnDestroy();
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            for (int edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
            {
                Edge edge = edges[edgeIndex];
                if (edge.PrerequisiteRect == null || edge.DependentRect == null)
                {
                    continue;
                }

                BuildRoutePoints(edge.PrerequisiteRect, edge.DependentRect, out Vector2 start, out Vector2 second, out Vector2 third, out Vector2 end);

                if (edge.Relationship != EdgeRelationship.None)
                {
                    bool prerequisiteMet = inspectionPlayer != null
                        && inspectionPlayer.GetMutationLevel(edge.PrerequisiteId) >= edge.RequiredLevel;
                    float depthFade = edge.Relationship == EdgeRelationship.Downstream
                        ? 1f
                        : Mathf.Pow(0.72f, edge.Depth);
                    float thickness = Mathf.Max(MinimumThickness, DirectThickness * depthFade);
                    float alpha = edge.Relationship == EdgeRelationship.Downstream
                        ? 0.94f
                        : Mathf.Lerp(0.42f, 0.96f, depthFade);
                    Color relationshipColor = edge.Relationship == EdgeRelationship.Upstream
                        ? UIStyleTokens.State.Warning
                        : MutationTreeColors.DependentBorder;
                    relationshipColor.a = alpha;

                    DrawOrthogonalRoute(
                        vertexHelper,
                        start,
                        second,
                        third,
                        end,
                        thickness,
                        relationshipColor,
                        dashed: !prerequisiteMet,
                        crossCategory: edge.IsCrossCategory,
                        drawArrowhead: true);
                }

                if (growingDependentIds.Contains(edge.DependentId))
                {
                    DrawGrowingPath(
                        vertexHelper,
                        start,
                        second,
                        third,
                        end,
                        unlockGrowthProgress,
                        UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, 0.98f));
                }

                if (edgeIndex == emphasizedEdgeIndex && routeEmphasisElapsed < RouteEmphasisDuration)
                {
                    float pulse = 1f - (routeEmphasisElapsed / RouteEmphasisDuration);
                    Color emphasisColor = UIStyleTokens.WithAlpha(UIStyleTokens.Text.Primary, Mathf.Lerp(0.18f, 0.92f, pulse));
                    DrawOrthogonalRoute(
                        vertexHelper,
                        start,
                        second,
                        third,
                        end,
                        DirectThickness + (2.4f * pulse),
                        emphasisColor,
                        dashed: false,
                        crossCategory: edge.IsCrossCategory,
                        drawArrowhead: true);
                }
            }
        }

        private static void DrawOrthogonalRoute(
            VertexHelper helper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Vector2 fourth,
            float thickness,
            Color color,
            bool dashed,
            bool crossCategory,
            bool drawArrowhead)
        {
            DrawSegment(helper, first, second, thickness, color, dashed);
            DrawSegment(helper, second, third, thickness, color, dashed);

            Vector2 arrowBase = fourth;
            bool canDrawArrowhead = false;
            if (drawArrowhead)
            {
                float finalSegmentLength = Vector2.Distance(third, fourth);
                if (finalSegmentLength > ArrowLength + 0.01f)
                {
                    arrowBase = Vector2.MoveTowards(fourth, third, ArrowLength);
                    canDrawArrowhead = true;
                }
            }

            // Reserve the final segment for the arrowhead. Its base must remain
            // outside the destination card; otherwise the arrow visibly finishes
            // in the card body even though its tip is technically on the border.
            DrawSegment(helper, third, arrowBase, thickness, color, dashed);

            if (crossCategory)
            {
                AddDiamond(helper, second, CrossCategoryKnotSize + (thickness * 0.25f), color);
                AddDiamond(helper, third, CrossCategoryKnotSize + (thickness * 0.25f), color);
            }

            if (canDrawArrowhead)
            {
                AddArrowHead(helper, arrowBase, fourth, color);
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
            float totalLength = Vector2.Distance(first, second)
                + Vector2.Distance(second, third)
                + Vector2.Distance(third, fourth);
            float remaining = totalLength * Mathf.Clamp01(progress);
            DrawGrowingSegment(helper, first, second, ref remaining, color);
            DrawGrowingSegment(helper, second, third, ref remaining, color);
            DrawGrowingSegment(helper, third, fourth, ref remaining, color);
        }

        private static void DrawGrowingSegment(VertexHelper helper, Vector2 start, Vector2 end, ref float remaining, Color color)
        {
            if (remaining <= 0f)
            {
                return;
            }

            float segmentLength = Vector2.Distance(start, end);
            if (segmentLength < 0.01f)
            {
                return;
            }

            float visibleLength = Mathf.Min(remaining, segmentLength);
            Vector2 visibleEnd = Vector2.Lerp(start, end, visibleLength / segmentLength);
            AddQuad(helper, start, visibleEnd, DirectThickness, color);
            remaining -= visibleLength;
        }

        private void BuildRoutePoints(
            RectTransform prerequisiteRect,
            RectTransform dependentRect,
            out Vector2 start,
            out Vector2 second,
            out Vector2 third,
            out Vector2 end)
        {
            Bounds prerequisiteBounds = GetCardBoundsInGraphSpace(prerequisiteRect);
            Bounds dependentBounds = GetCardBoundsInGraphSpace(dependentRect);
            Vector2 prerequisiteCenter = prerequisiteBounds.center;
            Vector2 dependentCenter = dependentBounds.center;
            Vector2 delta = dependentCenter - prerequisiteCenter;

            // Enter a card through the border facing its prerequisite. This keeps
            // arrowheads outside the card instead of routing up into its body when
            // a cross-lane dependency happens to share a tier row.
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                bool dependentIsToTheRight = delta.x >= 0f;
                start = new Vector2(
                    dependentIsToTheRight ? prerequisiteBounds.max.x : prerequisiteBounds.min.x,
                    prerequisiteCenter.y);
                end = new Vector2(
                    dependentIsToTheRight ? dependentBounds.min.x : dependentBounds.max.x,
                    dependentCenter.y);

                float middleX = (start.x + end.x) * 0.5f;
                second = new Vector2(middleX, start.y);
                third = new Vector2(middleX, end.y);
                return;
            }

            bool dependentIsBelow = delta.y < 0f;
            start = new Vector2(
                prerequisiteCenter.x,
                dependentIsBelow ? prerequisiteBounds.min.y : prerequisiteBounds.max.y);
            end = new Vector2(
                dependentCenter.x,
                dependentIsBelow ? dependentBounds.max.y : dependentBounds.min.y);

            float middleY = (start.y + end.y) * 0.5f;
            second = new Vector2(start.x, middleY);
            third = new Vector2(end.x, middleY);
        }

        private Bounds GetCardBoundsInGraphSpace(RectTransform cardRect)
        {
            cardRect.GetWorldCorners(cardWorldCorners);
            Vector3 minimum = rectTransform.InverseTransformPoint(cardWorldCorners[0]);
            Vector3 maximum = minimum;
            for (int index = 1; index < cardWorldCorners.Length; index++)
            {
                Vector3 corner = rectTransform.InverseTransformPoint(cardWorldCorners[index]);
                minimum = Vector3.Min(minimum, corner);
                maximum = Vector3.Max(maximum, corner);
            }

            return new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
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

        private static void AddArrowHead(VertexHelper helper, Vector2 approach, Vector2 tip, Color color)
        {
            Vector2 direction = (tip - approach).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector2 normal = new(-direction.y, direction.x);
            Vector2 baseCenter = tip - (direction * ArrowLength);
            int index = helper.currentVertCount;
            helper.AddVert(tip, color, Vector2.zero);
            helper.AddVert(baseCenter + (normal * ArrowHalfWidth), color, Vector2.zero);
            helper.AddVert(baseCenter - (normal * ArrowHalfWidth), color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
        }

        private static void AddDiamond(VertexHelper helper, Vector2 center, float radius, Color color)
        {
            int index = helper.currentVertCount;
            helper.AddVert(center + (Vector2.up * radius), color, Vector2.zero);
            helper.AddVert(center + (Vector2.right * radius), color, Vector2.zero);
            helper.AddVert(center + (Vector2.down * radius), color, Vector2.zero);
            helper.AddVert(center + (Vector2.left * radius), color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
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

        private static void AddEdgeIndex(Dictionary<int, List<int>> indexesByMutationId, int mutationId, int edgeIndex)
        {
            if (!indexesByMutationId.TryGetValue(mutationId, out List<int> indexes))
            {
                indexes = new List<int>();
                indexesByMutationId[mutationId] = indexes;
            }

            indexes.Add(edgeIndex);
        }

        private enum EdgeRelationship
        {
            None,
            Upstream,
            Downstream
        }

        private sealed class Edge
        {
            public Edge(
                int prerequisiteId,
                int dependentId,
                int requiredLevel,
                RectTransform prerequisiteRect,
                RectTransform dependentRect,
                bool isCrossCategory)
            {
                PrerequisiteId = prerequisiteId;
                DependentId = dependentId;
                RequiredLevel = requiredLevel;
                PrerequisiteRect = prerequisiteRect;
                DependentRect = dependentRect;
                IsCrossCategory = isCrossCategory;
            }

            public int PrerequisiteId { get; }
            public int DependentId { get; }
            public int RequiredLevel { get; }
            public RectTransform PrerequisiteRect { get; }
            public RectTransform DependentRect { get; }
            public bool IsCrossCategory { get; }
            public EdgeRelationship Relationship { get; set; }
            public int Depth { get; set; }
        }
    }
}
