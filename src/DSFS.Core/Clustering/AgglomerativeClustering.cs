using DSFS.Core.Models;
using DSFS.Core.Similarity;

namespace DSFS.Core.Clustering;

/// <summary>
/// Clustering Module (section 5.4.1.3 / Figure 10): builds a dendrogram over the TF-IDF
/// vectors of the corpus using bottom-up (agglomerative) hierarchical clustering, then lets
/// the caller cut the dendrogram into the desired number of document clusters.
///
/// This class implements four linkage strategies (see <see cref="LinkageType"/>), including
/// <see cref="LinkageType.ImprovedSingleLinkage"/>, which is this project's implementation of
/// the thesis's central contribution: addressing the "chaining issue" that plain single-linkage
/// clustering suffers from. Chaining happens because single linkage merges two clusters as soon
/// as *any one* pair of members is close, even if the rest of the two clusters are far apart -
/// this can string together a long chain of loosely related documents into one over-grown cluster.
/// The improved rule keeps single linkage's sensitivity but additionally requires that the
/// candidate merge is not "too optimistic": the average-linkage distance between the two
/// clusters must not exceed the single-link distance by more than a configurable guard factor.
/// If it does, that merge is skipped in favour of the next-closest candidate, which prevents a
/// single stray close pair from chaining two otherwise dissimilar clusters together.
/// </summary>
public class AgglomerativeClustering
{
    private readonly LinkageType _linkage;
    private readonly double _chainGuardFactor;

    /// <param name="linkage">Which linkage rule to use for merging clusters.</param>
    /// <param name="chainGuardFactor">
    /// Only used by <see cref="LinkageType.ImprovedSingleLinkage"/>. A merge candidate is accepted
    /// when averageLinkDistance &lt;= singleLinkDistance * chainGuardFactor. Lower values guard more
    /// aggressively against chaining (at the cost of sometimes needing more, smaller clusters);
    /// higher values degrade gracefully towards plain single linkage. Default: 1.5.
    /// </param>
    public AgglomerativeClustering(LinkageType linkage, double chainGuardFactor = 1.5)
    {
        _linkage = linkage;
        _chainGuardFactor = chainGuardFactor;
    }

    /// <summary>
    /// Runs the full agglomeration over the given documents (which must already have a
    /// non-empty <see cref="DocumentRecord.TfIdfVector"/>) and returns the dendrogram root.
    /// </summary>
    public ClusterNode BuildDendrogram(IReadOnlyList<DocumentRecord> documents)
    {
        int n = documents.Count;
        if (n == 0) throw new ArgumentException("Cannot cluster an empty document set.");

        // Pre-compute the full pairwise cosine-distance matrix once.
        var distance = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                double d = CosineSimilarity.Distance(documents[i].TfIdfVector, documents[j].TfIdfVector);
                distance[i, j] = d;
                distance[j, i] = d;
            }

        // Active clusters: nodeId -> set of original document indices (0-based, into `documents`).
        var activeClusters = new Dictionary<int, List<int>>();
        var nodesById = new Dictionary<int, ClusterNode>();
        int nextNodeId = 0;

        for (int i = 0; i < n; i++)
        {
            var leaf = new ClusterNode
            {
                Id = nextNodeId,
                DocumentId = documents[i].Id,
                MergeDistance = 0.0,
                Members = new List<int> { documents[i].Id }
            };
            nodesById[nextNodeId] = leaf;
            activeClusters[nextNodeId] = new List<int> { i }; // store matrix index, not DocumentRecord.Id
            nextNodeId++;
        }

        while (activeClusters.Count > 1)
        {
            var (idA, idB, mergeDistance) = FindClosestPair(activeClusters, distance);

            var mergedMembers = activeClusters[idA].Concat(activeClusters[idB]).ToList();
            var mergedNode = new ClusterNode
            {
                Id = nextNodeId,
                Left = nodesById[idA],
                Right = nodesById[idB],
                MergeDistance = mergeDistance,
                Members = mergedMembers.Select(matrixIndex => documents[matrixIndex].Id).ToList()
            };

            nodesById[nextNodeId] = mergedNode;
            activeClusters[nextNodeId] = mergedMembers;
            activeClusters.Remove(idA);
            activeClusters.Remove(idB);
            nextNodeId++;
        }

