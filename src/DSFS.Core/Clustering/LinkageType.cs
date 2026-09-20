namespace DSFS.Core.Clustering;

/// <summary>
/// The inter-cluster distance rule used while merging clusters (section 2.4 / 3.3.3 of the thesis).
/// </summary>
public enum LinkageType
{
    /// <summary>Distance between two clusters = the minimum distance between any pair of their members. Prone to "chaining".</summary>
    Single,

    /// <summary>Distance between two clusters = the maximum distance between any pair of their members.</summary>
    Complete,

    /// <summary>Distance between two clusters = the average distance across all pairs of their members.</summary>
    Average,

    /// <summary>
    /// The algorithm proposed by this research: behaves like Single linkage (keeps its sensitivity to
    /// closely related documents) but rejects a candidate merge that would only be justified by a single
    /// close pair while the rest of the two clusters are, on average, far apart - the classic cause of
    /// chaining. See <see cref="AgglomerativeClustering"/> for the exact rule.
    /// </summary>
    ImprovedSingleLinkage
}
