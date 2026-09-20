# DSFS — Desktop Scholar Facilitator System

DSFS is a C# Windows application built with .NET 8. It organizes and clusters academic research papers by topic using an agglomerative hierarchical clustering algorithm (TF-IDF + cosine similarity + single-linkage).
This project implements the architecture from the MSCS thesis "Development of an Efficient Hierarchical Clustering Analysis Using Agglomerative Clustering Algorithm."

## Features

  -**Admin Dashboard:** -
1. **Data Collection:** Import research papers in PDF, DOCX, or TXT formats.
2. **Preprocessing:** Handles tokenization, stop-word removal, Porter stemming, and TF-IDF vectorization.
3. **Clustering:** Runs hierarchical clustering (Single, Complete, Average, and Improved Single Linkage) and displays a real-time dendrogram.
4. **Similarity Matrix:** Calculates full pairwise cosine similarity scores.
5. **Evaluation:** Generates Precision, Recall, and F-Measure metrics with built-in charts.
6. **Scholar Portal:**
Allows scholars to register, log in, and view which cluster their publications belong to.
7. **Zero External Dependencies:**
Uses no third-party NuGet packages or database servers. Everything runs on standard .NET libraries.
8. **Sample Dataset Included:**
Comes with 6 pre-loaded documents across 3 scholars so you can test the pipeline immediately.

## Project layout

```
DSFS.sln
├── src/
│   ├── DSFS.Core/       # Core library: algorithms, text extraction, security, and JSON storage
│   └── DSFS.App/        # WinForms UI: dashboards, forms, and custom GDI+ charts
└── docs/
    └── architecture.md  # Maps thesis sections directly to code files
```

## Quick Start
### Prerequisites
-Windows OS
-.NET 8 SDK
-Visual Studio 2022 or the dotnet CLI

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
1. Log in with the default admin credentials:
-Username: admin
-Password: Admin@123
2. Go to the Data Collection tab and click Load Sample Dataset.
3. Move to Preprocessing and click Run Preprocessing.
4. Go to Clustering, pick a linkage type (default is Improved Single Linkage), set your cluster count, and click Run Clustering.
5. Check the Similarity and Evaluation & Results tabs to see the generated matrix and accuracy scores.

## Notes on faithfulness to the thesis

-**PDF Reading:** Uses standard internal PDF extraction for basic files. For complex or scanned PDFs, you can integrate the PdfPig NuGet package in PdfTextExtractor.cs.
-**Database:** Uses lightweight JSON files for data storage instead of SQL Server to keep setup quick and dependency-free.
-**Chaining Fix:** The thesis focuses on solving the "chaining problem" in single-linkage clustering. The ImprovedSingleLinkage option in AgglomerativeClustering.cs implements this custom logic.

## License
This project is licensed under the MIT License.
