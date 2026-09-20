# DSFS Architecture — Thesis → Code Map

This document maps **"Development of an Efficient Hierarchical Clustering Analysis
Using Agglomerative Clustering Algorithm"** (Arshia Naeem, MSCS Thesis, Lahore
College for Women University) onto this repository's source files, so a reviewer
(or supervisor) can trace every box in **Figure 7 — "A Desktop Scholar Facilitator
System Architecture"** to a concrete class.

## Figure 7, box by box

| Figure 7 box | Thesis section | Code |
|---|---|---|
| User Interface | 5.2.2 / 5.4.2 | `DSFS.App.Forms.LoginForm` |
| Admin Interface | 5.2.1 / 5.4.1 | `DSFS.App.Forms.AdminDashboardForm` |
| User Sign up | 5.4.2.1 | `DSFS.App.Forms.RegisterForm`, `DSFS.Core.Persistence.UserRepository.Register` |
| User Profile | 5.4.2.2 | `DSFS.App.Forms.ScholarProfileForm` |
| Research Publications / Database | 5.4.1.1 (Data Collection Module, Figure 8) | `DSFS.Core.TextExtraction.*`, `DSFS.Core.Persistence.DocumentRepository` |
| Tokenization | 3.3.2.1 | `DSFS.Core.Preprocessing.Tokenizer` |
| Stop Word Removal | 3.3.2.2 | `DSFS.Core.Preprocessing.StopWordRemover` |
| Stemming | 3.3.2.3 (Porter, 1980) | `DSFS.Core.Preprocessing.PorterStemmer` |
| TF-IDF | 3.3.2.4 | `DSFS.Core.Preprocessing.TfIdfVectorizer` |
| Mapping Document to Cluster / Document Clusters | 5.4.1.3 (Clustering Module, Figure 10) | `DSFS.Core.Clustering.AgglomerativeClustering` |
| Similarity Computation (Document 1 / Document 2) | 3.3.4.1, 5.4.1.4 (Figure 11) | `DSFS.Core.Similarity.CosineSimilarity` |
| Results Generation | 5.4.1.5 (Figure 12), Chapter 4 | `DSFS.Core.Evaluation.ClusterEvaluator`, `DSFS.App.Controls.BarChartControl` |

The `AdminDashboardForm`'s five tabs are laid out in the same left-to-right,
top-to-bottom order as the pipeline in Figure 7:

1. **Data Collection** — import `.pdf` / `.docx` / `.txt` publications, tag each
   with a Scholar and Domain (the ground truth used later in tab 5).
2. **Preprocessing** — runs Tokenization → Stop Word Removal → Porter Stemming →
   TF-IDF and shows the resulting vocabulary/vectors.
3. **Clustering** — builds the dendrogram and cuts it into *k* clusters.
4. **Similarity** — the full pairwise cosine similarity matrix.
5. **Evaluation & Results** — Precision / Recall / F-Measure, drawn as a bar chart
   matching Figures 4–6.

## The "chaining issue" and `LinkageType.ImprovedSingleLinkage`

Section 2 of the thesis identifies **chaining** as the central weakness of plain
single-linkage clustering: two clusters get merged as soon as *any single pair*
of their members is close, even if the rest of the two clusters are, on average,
far apart. Repeated over many merges this can string together a long chain of
only loosely related documents into one over-grown cluster instead of the tight,
topic-pure clusters the thesis is trying to produce.

`AgglomerativeClustering` (in `DSFS.Core/Clustering/AgglomerativeClustering.cs`)
implements this project's fix as `LinkageType.ImprovedSingleLinkage`:

1. Rank all candidate cluster-pair merges by their **single-link distance**
   (the classic, chaining-prone rule) from smallest to largest.
2. For each candidate, in that order, also compute the **average-link distance**
   between the same two clusters.
3. Accept the first candidate whose average-link distance does not exceed
   `singleLinkDistance * chainGuardFactor` (default `1.5`, adjustable in the
   Clustering tab). This rejects a merge that is only "close" because of one
   outlier pair while the two clusters are, in aggregate, dissimilar.
4. If every candidate fails the guard (rare, and only on pathological inputs),
   fall back to the plain single-link minimum so the algorithm always
   terminates with a full dendrogram.

The other three linkage rules (`Single`, `Complete`, `Average`) are included
so the app can reproduce the classic baselines the thesis compares against in
Table 7 (Performance Evaluation).

## Evaluation methodology

`ClusterEvaluator.EvaluatePairwise` implements the definitions from section
4.1.2 in their standard, pairwise form: for every pair of documents, a
**true match** is a pair with the same Scholar *and* the same Domain. A pair is
then a **True Positive** if it's a true match placed in the same cluster, a
**False Positive** if it's not a true match but was placed in the same cluster,
and a **False Negative** if it's a true match that was split across clusters.
Precision, Recall and F-Measure are then computed exactly as in the thesis's
Table 5.

## Why JSON files instead of SQL Server?

The thesis's own implementation used SQL Server via Visual Studio 2013
(Table in section 3, "C# / Visual Studio 2013"). This repository swaps that for
a dependency-free JSON file store (`DSFS.Core.Persistence.JsonFileStore<T>`) so
the whole solution builds and runs with nothing installed beyond the .NET 8 SDK
— no database server to stand up before you can try it. Swapping in SQL Server,
SQLite or EF Core is a drop-in replacement behind `UserRepository` /
`DocumentRepository` without touching any of the clustering/preprocessing code.
