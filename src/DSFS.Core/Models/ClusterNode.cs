namespace DSFS.Core.Models;

/// <summary>
/// A node of the dendrogram built by agglomerative hierarchical clustering.
/// A leaf node wraps a single document; an internal node is the merge of two
/// child clusters at a given linkage distance.
/// </summary>
public class ClusterNode
{
    /// <summary>Unique id of this node within the dendrogram.</summary>
    public int Id { get; set; }

    /// <summary>True when this node is a single original document (no children).</summary>
    public bool IsLeaf => Left is null && Right is null;

    /// <summary>Document id when <see cref="IsLeaf"/> is true, otherwise -1.</summary>
    public int DocumentId { get; set; } = -1;

    /// <summary>Left child (null for leaves).</summary>
    public ClusterNode? Left { get; set; }

    /// <summary>Right child (null for leaves).</summary>
    public ClusterNode? Right { get; set; }

    /// <summary>
    /// The linkage distance at which Left and Right were merged (0 for leaves).
    /// This is the height of the node in the dendrogram.
    /// </summary>
    public double MergeDistance { get; set; }

    /// <summary>All original document ids contained in the sub-tree rooted at this node.</summary>
    public List<int> Members { get; set; } = new();
}