        return nodesById[nextNodeId - 1];
    }

    private (int idA, int idB, double distance) FindClosestPair(
        Dictionary<int, List<int>> activeClusters, double[,] distance)
    {
        var ids = activeClusters.Keys.ToList();

        // Rank all candidate pairs by their single-link (minimum) distance first - this is the
        // basis for both plain Single linkage and the ImprovedSingleLinkage guard check.
        var candidates = new List<(int a, int b, double single, double average, double complete)>();

        for (int x = 0; x < ids.Count; x++)
        {
            for (int y = x + 1; y < ids.Count; y++)
            {
                var membersA = activeClusters[ids[x]];
                var membersB = activeClusters[ids[y]];

                double min = double.MaxValue, max = double.MinValue, sum = 0.0;
                int pairs = 0;
                foreach (var i in membersA)
                {
                    foreach (var j in membersB)
                    {
                        double d = distance[i, j];
                        if (d < min) min = d;
                        if (d > max) max = d;
                        sum += d;
                        pairs++;
                    }
                }
                double avg = pairs > 0 ? sum / pairs : 0.0;
                candidates.Add((ids[x], ids[y], min, avg, max));
            }
        }

        switch (_linkage)
        {
            case LinkageType.Single:
            {
                var best = candidates.OrderBy(c => c.single).First();
                return (best.a, best.b, best.single);
            }
            case LinkageType.Complete:
            {
                var best = candidates.OrderBy(c => c.complete).First();
                return (best.a, best.b, best.complete);
            }
            case LinkageType.Average:
            {
                var best = candidates.OrderBy(c => c.average).First();
                return (best.a, best.b, best.average);
            }
            case LinkageType.ImprovedSingleLinkage:
            default:
            {
                // Try candidates in increasing order of single-link distance; accept the first
                // one whose average-link distance is not disproportionately larger (the chaining guard).
                foreach (var c in candidates.OrderBy(c => c.single))
                {
                    bool safeFromChaining = c.single <= 0.0
                        ? true // identical documents: always safe to merge
                        : c.average <= c.single * _chainGuardFactor;

                    if (safeFromChaining)
                        return (c.a, c.b, c.single);
                }

                // Fallback (guarantees termination): plain single-linkage minimum.
                var fallback = candidates.OrderBy(c => c.single).First();
                return (fallback.a, fallback.b, fallback.single);
            }
        }
    }

    /// <summary>
    /// Cuts the dendrogram into exactly <paramref name="desiredClusterCount"/> flat clusters by
    /// repeatedly splitting the currently-largest (highest merge distance) node in the frontier,
    /// starting from the root. Returns a map of clusterId -&gt; member document ids, and also
    /// writes <see cref="DocumentRecord.ClusterId"/> on the supplied documents.
    /// </summary>
    public Dictionary<int, List<int>> CutClusters(
        ClusterNode root, int desiredClusterCount, IReadOnlyList<DocumentRecord> documents)
    {
        if (desiredClusterCount < 1) desiredClusterCount = 1;

        var frontier = new List<ClusterNode> { root };

        while (frontier.Count < desiredClusterCount)
        {
            // Pick the node with the largest merge distance that still has children to split.
            var splittable = frontier.Where(nd => !nd.IsLeaf).ToList();
            if (splittable.Count == 0) break; // cannot split further than the number of leaves

            var toSplit = splittable.OrderByDescending(nd => nd.MergeDistance).First();
            frontier.Remove(toSplit);
            frontier.Add(toSplit.Left!);
            frontier.Add(toSplit.Right!);
        }

        var result = new Dictionary<int, List<int>>();
        for (int clusterId = 0; clusterId < frontier.Count; clusterId++)
            result[clusterId] = frontier[clusterId].Members;

        var docsById = documents.ToDictionary(d => d.Id);
        foreach (var (clusterId, memberDocIds) in result)
            foreach (var docId in memberDocIds)
                if (docsById.TryGetValue(docId, out var doc))
                    doc.ClusterId = clusterId;

        return result;
    }
}
