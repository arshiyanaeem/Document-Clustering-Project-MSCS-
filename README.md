# DSFS — Desktop Scholar Facilitator System

A complete C# (.NET 8, Windows Forms) implementation of the MSCS thesis
**"Development of an Efficient Hierarchical Clustering Analysis Using
Agglomerative Clustering Algorithm."**

DSFS clusters research scholars' publications by topic using an **agglomerative
hierarchical clustering algorithm** (TF-IDF + cosine similarity + single-linkage
with a chaining-issue fix), exactly following the system architecture in the
thesis's Figure 7. See [`docs/architecture.md`](docs/architecture.md) for a full
box-by-box map from the thesis to the source code.

## Features

- **Admin Interface** — a 5-tab pipeline that mirrors the thesis architecture:
  1. **Data Collection** — import `.pdf`, `.docx` or `.txt` research publications
  2. **Preprocessing** — Tokenization → Stop Word Removal → Porter Stemming → TF-IDF
  3. **Clustering** — agglomerative hierarchical clustering (Single / Complete /
     Average / **Improved Single Linkage**), with a live dendrogram
  4. **Similarity** — full pairwise cosine similarity matrix
  5. **Evaluation & Results** — Precision / Recall / F-Measure, charted
- **User Interface** — scholar registration, login and profile, including a
  read-only view of which cluster their own publications ended up in
- **No external services required** — no database server, no NuGet packages
  beyond the .NET SDK itself; everything (charts, dendrogram, JSON storage,
  password hashing, DOCX parsing) is built on the base class library
- A bundled **6-document sample dataset** (2 domains × 3 scholars) so you can
  try the whole pipeline in under a minute without hunting for real PDFs

## Project layout

```
DSFS.sln
src/
  DSFS.Core/                 Class library - all the algorithms, no UI dependency
    Preprocessing/           Tokenizer, StopWordRemover, PorterStemmer, TfIdfVectorizer
    Clustering/               AgglomerativeClustering, LinkageType
    Similarity/               CosineSimilarity
    Evaluation/               ClusterEvaluator (Precision/Recall/F-Measure)
    TextExtraction/           .pdf / .docx / .txt readers
    Security/                 PBKDF2 password hashing
    Persistence/              JSON-file "database" (Users, Documents)
    Models/                   DocumentRecord, ClusterNode, UserAccount, EvaluationResult
  DSFS.App/                  WinForms UI
    Forms/                    LoginForm, RegisterForm, ScholarProfileForm, AdminDashboardForm, AboutForm
    Controls/                 BarChartControl, DendrogramControl (hand-drawn GDI+, no charting package)
    SampleDocuments/          Bundled demo corpus
docs/
  architecture.md             Thesis section -> code file map, and the chaining-fix writeup
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (Windows, since the UI is
  Windows Forms)
- Visual Studio 2022 (recommended) or `dotnet` CLI

### Build & run

```bash
git clone <your-fork-url>
cd DSFS
dotnet build
dotnet run --project src/DSFS.App
```

Or just open `DSFS.sln` in Visual Studio, set **DSFS.App** as the startup
project, and press F5.

### First run

1. Sign in with the seeded admin account: **admin / Admin@123**
2. On the **Data Collection** tab, click **Load Sample Dataset** (or **Add
   File(s)...** to import your own `.pdf`/`.docx`/`.txt` publications)
3. Fill in the **Scholar** and **Domain** column for any documents that don't
   already have one (the sample dataset comes pre-labelled)
4. **Preprocessing** tab → **Run Preprocessing**
5. **Clustering** tab → pick a linkage type (default: **Improved Single
   Linkage**, this project's fix for the chaining issue) and a cluster count,
   then **Run Clustering** — watch the dendrogram render
6. **Similarity** tab → **Compute Cosine Similarity Matrix**
7. **Evaluation & Results** tab → **Evaluate Clustering** to see Precision /
   Recall / F-Measure, computed exactly as in the thesis's Chapter 4

You can also register a new scholar account from the login screen and see
their publications and cluster assignment from **My Publications & Cluster
Assignment** on the Scholar Profile screen (after the admin has clustered
them).

## Notes on faithfulness to the thesis

- **PDF text extraction**: real PDF parsing is a large problem on its own. This
  project extracts text with zero extra dependencies (tries the `pdftotext`
  command-line tool if installed, else a built-in fallback for simple,
  uncompressed PDFs). For robust extraction of arbitrary PDFs, swap in the
  `PdfPig` NuGet package as documented in `PdfTextExtractor.cs`.
- **Database**: the thesis's own prototype used SQL Server via Visual Studio
  2013. This repo uses a small JSON file store instead so it builds with
  nothing but the .NET SDK — see `docs/architecture.md` for why, and how to
  swap in a real database.
- **"Improved Single Linkage"**: the thesis states its contribution is fixing
  the chaining problem in single-linkage clustering. This repo's
  `LinkageType.ImprovedSingleLinkage` is a concrete, documented algorithm that
  implements that idea (a single-link candidate is only accepted if it isn't
  disproportionately more optimistic than the average-link distance between
  the same two clusters) — see `docs/architecture.md` for the full writeup and
  `AgglomerativeClustering.cs` for the implementation.

## License

MIT — see [`LICENSE`](LICENSE).
"# Document-Clustering-Project-MSCS-" 
