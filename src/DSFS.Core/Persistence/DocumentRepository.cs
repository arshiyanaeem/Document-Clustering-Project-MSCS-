using DSFS.Core.Models;

namespace DSFS.Core.Persistence;

/// <summary>
/// Backs the "Database" box in Figure 7 that feeds the Data Collection module: stores the
/// corpus of research publications (metadata + extracted text + latest pipeline results)
/// so an admin session can be closed and re-opened without re-importing every file.
/// </summary>
public class DocumentRepository
{
    private readonly string _path;
    private List<DocumentRecord> _documents;

    public DocumentRepository(string? path = null)
    {
        _path = path ?? AppPaths.DocumentsFile;
        _documents = JsonFileStore<DocumentRecord>.Load(_path);
    }

    public List<DocumentRecord> GetAll() => _documents;

    public int NextId() => _documents.Count == 0 ? 1 : _documents.Max(d => d.Id) + 1;

    public void Add(DocumentRecord document)
    {
        _documents.Add(document);
        Save();
    }

    public void Remove(int documentId)
    {
        _documents.RemoveAll(d => d.Id == documentId);
        Save();
    }

    public void ReplaceAll(IEnumerable<DocumentRecord> documents)
    {
        _documents = documents.ToList();
        Save();
    }

    public void Save() => JsonFileStore<DocumentRecord>.Save(_path, _documents);
}
